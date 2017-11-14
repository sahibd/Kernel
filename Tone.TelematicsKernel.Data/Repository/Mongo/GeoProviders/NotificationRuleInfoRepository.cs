using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.Data.Mongo.Base;
using Tone.TelematicsKernel.Data.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo.GeoProviders
{

    /// <summary>
    /// Для работы с данным классом нужна версия MongoDb НЕ НИЖЕ 3.2/////////////////////////////////////////////////////////
    /// </summary>
    
    class NotificationRuleInfoRepository : RepositoryBaseObjectId<INotificationRuleInfo, NotificationRuleInfo>,
        INotificationRuleInfoRepository
    {
        public NotificationRuleInfoRepository(IConnectionStringProvider connectionStringProvider)
            : base(Config.NotificationRuleCollection, connectionStringProvider.ConnectionString)
        {
        }

        public List<INotificationRuleInfo> GetRulesWithDevices()
        {
            #region Тело запроса на всякий случай

            /* 
                   db.NotificationRules.aggregate([
                        
            // Фильтр по Enabled
                                            { "$match": {
                        "Enabled": { "$eq" : true}
                    }},

                   // Распаковываем ResourcesFilter.Include так, что в каждом документе оказывается только 1 идентификатор (документы копируются)
                   { "$unwind": { 
                     "path": "$ResourcesFilter.Include"
                   }},
                            
                   // Объединение с коллекциями ресурсов
                   {"$lookup":
                      {
                        from: "Vehicles",
                        localField: "ResourcesFilter.Include",
                        foreignField: "_id",
                        as: "Vehicles"
                      }
                   },	
                   {
                    "$lookup":
                      {
                        from: "Employees",
                        localField: "ResourcesFilter.Include",
                        foreignField: "_id",
                        as: "Employees"
                      }
                   },
                            
                   // Делаем так, чтоб было одно поле Resources, а не два - Vehicles и Employees
                   {"$project":   {
                           "Resources": { "$cond": { 
                             if: { $eq: [ { $size: "$Vehicles" }, 0 ] },
                             then: "$Employees", 
                             else: "$Vehicles" } },
                           "Rule": "$$ROOT"
                   }},
                            
                   // Распаковываем Resources так, что в каждом документе оказывается только 1 идентификатор (документы копируются)
                   { "$unwind": {
                     "path": "$Resources" 
                   }},
                        
                   // Формируем документ только с теми полями, которые нужны
                   {"$project":   {
                           "Id": "$Rule.Id",
                           "CustomerId": "$Rule.CustomerId",
                           "DeviceId": "$Resources.DeviceId",
                           "ResourceId": "$Recources._id",
                           "DaysOfWeek": "$Rule.DaysOfWeek",
                           "TimeFrom": "$Rule.TimeFrom",
                           "TimeTo": "$Rule.TimeTo",
                           "Title": "$Rule.Title",
                           "IsImportant": "$Rule.IsImportant",
                           "ShowOnDisplay": "$Rule.ShowOnDisplay",
                           "RoadEventType": "$Rule.RoadEventType",
                           "RoadEventArguments": "$Rule.RoadEventArguments"
                    }},

                    { "$match": {
                        "DeviceId": { "$exists" : true}
                    }}
                    ])
                    */

            #endregion

            var enabledFilter = new BsonDocument
            {
                {
                    "$match", new BsonDocument
                    {
                        {
                            "Enabled", new BsonDocument {{ "$eq", true}} 
                        }
                    }
                }
            };

            // Распаковываем ResourcesFilter.Include так, что в каждом документе оказывается только 1 идентификатор (документы копируются)
            var unwind1 = new BsonDocument
            {
                {
                    "$unwind", new BsonDocument
                    {
                        {"path", "$ResourcesFilter.Include"}
                    }
                }
            };

            // Объединение с коллекциями ресурсов
            var lookupWithVehicles =
                new BsonDocument
                {
                    {
                        "$lookup",
                        new BsonDocument
                        {
                            {"from", "Vehicles"},
                            {"localField", "ResourcesFilter.Include"},
                            {"foreignField", "_id"},
                            {"as", "Vehicles"}
                        }
                    }
                };
            var lookupWithEmployees =
                new BsonDocument
                {
                    {
                        "$lookup",
                        new BsonDocument
                        {
                            {"from", "Employees"},
                            {"localField", "ResourcesFilter.Include"},
                            {"foreignField", "_id"},
                            {"as", "Employees"}
                        }
                    }
                };

            // Делаем так, чтоб было одно поле Resources, а не два - Vehicles и Employees
            var project1 = new BsonDocument
            {
                {
                    "$project", new BsonDocument
                    {
                        {
                            "Resources", new BsonDocument
                            {
                                {
                                    "$cond", new BsonDocument
                                    {
                                        {
                                            "if", new BsonDocument
                                            {
                                                {
                                                    "$eq", new BsonArray {new BsonDocument {{"$size", "$Vehicles"}}, 0}
                                                }
                                            }
                                        },
                                        {"then", "$Employees"},
                                        {"else", "$Vehicles"},
                                    }
                                }

                            }
                        },
                        {"Rule", "$$ROOT"}

                    }
                }
            };

            // Распаковываем Resources так, что в каждом документе оказывается только 1 идентификатор (документы копируются)
            var unwind2 = new BsonDocument
            {
                {
                    "$unwind", new BsonDocument
                    {
                        {"path", "$Resources"}
                    }
                }
            };

            // Формируем документ только с теми полями, которые нужны
            var project2 = new BsonDocument
            {
                {
                    "$project",
                    new BsonDocument
                    {
                        {"Id", "$Rule.Id"},
                        {"CustomerId", "$Rule.CustomerId"},
                        {"DeviceId", "$Resources.DeviceId"},
                        {"ResourceId", "$Resources._id"},
                        {"DaysOfWeek", "$Rule.DaysOfWeek"},
                        {"TimeFrom", "$Rule.TimeFrom"},
                        {"TimeTo", "$Rule.TimeTo"},
                        {"Title", "$Rule.Title"},
                        {"IsImportant", "$Rule.IsImportant"},
                        {"ShowOnDisplay", "$Rule.ShowOnDisplay"},
                        {"RoadEventType", "$Rule.RoadEventType"},
                        {"RoadEventArguments", "$Rule.RoadEventArguments"}
                    }
                }
            };

            var deviceIdFilter = new BsonDocument
            {
                {
                    "$match", new BsonDocument
                    {
                        {
                            "DeviceId", new BsonDocument {{ "$exists", true}}
                        }
                    }
                }
            };

            var pipeline = new[] { enabledFilter, unwind1, lookupWithVehicles, lookupWithEmployees, project1, unwind2, project2, deviceIdFilter };
            var queryResult = Collection.Aggregate<NotificationRuleInfo>(pipeline).ToList();

            return queryResult.Select(x => (INotificationRuleInfo) x).ToList();
        }
    }
}
