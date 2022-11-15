using Microsoft.EntityFrameworkCore.Storage;

namespace Repos
{
    public class ClientsRepository : IClientsRepository
    {
        private PassiDbContext _dbContext;

        public ClientsRepository(PassiDbContext dbContext)
        {
            _dbContext = dbContext;
        }

      

        public IDbContextTransaction BeginTransaction()
        {
            return _dbContext.Database.BeginTransaction();
        }

        
    }

    public interface IClientsRepository
    {
        IDbContextTransaction BeginTransaction();
    }
}
