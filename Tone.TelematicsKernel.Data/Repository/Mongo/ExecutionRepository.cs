using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core.Data;
using Tone.Core.Enums;
using Tone.Core.Extensions;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class ExecutionRepository : RepositoryBaseObjectId<IExecution, Execution>, IExecutionRepository
    {
        public ExecutionRepository(IConnectionStringProvider connectionStringProvider)
            : base(Config.CommandExecutionCollection, connectionStringProvider.ConnectionString)
        {
        }

        public async Task<IExecution> PickExecution(object[] deviceIds)
        {
            var filter = Builders<Execution>.Filter.And(
                Builders<Execution>.Filter.In(f => f.DeviceId, deviceIds),
                Builders<Execution>.Filter.Eq(f => f.State, ExecutionState.New));
            var update = Builders<Execution>.Update.Set(u => u.State, ExecutionState.Processing);
            var execution = await Collection.FindOneAndUpdateAsync(filter, update,
                new FindOneAndUpdateOptions<Execution, Execution>
                {
                    IsUpsert = false,
                    ReturnDocument = ReturnDocument.After,
                    Sort = Builders<Execution>.Sort.Ascending(s => s.Created)
                });
            return execution;
        }

        public async Task ResetExecutions()
        {
            var filter = Builders<Execution>.Filter.Eq(c => c.State, ExecutionState.Processing);
            var update = Builders<Execution>.Update.Set(c => c.State, ExecutionState.New);
            await Collection.UpdateManyAsync(filter, update, new UpdateOptions { IsUpsert = false });
        }
        
        public async Task<IExecution> ExecuteCommand(IDevice device, string commandCode, params ValueBase[] values)
        {
            var command = device.Commands.FirstOrDefault(c => c.Code == commandCode);
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            var commandIndex = device.Commands.IndexOf(command);
            var execution = New();
            execution.DeviceId = device.Id;
            execution.Index = commandIndex;
            execution.Type = ExecutionType.Command;

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                var commandArgument = command.Arguments[i];
                var executionValue = commandArgument.Type.GetValueBase();
                executionValue?.SetValue(value);
                execution.Parameters.Add(new ExecutionValue
                {
                    Id = i,
                    Value = executionValue
                });
            }

            return await Repsert(execution);
        }
    }
}