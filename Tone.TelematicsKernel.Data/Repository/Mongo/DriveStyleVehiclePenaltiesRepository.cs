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

    public class DriveStyleVehiclePenaltiesRepository : CustomerRepositoryBase<IDriveStyleVehiclePenalties, DriveStyleVehiclePenalties>, IDriveStyleVehiclePenaltiesRepository
    {

        public DriveStyleVehiclePenaltiesRepository(IConnectionStringProvider provider)
            : base(Config.DriveStyleVehiclePenaltiesCollection, provider.ConnectionString)
        {
        }

        public async Task<IDriveStyleVehiclePenalties> GetVehiclePenalties()
        {
            var filterCustomer = BuildCustomerFilter();
            var result = await Collection.Find(filterCustomer).FirstOrDefaultAsync();
            return result;
        }

        public async Task<IDriveStyleVehiclePenalties> ManageVehiclePenalties(IDriveStyleVehiclePenalties penalties)
        {
            var currentPenalties = await GetVehiclePenalties();

            if (currentPenalties != null)
            {
                currentPenalties.CarPenalty = penalties.CarPenalty;
                currentPenalties.BusPenalty = penalties.BusPenalty;
                currentPenalties.TruckPenalty = penalties.TruckPenalty;
            }
            else
                currentPenalties = penalties;

            return await Repsert(currentPenalties);
        }

        private FilterDefinition<DriveStyleVehiclePenalties> BuildCustomerFilter() => Builders<DriveStyleVehiclePenalties>.Filter.Eq(r => r.CustomerId, CustomerId);
    }
}
