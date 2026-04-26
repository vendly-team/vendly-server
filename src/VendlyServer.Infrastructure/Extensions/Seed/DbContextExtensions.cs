using Microsoft.EntityFrameworkCore;
using VendlyServer.Domain.Entities.Public;
using VendlyServer.Domain.Enums;
using VendlyServer.Infrastructure.Authentication;
using VendlyServer.Infrastructure.Persistence;

namespace VendlyServer.Infrastructure.Extensions.Seed;

public static class DbContextExtensions
{
    extension(AppDbContext dbContext)
    {
        public async Task SeedAsync()
        {
            IPasswordHasher passwordHasher = new PasswordHasher();
        
            await dbContext.SeedUsersAsync(passwordHasher);
        }

        private async Task SeedUsersAsync(IPasswordHasher passwordHasher)
        {
            var seedUsers = new[]
            {
                new { FirstName = "Customer",  LastName = "User", Email="user@test.com",   Phone = "+998900000001", Password = "user123",    Role = UserRole.Customer },
                new { FirstName = "Admin",     LastName = "User", Email="admin@test.com",  Phone = "+998900000002", Password = "admin123",   Role = UserRole.Admin    },
                new { FirstName = "Manager",   LastName = "User", Email="manager@test.com",Phone = "+998900000003", Password = "manager123", Role = UserRole.Manager  },
            };

            var phones = seedUsers.Select(u => u.Phone).ToList();

            var existingPhones = await dbContext.Users
                .Where(u => phones.Contains(u.Phone))
                .Select(u => u.Phone)
                .ToListAsync();

            var toInsert = seedUsers
                .Where(u => !existingPhones.Contains(u.Phone))
                .Select(u => new User
                {
                    FirstName    = u.FirstName,
                    LastName     = u.LastName,
                    Phone        = u.Phone,
                    Email        = u.Email,
                    PasswordHash = passwordHasher.Hash(u.Password),
                    Role         = u.Role,
                })
                .ToList();

            if (toInsert.Count == 0) return;

            dbContext.Users.AddRange(toInsert);
            await dbContext.SaveChangesAsync();
        }
    }
}
