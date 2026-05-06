using Microsoft.EntityFrameworkCore;
using VendlyServer.Application.Services.Addresses.Contracts;
using VendlyServer.Domain.Abstractions;
using VendlyServer.Domain.Entities.Ref;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Application.Services.Addresses;

public class AddressService(AppDbContext dbContext) : IAddressService
{
    private const int MaxAddressesPerUser = 10;

    public async Task<Result<List<AddressResponse>>> GetAllForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var addresses = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressResponse(
                a.Id,
                a.Label,
                a.City,
                a.District,
                a.Street,
                a.House,
                a.Extra,
                a.BtsCityCode,
                a.IsDefault,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return addresses;
    }

    public async Task<Result<AddressResponse>> GetByIdAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .AsNoTracking()
            .Where(a => a.Id == id && a.UserId == userId && !a.IsDeleted)
            .Select(a => new AddressResponse(
                a.Id,
                a.Label,
                a.City,
                a.District,
                a.Street,
                a.House,
                a.Extra,
                a.BtsCityCode,
                a.IsDefault,
                a.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return address is null ? AddressErrors.NotFound : address;
    }

    public async Task<Result<AddressResponse>> AddAsync(long userId, CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var btsCityExists = await dbContext.BtsCities
            .AsNoTracking()
            .AnyAsync(c => c.Code == request.BtsCityCode, cancellationToken);

        if (!btsCityExists)
            return AddressErrors.BtsCityCodeInvalid;

        var existingCount = await dbContext.Addresses
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId && !a.IsDeleted, cancellationToken);

        if (existingCount >= MaxAddressesPerUser)
            return AddressErrors.LimitReached;

        var shouldBeDefault = existingCount == 0 || request.IsDefault;

        if (shouldBeDefault && existingCount > 0)
        {
            var defaults = await dbContext.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted && a.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existing in defaults)
                existing.IsDefault = false;
        }

        var address = new Address
        {
            UserId = userId,
            Label = request.Label,
            City = request.City,
            District = request.District,
            Street = request.Street,
            House = request.House,
            Extra = request.Extra,
            BtsCityCode = request.BtsCityCode,
            IsDefault = shouldBeDefault
        };

        dbContext.Addresses.Add(address);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddressResponse(
            address.Id,
            address.Label,
            address.City,
            address.District,
            address.Street,
            address.House,
            address.Extra,
            address.BtsCityCode,
            address.IsDefault,
            address.CreatedAt);
    }

    public async Task<Result<AddressResponse>> UpdateAsync(long userId, long id, UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted, cancellationToken);

        if (address is null)
            return AddressErrors.NotFound;

        if (address.BtsCityCode != request.BtsCityCode)
        {
            var btsCityExists = await dbContext.BtsCities
                .AsNoTracking()
                .AnyAsync(c => c.Code == request.BtsCityCode, cancellationToken);

            if (!btsCityExists)
                return AddressErrors.BtsCityCodeInvalid;
        }

        if (request.IsDefault && !address.IsDefault)
        {
            var defaults = await dbContext.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted && a.IsDefault && a.Id != id)
                .ToListAsync(cancellationToken);

            foreach (var existing in defaults)
                existing.IsDefault = false;
        }

        address.Label = request.Label;
        address.City = request.City;
        address.District = request.District;
        address.Street = request.Street;
        address.House = request.House;
        address.Extra = request.Extra;
        address.BtsCityCode = request.BtsCityCode;
        address.IsDefault = request.IsDefault || address.IsDefault;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddressResponse(
            address.Id,
            address.Label,
            address.City,
            address.District,
            address.Street,
            address.House,
            address.Extra,
            address.BtsCityCode,
            address.IsDefault,
            address.CreatedAt);
    }

    public async Task<Result> DeleteAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted, cancellationToken);

        if (address is null)
            return AddressErrors.NotFound;

        var wasDefault = address.IsDefault;
        address.IsDeleted = true;
        address.IsDefault = false;

        if (wasDefault)
        {
            var nextDefault = await dbContext.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted && a.Id != id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextDefault is not null)
                nextDefault.IsDefault = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<AddressResponse>> SetDefaultAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var address = await dbContext.Addresses
            .SingleOrDefaultAsync(a => a.Id == id && a.UserId == userId && !a.IsDeleted, cancellationToken);

        if (address is null)
            return AddressErrors.NotFound;

        if (!address.IsDefault)
        {
            var defaults = await dbContext.Addresses
                .Where(a => a.UserId == userId && !a.IsDeleted && a.IsDefault && a.Id != id)
                .ToListAsync(cancellationToken);

            foreach (var existing in defaults)
                existing.IsDefault = false;

            address.IsDefault = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AddressResponse(
            address.Id,
            address.Label,
            address.City,
            address.District,
            address.Street,
            address.House,
            address.Extra,
            address.BtsCityCode,
            address.IsDefault,
            address.CreatedAt);
    }
}
