using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Repos;

public class BaseRepo<T> where T : DbContext
{
    protected T _dbContext;
    public BaseRepo(T dbContext)
    {
        _dbContext = dbContext;
    }
    public IDbContextTransaction BeginTransaction()
    {
        return _dbContext.Database.BeginTransaction();
    }
    public IExecutionStrategy GetExecutionStrategy()
    {
        return _dbContext.Database.CreateExecutionStrategy();
    }
}