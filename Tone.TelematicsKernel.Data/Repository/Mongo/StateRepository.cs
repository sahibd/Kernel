using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core.Data;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo;
using Tone.Data.Mongo.Base;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class StateRepository : RepositoryBaseObjectId<IRecordState, RecordState>, IStateRepository
    {
        public StateRepository(IConnectionStringProvider provider)
            : base(Config.DeviceStateCollection, provider.ConnectionString)
        {
        }

        public async Task<IRecordState> Get(object deviceId)
        {
            var filter = Builders<RecordState>.Filter.Eq(r => r.DeviceId, deviceId);
            var state = await Collection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
            return state;
        }

        public async Task<IRecordState[]> Get(params object[] deviceIds)
        {
            var filter = Builders<RecordState>.Filter.In(r => r.DeviceId, deviceIds);
            var states = await Collection.Find(filter).ToListAsync();
            return states.Cast<IRecordState>().ToArray();
        }

        public async Task<IRecordState> Get(IVirtualDevice virtualDevice)
        {
            var deviceIds = virtualDevice.Properties.Select(d => d.DeviceId).Distinct().ToArray();
            var deviceProperties = deviceIds.ToDictionary(id => id,
                id => virtualDevice.Properties.Where(p => p.DeviceId == id).ToArray());
            var state = New();
            state.DeviceId = virtualDevice.Id;

            var deviceStates = await Get(deviceIds);
            foreach (var deviceState in deviceStates)
            {
                var stateProperties = deviceState.Values
                    .Where(p => deviceProperties[deviceState.DeviceId].Any(sp => sp.PropertyIndex == p.Index));
                state.Values.AddRange(stateProperties);
            }

            return state;
        }
    }
}