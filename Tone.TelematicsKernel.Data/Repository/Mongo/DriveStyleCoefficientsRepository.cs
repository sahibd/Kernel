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
    public class DriveStyleCoefficientsRepository : CustomerRepositoryBase<IDriveStyleCoefficients, DriveStyleCoefficients>, IDriveStyleCoefficientsRepository
    {
        public DriveStyleCoefficientsRepository(IConnectionStringProvider provider)
            : base(Config.DriveStyleCoefficientsCollection, provider.ConnectionString)
        {
        }

        public async Task<IDriveStyleCoefficients> GetCoefficients()
        {
            var filterCustomer = BuildCustomerFilter();
            var result = await Collection.Find(filterCustomer).FirstOrDefaultAsync();
            return result;
        }

        public async Task<IDriveStyleCoefficients> ManageCoefficients(IDriveStyleCoefficients coefficients)
        {
            var currentcoefficients = await GetCoefficients();

            if (currentcoefficients != null)
            {
                currentcoefficients.Speed = coefficients.Speed;
                currentcoefficients.DrivingExperience = coefficients.DrivingExperience;
                currentcoefficients.AccidentsCounts = coefficients.AccidentsCounts;
                currentcoefficients.RoadSigns = coefficients.RoadSigns;
                currentcoefficients.TripTime = coefficients.TripTime;
            }
            else
                currentcoefficients = coefficients;

            return await Repsert(currentcoefficients);
        }

        private FilterDefinition<DriveStyleCoefficients> BuildCustomerFilter() => Builders<DriveStyleCoefficients>.Filter.Eq(r => r.CustomerId, CustomerId);
    }
}
