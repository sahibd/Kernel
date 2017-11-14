using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.Data.Mongo.Base;
using Tone.TelematicsKernel.Data.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo.GeoProviders
{
    public class SpeedLimitRepository : RepositoryBaseObjectId<ISpeedLimit, SpeedLimit>, ISpeedLimitRepository
    {
        public SpeedLimitRepository(IConnectionStringProvider connectionStringProvider)
            : base(Config.SpeedLimitCollection, connectionStringProvider.GeoConnectionString, 0)
        {
        }

        public async Task<int> GetSpeedLimit_ver2(double longitude, double latitude, int distance)
        {
            GeoJsonPoint<GeoJson2DGeographicCoordinates> gp = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude, latitude));
            var query = Builders<SpeedLimit>.Filter.Near("GeoInfo", gp, distance);
            try
            {
                var res = await Collection.Find(query).Limit(1).Project<SpeedLimit>("{mesh_id:1, link_id:1, speed_lmt:1}").ToListAsync().ConfigureAwait(false);
                if (res.Count == 0) return 0;
                return res[0].SpeedLmt;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetSpeedLimit(double longitude, double latitude, int distance)
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
                var res = await Collection.Find(match).Limit(1).Project<SpeedLimit>("{mesh_id:1, link_id:1, speed_lmt:1}").ToListAsync().ConfigureAwait(false);
                if (res.Count == 0) return 0;
                return res[0].SpeedLmt;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetSpeedLimit_ver3(double longitude, double latitude)
        {
            string s = "{ \"GeoInfo\" : { \"$near\" : { \"$geometry\" : { \"type\" : \"Point\", \"coordinates\" : [" + longitude.ToString(CultureInfo.InvariantCulture) + ", " + latitude.ToString(CultureInfo.InvariantCulture) + "] }, \"$maxDistance\" : 50 } } }";
            BsonDocument doc = BsonDocument.Parse(s);
            try
            {
                var res = await Collection.Find(doc).Limit(1).Project<SpeedLimit>("{mesh_id:1, link_id:1, speed_lmt:1}").ToListAsync().ConfigureAwait(false);
                if (res.Count == 0) return 0;
                return res[0].SpeedLmt;
            }
            catch
            {
                return 0;
            }
        }
    }
}