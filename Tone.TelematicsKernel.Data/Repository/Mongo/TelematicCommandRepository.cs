using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Tone.Core.Provider;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.AnalyticSystem.Repository;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Model;

namespace Tone.AnalyticProcessorCore.Data.Repository
{
    public class TelematicCommandRepository : RepositoryBaseObjectId<ITelematicCommand, TelematicCommand>, ITelematicCommandRepository
    {
        public TelematicCommandRepository(IConnectionStringProvider connectionStringProvider) : base("TelematicCommands", connectionStringProvider.ConnectionString, 0)
        {
        }

        public async Task<ITelematicCommand[]> GetNewCommands()
        {
			try
			{
				var filter = Builders<TelematicCommand>.Filter.Eq(r => r.Status, Status.NotExecuted);
				var result = await Collection.Find(filter).ToListAsync();
				return result.ToArray<ITelematicCommand>();
			}
			catch
			{
				return null;
			}
        }
    }
}
