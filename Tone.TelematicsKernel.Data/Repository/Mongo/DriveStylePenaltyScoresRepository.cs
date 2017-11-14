using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class DriveStylePenaltyScoresRepository : CustomerRepositoryBase<IDriveStylePenaltyScores, DriveStylePenaltyScores>, IDriveStylePenaltyScoresRepository
    {
        public DriveStylePenaltyScoresRepository(IConnectionStringProvider provider)
            : base(Config.DriveStylePenaltyScoresCollection, provider.ConnectionString)
        {
        }

        public async Task<IDriveStylePenaltyScores> GetPenaltyScores()
        {
            var filterCustomer = BuildCustomerFilter();
            var result = await Collection.Find(filterCustomer).FirstOrDefaultAsync();
            return result;
        }

        public async Task<IDriveStylePenaltyScores> ManagePenaltyScores(IDriveStylePenaltyScores scores)
        {
            var currentScores = await GetPenaltyScores();

            if (currentScores != null)
            {
                currentScores.OverSpeeding = scores.OverSpeeding;
                currentScores.SharpAccelerations = scores.SharpAccelerations;
                currentScores.SharpBrakings = scores.SharpBrakings;
                currentScores.SharpTurns = scores.SharpTurns;
            }
            else
                currentScores = scores;

            return await Repsert(currentScores);
        }

        private FilterDefinition<DriveStylePenaltyScores> BuildCustomerFilter() => Builders<DriveStylePenaltyScores>.Filter.Eq(r => r.CustomerId, CustomerId);
    }
}
