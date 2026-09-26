using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIDC.Data;
using OpenIDC.Models;

namespace OpenIDC.Services
{
    public interface IRegisteredClientRepository
    {
        Task<RegisteredClient> FindByClientIdAsync(string clientId);
        Task<List<RegisteredClient>> ListByOwnerAsync(string ownerSubject);
        Task<int> CountByOwnerAsync(string ownerSubject);
        Task AddAsync(RegisteredClient client);
        Task UpdateAsync(RegisteredClient client);
        Task DeleteAsync(RegisteredClient client);
    }

    /// <summary>Opens a DbContext scope per call so it can be used from the singleton ClientStore.</summary>
    public class RegisteredClientRepository : IRegisteredClientRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public RegisteredClientRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<RegisteredClient> FindByClientIdAsync(string clientId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenIdcDbContext>();
            return await db.RegisteredClients.AsNoTracking().FirstOrDefaultAsync(x => x.ClientId == clientId);
        }

        public async Task<List<RegisteredClient>> ListByOwnerAsync(string ownerSubject)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenIdcDbContext>();
            return await db.RegisteredClients.AsNoTracking()
                .Where(x => x.OwnerSubject == ownerSubject)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountByOwnerAsync(string ownerSubject)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenIdcDbContext>();
            return await db.RegisteredClients.CountAsync(x => x.OwnerSubject == ownerSubject);
        }

        public async Task AddAsync(RegisteredClient client)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenIdcDbContext>();
            db.RegisteredClients.Add(client);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(RegisteredClient client)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenIdcDbContext>();
            db.RegisteredClients.Update(client);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(RegisteredClient client)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpenIdcDbContext>();
            db.RegisteredClients.Remove(client);
            await db.SaveChangesAsync();
        }
    }
}
