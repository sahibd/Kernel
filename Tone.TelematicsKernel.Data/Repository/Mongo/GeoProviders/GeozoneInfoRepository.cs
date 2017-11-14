using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.Data.Mongo.Base;
using Tone.TelematicsKernel.Data.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo.GeoProviders
{
    public class GeozoneInfoRepository : RepositoryBaseObjectId<IGeozoneInfo, GeozoneInfo>, IGeozoneInfoRepository
    {
        public GeozoneInfoRepository(IConnectionStringProvider connectionStringProvider) : base(Config.GeozoneCollection, connectionStringProvider.ConnectionString)
        {
        }

        public bool GeozoneIntersect(ObjectId _id, double? longitude, double? latitude)
        {
            var match = new BsonDocument
            {
                {"_id", _id},
                {
                    "GeoInfo",
                    new BsonDocument
                    {
                        {
                            "$geoIntersects",
                            new BsonDocument
                            {
                                {
                                    "$geometry", new BsonDocument
                                    {
                                        {"type", "Point"},
                                        {"coordinates", new BsonArray(new[] {longitude.Value, latitude.Value})}
                                    }
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                int res = (int)Collection.Find(match).Count();
                return (res != 0);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<object>> GetGeozoneInfo(double? longitude, double? latitude)
        {
            if (longitude == null || latitude == null)
                return new List<object>();

            var match = new BsonDocument
            {
                {
                    "GeoInfo",
                    new BsonDocument
                    {
                        {
                            "$geoIntersects",
                            new BsonDocument
                            {
                                {
                                    "$geometry", new BsonDocument
                                    {
                                        {"type", "Point"},
                                        {"coordinates", new BsonArray(new[] {longitude.Value, latitude.Value})}
                                    }
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                var res = await Collection.Find(match).Project<BsonDocument>("{_id:1}").ToListAsync();
                return res.Select(x => (object)x["_id"].AsObjectId).ToList();
            }
            catch (Exception)
            {
                return new List<object>();
            }
        }

        public bool GeozoneIntersect(ObjectId _id, double longitude, double latitude, int GeozoneDistance)
        {
            var match = new BsonDocument
                {
                    {"_id", _id },
                    { "GeoInfo",
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
                                                { "$maxDistance",  GeozoneDistance }
                                    }
                                }
                            }
                    }
                };

            try
            {
                int res = (int)Collection.Find(match).Count();
                return (res != 0);
            }
            catch
            {
                return false;
            }
        }

        public List<BsonDocument> GetGeozoneInfo(double longitude, double latitude, int GeozoneDistance)
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
                                                { "$maxDistance",  GeozoneDistance }
                                    }
                                }
                            }
                    }
                };

            try
            {
                var res = Collection.Find(match).Project<BsonDocument>("{_id:1}").ToList();
                return res;
            }
            catch
            {
                return new List<BsonDocument>();
            }
        }
    }
}