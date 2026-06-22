using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VendlyServer.Api.Filters;

public class MlfHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Example = new OpenApiString("UZ"),
            AllowEmptyValue = true,
            Schema = new OpenApiSchema
            {
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("UZ"),
                    new OpenApiString("RU"),
                    new OpenApiString("EN"),
                    new OpenApiString("UZ-CYRL"),
                },
                Default = new OpenApiString("UZ")
            }
        });
    }
}
