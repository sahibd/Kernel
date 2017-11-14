using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class SafetyInfoRepository : RepositoryBaseObjectId<ISafetyInfo, SafetyInfo>, ISafetyInfoRepository
    {
        public SafetyInfoRepository(IConnectionStringProvider connectionStringProvider) : base(Config.SafetyInfoCollection, connectionStringProvider.GeoConnectionString, 0)
        {
        }

        public async Task<List<ISafetyInfo>> GetSafetyInfo(double longitude, double latitude, int distance)
        {
            var match = new BsonDocument
            {
                {
                    "GeoInfo",
                    new BsonDocument
                    {
                        {
                            "$near",
                            new BsonDocument
                            {
                                {
                                    "$geometry", new BsonDocument
                                    {
                                        {"type", "Point"},
                                        {"coordinates", new BsonArray(new[] {longitude, latitude})}
                                    }
                                },
                                {"$maxDistance", distance}
                            }
                        }
                    }
                }
            };

            try
            {
                var result = await Collection.Find(match).Project<SafetyInfo>(new BsonDocument { { "_id", 0 }, { "SСode", "$s_code" }, { "SpeedLmt", "$speed_lmt" } }).ToListAsync(); //.ToListAsync().ConfigureAwait(false);
                return result.ToList<ISafetyInfo>();
            }
            catch(Exception)
            {
                return null;
            }
        }

        public async Task<int> GetSafetyInfoCount(double longitude, double latitude, int distance)
        {
            var match = new BsonDocument
            {
                {
                    "GeoInfo",
                    new BsonDocument
                    {
                        {
                            "$near",
                            new BsonDocument
                            {
                                {
                                    "$geometry", new BsonDocument
                                    {
                                        {"type", "Point"},
                                        {"coordinates", new BsonArray(new[] {longitude, latitude})}
                                    }
                                },
                                {"$maxDistance", distance}
                            }
                        }
                    }
                }
            };
            try
            {
                return (int)await Collection.Find(match).CountAsync().ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetSafetyInfoCount_ver3(double longitude, double latitude, int distance)
        {
            string s = "{ \"GeoInfo\" : { \"$near\" : { \"$geometry\" : { \"type\" : \"Point\", \"coordinates\" : [" +
                       longitude.ToString(CultureInfo.InvariantCulture) + ", " +
                       latitude.ToString(CultureInfo.InvariantCulture) + "] }, \"$maxDistance\" : " + distance + " } } }";
            BsonDocument doc = BsonDocument.Parse(s);
            try
            {
                return (int)await Collection.Find(doc).CountAsync().ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }
        }

        //этот метод надо проверить
        public async Task<int> GetSafetyInfoCount_ver2(double longitude, double latitude, int distance)
        {
            GeoJsonPoint<GeoJson2DGeographicCoordinates> gp =
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude, latitude));

            var query = Builders<SafetyInfo>.Filter.Near("GeoInfo", gp, distance);
            try
            {
                return (int)await Collection.Find(query).CountAsync().ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }

        }
    }
}
