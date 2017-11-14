using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.Data.Mongo.Base;
using Tone.TelematicsKernel.Data.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo.GeoProviders
{
    public class GeocodeInfoRepository : RepositoryBaseObjectId<IGeocodeInfo, GeocodeInfo>, IGeocodeInfoRepository
    {
        public GeocodeInfoRepository(IConnectionStringProvider connectionStringProvider) : base(Config.GeocodingCollection, connectionStringProvider.GeoConnectionString)
        {
        }

        public async Task<IGeocodeInfo> GetGeocodeInfo(double longitude, double latitude, int distance)
        {
            var match = new BsonDocument
                {
                    {"GeoInfo",
                        new BsonDocument
                            {
                                {"$near",
                                 new BsonDocument
                                    {
                                                {"$geometry" , new BsonDocument
                                                    {
                                                        {"type", "Point"},
                                                        {"coordinates", new BsonArray(new[] { longitude, latitude} )}
                                                    }
                                                },
                                                { "$maxDistance",  distance }
                                    }
                                }
                            }
                    }
                };

            try
            {
                var res = await Collection.Find(match).Limit(1).Sort("{level:-1}").Project<GeocodeInfo>("{subject:1,city:1,street:1,building:1,vladenie:1,corpus:1,stroenie:1,soorugenie:1}").ToListAsync().ConfigureAwait(false);
                return res[0];
            }
            catch(Exception)
            {
                return null;
            }
        }

        public async Task<IGeocodeInfo> GetGeocodeInfo_ver2(double longitude, double latitude, int distance)
        {
            GeoJsonPoint<GeoJson2DGeographicCoordinates> gp = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude, latitude));
            var query = Builders<GeocodeInfo>.Filter.Near("GeoInfo", gp, distance);
            try
            {
                List<GeocodeInfo> res = await Collection.Find(query).Limit(1).Sort("{level:-1}").Project<GeocodeInfo>("{subject:1,city:1,street:1,building:1,vladenie:1,corpus:1,stroenie:1,soorugenie:1}").ToListAsync().ConfigureAwait(false);

                if (res.Count == 0) return null;
                return res[0];
            }
            catch
            {
                return null;
            }
        }
    }
}
