using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Tone.Core.Data.Account;
using Tone.Core.Enums;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class DriveStyleSettingsHistoryRepository : CustomerRepositoryBase<IDriveStyleSettingsHistory, DriveStyleSettingsHistory>, IDriveStyleSettingsHistoryRepository
    {
        public DriveStyleSettingsHistoryRepository(IConnectionStringProvider provider)
            : base(Config.DriveStyleSettingsHistoryCollection, provider.ConnectionString)
        {
        }

        public async Task<IDriveStyleSettingsHistory[]> GetDriveStyleSettingsHistory(DriveStyleHistoryRequest request)
        {
            var cusomerId = ToId(request.CustomerId);
            var customerFilter = Builders<DriveStyleSettingsHistory>.Filter.Eq(r => r.CustomerId, cusomerId);
            var dateFilter = (request.DateStart.HasValue && request.DateFinish.HasValue)
                ? Builders<DriveStyleSettingsHistory>.Filter.Where(r => r.DateUpdated >= request.DateStart && r.DateUpdated <= request.DateFinish)
                : Builders<DriveStyleSettingsHistory>.Filter.Where(r => true);
            var typefilter = (!request.HistoryType.HasValue || request.HistoryType == DriveStyleSettingsHistoryType.All)
                ? Builders<DriveStyleSettingsHistory>.Filter.Where(r => true)
                : Builders<DriveStyleSettingsHistory>.Filter.Where(r => r.HistoryType == request.HistoryType);
            var filter = Builders<DriveStyleSettingsHistory>.Filter.And(customerFilter, dateFilter, typefilter);

            var history = await Collection.Find(filter).ToListAsync();
            return history.Cast<IDriveStyleSettingsHistory>().ToArray();
        }

        public async Task<IDriveStyleSettingsHistory> SaveDriveStyleSettingsHistory(IDriveStyleSettingsHistory history)
        {
            //var id = ToId(history.Id);
            //var filter = Builders<DriveStyleSettingsHistory>.Filter.Where(r => r.Id.Equals(id));
            return await Repsert(history);
        }
        

        public async Task<IDriveStyleSettingsHistory> SavePenaltyScores(IDriveStylePenaltyScores scores)
        {
            var history = BuildDriveStyleSettingsHistory(DriveStyleSettingsHistoryType.PenaltyScores);
            history.DriveStylePenaltyScores = scores;
            return await Repsert(history);
        }

        public async Task<IDriveStyleSettingsHistory> SaveCoefficients(IDriveStyleCoefficients coefficients)
        {
            var history = BuildDriveStyleSettingsHistory(DriveStyleSettingsHistoryType.Coefficients);
            history.DriveStyleCoefficients = coefficients;
            return await Repsert(history);
        }

        public async Task<IDriveStyleSettingsHistory> SaveVehiclePenalties(IDriveStyleVehiclePenalties penalties)
        {
            var history = BuildDriveStyleSettingsHistory(DriveStyleSettingsHistoryType.VehiclePenalties);
            history.DriveStyleVehiclePenalties = penalties;
            return await Repsert(history);
        }

        private DriveStyleSettingsHistory BuildDriveStyleSettingsHistory(DriveStyleSettingsHistoryType historyType)
        {
            var history = new DriveStyleSettingsHistory()
            {
                AccountId = Rc.Account?.Id,
                EmployeeId = (Rc.Account?.Profile is PersonProfile) ? ((PersonProfile) Rc.Account?.Profile).EmployeeId : null,
                DateUpdated = DateTime.UtcNow,
                HistoryType = historyType
            };
            
            return history;
            
        }

        private FilterDefinition<IDriveStyleSettingsHistory> BuildCustomerFilter() => Builders<IDriveStyleSettingsHistory>.Filter.Eq(r => r.CustomerId, CustomerId);
        
    }
}
