using Microsoft.EntityFrameworkCore.Storage;

namespace Repos;

public interface ITransaction
{
    IDbContextTransaction BeginTransaction();
    IExecutionStrategy GetExecutionStrategy();
}