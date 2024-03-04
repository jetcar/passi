using Microsoft.EntityFrameworkCore.Diagnostics;
using PostSharp.Extensibility;
using Serilog;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Repos
{
    [ReposProfile(AttributeTargetElements = MulticastTargets.Method)]
    public class TaggedQueryCommandInterceptor : DbCommandInterceptor
    {
        private ILogger _logger;

        public TaggedQueryCommandInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            _logger.LogAppDebug("ReaderExecuted " + command.CommandText);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
        {
            _logger.LogAppDebug("CommandCreated" + result.CommandText);
            return base.CommandCreated(eventData, result);
        }

        public override InterceptionResult<DbCommand> CommandCreating(CommandCorrelatedEventData eventData, InterceptionResult<DbCommand> result)
        {
            if (result.HasResult)
                _logger.LogAppDebug(result.Result.CommandText);
            return base.CommandCreating(eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = new CancellationToken())
        {
            _logger.LogAppDebug(command.CommandText);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            _logger.LogAppDebug(command.CommandText);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            _logger.LogAppDebug(command.CommandText);
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            _logger.LogAppDebug(command.CommandText);
            return base.NonQueryExecuted(command, eventData, result);
        }
    }
}