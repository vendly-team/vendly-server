using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Infrastructure.Brokers.Hamkor.Contracts;

namespace VendlyServer.Infrastructure.Brokers.Hamkor;

public class HamkorBroker(
    IHttpClientFactory httpClientFactory,
    IOptions<HamkorOptions> options,
    ILogger<HamkorBroker> logger) : IHamkorBroker
{
    private const string AcquiringPath = "/acquiring/v1";

    private readonly HamkorOptions options = options.Value;

    private string? accessToken;
    private DateTime accessTokenExpiry = DateTime.MinValue;

    #region Auth

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (accessToken is not null && DateTime.UtcNow < accessTokenExpiry)
            return accessToken;

        var client = CreateHttpClient();
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Key}:{options.Secret}"));
        var requestBodyJson = JsonSerializer.Serialize(new { grant_type = "client_credentials" });

        logger.LogInformation("Hamkor → POST {Url}/token Body: {Body}", options.BaseUrl, requestBodyJson);

        var request = new HttpRequestMessage(HttpMethod.Post, "/token")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", basic) },
            Content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json"),
        };

        var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogInformation("Hamkor ← /token {StatusCode} Body: {Body}", (int)response.StatusCode, responseBody);

        if (!response.IsSuccessStatusCode) return null;

        var result = JsonSerializer.Deserialize<HamkorTokenResponse>(responseBody);
        if (string.IsNullOrWhiteSpace(result?.AccessToken)) return null;

        accessToken = result.AccessToken;
        // Refresh a minute early to avoid edge-of-expiry failures.
        accessTokenExpiry = DateTime.UtcNow.AddSeconds(Math.Max(60, result.ExpiresIn) - 60);
        return accessToken;
    }

    #endregion

    #region Payments

    public async Task<Result<string>> CreatePaymentUrlAsync(
        HamkorCreatePaymentUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var fiscalItems = request.Items
            .Select(item => new HamkorFiscalItem
            {
                Amount = item.AmountMinorUnits,
                Count = item.Qty,
                PackageCode = options.PackageCode,
                Spic = options.Spic,
            })
            .ToArray();

        var rpcResult = await SendRpcAsync<HamkorCreateUrlParams, HamkorCreateUrlResult>(
            HamkorMethods.CreatePaymentUrl,
            new HamkorCreateUrlParams
            {
                ExternalId = request.ExternalId,
                Amount = request.AmountMinorUnits,
                SuccessUrl = request.SuccessUrl,
                FailureUrl = request.FailureUrl,
                CallbackUrl = request.CallbackUrl,
                Hold = 1,
                FiscalData = new HamkorFiscalData
                {
                    Item = fiscalItems,
                    Location = new HamkorLocation
                    {
                        Lat = options.LocationLat,
                        Long = options.LocationLong,
                    },
                    Tin = options.Tin,
                    VatPercent = options.VatPercent,
                },
            },
            cancellationToken);

        if (rpcResult.IsFailure)
            return HamkorErrors.CreatePaymentUrlFailed;

        var url = rpcResult.Data?.Url;
        if (string.IsNullOrWhiteSpace(url))
            return HamkorErrors.CreatePaymentUrlFailed;

        return url;
    }

    public async Task<Result<HamkorInvoiceResult>> GetByExtIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        // pay.get.inv returns an array of invoices.
        var rpcResult = await SendRpcAsync<HamkorExtIdParams, HamkorInvoiceResult[]>(
            HamkorMethods.GetInvoice,
            new HamkorExtIdParams { ExtId = externalId },
            cancellationToken);

        if (rpcResult.IsFailure)
            return HamkorErrors.GetInvoiceFailed;

        var invoice = rpcResult.Data?.FirstOrDefault();
        if (invoice is null)
            return HamkorErrors.GetInvoiceFailed;

        return invoice;
    }

    #endregion

    #region Helpers

    private async Task<Result<TResult>> SendRpcAsync<TParams, TResult>(
        string method,
        TParams param,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (token is null)
            return HamkorErrors.TokenFailed;

        var client = CreateHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rpcRequest = new HamkorRpcRequest<TParams>
        {
            Method = method,
            Params = [param],
            Id = Guid.NewGuid().ToString()
        };

        var requestBodyJson = JsonSerializer.Serialize(rpcRequest);

        logger.LogInformation(
            "Hamkor → {Method} requestId={RequestId} Body: {Body}",
            method, rpcRequest.Id, requestBodyJson);

        var response = await client.PostAsync(
            AcquiringPath,
            new StringContent(requestBodyJson, Encoding.UTF8, "application/json"),
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        logger.LogInformation(
            "Hamkor ← {Method} {StatusCode} requestId={RequestId} Body: {Body}",
            method, (int)response.StatusCode, rpcRequest.Id, responseBody);

        if (!response.IsSuccessStatusCode)
            return Error.Failure($"Hamkor.{method}.HttpError");

        var rpcResponse = JsonSerializer.Deserialize<HamkorRpcResponse<TResult>>(responseBody);

        if (rpcResponse?.Error is not null)
            return Error.Failure($"Hamkor.{method}.Error");

        if (rpcResponse is null || rpcResponse.Result is null)
            return Error.Failure($"Hamkor.{method}.EmptyResult");

        return rpcResponse.Result;
    }

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient("Hamkor");
        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    #endregion
}
