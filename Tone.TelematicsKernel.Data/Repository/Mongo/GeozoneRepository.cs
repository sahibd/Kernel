using System.Collections.Generic;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using Tone.Core.Enums;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class GeozoneRepository : CustomerRepositoryBase<IGeozone, Geozone>, IGeozoneRepository
    {
        public GeozoneRepository(IConnectionStringProvider provider)
            : base(Config.GeozoneCollection, provider.ConnectionString)
        {
        }

        public async Task<IGeozone[]> GetAllByAccessType(GeozoneAccessType? accessType, string nameFilter)
        {
            var filterCustomer = BuildCustomerFilter();

            var filterAccess = (accessType != null && accessType.Value != GeozoneAccessType.All)
                ? Builders<Geozone>.Filter.Eq(g => g.Access, accessType.Value)
                : Builders<Geozone>.Filter.Where(g => true);

            var filterName = !string.IsNullOrEmpty(nameFilter)
                ? Builders<Geozone>.Filter.Where(g => g.Name.ToLower().Contains(nameFilter.ToLower()))
                : Builders<Geozone>.Filter.Where(g => true);

            var filter = Builders<Geozone>.Filter.And(filterCustomer, filterAccess, filterName);
            var result = await Collection.Find(filter).ToListAsync();
            return result.Cast<IGeozone>().ToArray();
        }

        private FilterDefinition<Geozone> BuildCustomerFilter() => Builders<Geozone>.Filter.Eq(r => r.CustomerId, CustomerId);
        
        public async Task<IGeozone> SaveGeozone(IGeozone geozone)
        {
            if (geozone.Type == GeozoneType.Round)
            {
                geozone.GeoInfo = GeoJsonGenerator.GenerateGeoInfo(geozone.Type, geozone.BufferThickness + (geozone.Radius ?? 0), geozone.Points);
                geozone = await Repsert(geozone);
            }
            else
            {
                geozone.GeoInfo = GeoJsonGenerator.GenerateGeoInfo(geozone.Type, geozone.BufferThickness, geozone.Points);
                geozone = await Repsert(geozone);
            }
            return geozone;
        }

        public async Task<IGeozone[]> GetGeozonesByCustomer(object customerId)
        {
            var id = ToId(customerId);
            var filter = Builders<Geozone>.Filter.Eq(r => r.CustomerId, id);

            var result = await Collection.Find(filter).ToListAsync();
            return result.Cast<IGeozone>().ToArray();
        }

        /// <summary>
        /// Получение элементов по их идентификаторам без привязки к Customer 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IGeozone[]> AdminGetGeozonesByIds(object[] ids)
        {
            var keys = ids.Cast<ObjectId?>().ToArray();
            var filter = Builders<Geozone>.Filter.Where(g => keys.Contains(g.Id));
            var result = await Collection.Find(filter).ToListAsync();

            return result.Cast<IGeozone>().ToArray();
            //var result = await Collection<>.Find(x => keys.Contains(x.Id)).ToListAsync().ConfigureAwait(false);
        }
    }
}