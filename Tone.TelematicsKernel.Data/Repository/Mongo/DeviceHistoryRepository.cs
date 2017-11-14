using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Data;
using Tone.Core.Enums;
using Tone.Core.Extensions;
using Tone.Core.Provider;
using Tone.Core.Subsystems.BusinessObjects.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class DeviceHistoryRepository : RepositoryBaseObjectId<IDeviceHistory, DeviceHistory>, IDeviceHistoryRepository
    {
        public DeviceHistoryRepository(IConnectionStringProvider provider)
            : base(Config.DeviceHistoryCollection, provider.ConnectionString)
        {
        }

        public async Task<List<IDeviceHistory>> GetByParams(ObjectId deviceId, int skip = 0, int limit = 0)
        {
            var deviceFilter = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, deviceId);
            var sort = Builders<DeviceHistory>.Sort.Descending(x => x.BoundToResourceTime);
            var result = (skip == 0 && limit == 0)
                ? await Collection.Find(deviceFilter).Sort(sort).ToListAsync()
                : await Collection.Find(deviceFilter).Sort(sort).Skip(skip).Limit(limit).ToListAsync();
            
            return result.ToList<IDeviceHistory>();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> BindDevice(object deviceId, object resourceId, RelatedResourceType resourceType)
        {
            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceId,
                ResourceId = resourceId,
                RelatedResourceType = resourceType,
                BoundToResourceTime = DateTime.UtcNow
            };

            var result = await Repsert(deviceHistory);
            return result;
        }

        public async Task<IDeviceHistory[]> UnbindDevice(object deviceId, object resourceId, RelatedResourceType resourceType)
        {
            var result = new List<IDeviceHistory>();
            var deviceHistoryList = await GetFullDeviceHistory(deviceId, resourceId, resourceType);

            foreach (var deviceHistory in deviceHistoryList)
            {
                if (deviceHistory.UnboundFromResourceTime == null)
                {
                    deviceHistory.UnboundFromResourceTime = DateTime.UtcNow;
                    var item = await Repsert(deviceHistory);
                    result.Add(item);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> GetBoundHistoryByDevice(object deviceId, object resourceId, RelatedResourceType? resourceType)
        {
            var objectDeviceId = ToId(deviceId);
            var objectResourceId = ToId(resourceId);
            var filterDevice = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, objectDeviceId);
            var filterResource = Builders<DeviceHistory>.Filter.Eq(r => r.ResourceId, objectResourceId);
            var filterType = resourceType != null ? Builders<DeviceHistory>.Filter.Eq(r => r.RelatedResourceType, resourceType) : Builders<DeviceHistory>.Filter.Where(r => true);
            var filterUnboundTime = Builders<DeviceHistory>.Filter.Eq(r => r.UnboundFromResourceTime, null);
            var filter = Builders<DeviceHistory>.Filter.And(filterDevice, filterResource, filterType, filterUnboundTime);

            var result = await Collection.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        public async Task<IDeviceHistory[]> GetDeviceHistoryByDevice(object deviceId)
        {
            var objectDeviceId = ToId(deviceId);
            var filter = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, objectDeviceId);
            var result = await Collection.Find(filter).ToListAsync();
            return result.Cast<IDeviceHistory>().ToArray();
        }


        /// <summary>
        /// Изменяем UnboundFromResourceTime
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory[]> UnboundDeviceHistory(object deviceId)
        {
            var result = new List<IDeviceHistory>();
            var deviceHistories = await GetDeviceHistoryByDevice(deviceId);
            foreach (var history in deviceHistories.Where(d => d.UnboundFromResourceTime == null))
            {
                history.UnboundFromResourceTime = DateTime.UtcNow;
                var deviseHistory = await Repsert(history);
                result.Add(deviseHistory);
            }

            return result.Cast<IDeviceHistory>().ToArray();
        }

        public async Task<IDeviceHistory[]> GetByResource(object resourceId, CustomerRepositoryAccess customerRepositoryAccess = CustomerRepositoryAccess.RequestContextCustomer)
        {
            var objectResourceId = ToId(resourceId);
            var result = await Collection.Find(x => x.ResourceId == objectResourceId).ToListAsync();
            return result.Cast<IDeviceHistory>().ToArray();
        }

        public async Task<IDeviceHistory[]> GetByResource(object resourceId, DateTime from, DateTime? to = null, CustomerRepositoryAccess customerRepositoryAccess = CustomerRepositoryAccess.RequestContextCustomer)
        {
            var objectResourceId = ToId(resourceId);            
            if (objectResourceId == null)
                return null;

            if (to != null && to.Value < from)
                return null;
            
            var filterResourceId = Builders<DeviceHistory>.Filter.Eq(dh => dh.ResourceId, objectResourceId);
            
            var filterFromNotNull = Builders<DeviceHistory>.Filter.Gt(dh => dh.UnboundFromResourceTime, from);
            var filterFromNull = Builders<DeviceHistory>.Filter.Eq(dh => dh.UnboundFromResourceTime, null);
            var filterFrom = Builders<DeviceHistory>.Filter.Or(filterFromNotNull, filterFromNull);

            var filterTo = to != null ? Builders<DeviceHistory>.Filter.Lt(dh => dh.BoundToResourceTime, to.Value) : Builders<DeviceHistory>.Filter.Empty;

            var filter = Builders<DeviceHistory>.Filter.And(filterResourceId, filterFrom, filterTo);
            
            var result = await Collection.Find(filter).ToListAsync();
            return result.Cast<IDeviceHistory>().ToArray();
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="skip"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public async Task<List<IDeviceHistory>> GetUnboundDeviceHistory(ObjectId deviceId, int skip = 0, int limit = 0)
        {
            var filter = Builders<DeviceHistory>.Filter.And(Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, deviceId), Builders<DeviceHistory>.Filter.Ne(r => r.UnboundFromResourceTime, null));
            var sort = Builders<DeviceHistory>.Sort.Descending(x => x.BoundToResourceTime);
            var allHistory = await Collection.Find(filter).Sort(sort).ToListAsync();

            var result = new List<IDeviceHistory>();
            foreach (var deviceHistory in allHistory)
            {
                if (deviceHistory.ResourceId == null || result.Any(x => x.ResourceId.ToString() == deviceHistory.ResourceId.ToString()))
                    continue;
                result.Add(deviceHistory);
            }

            return (skip == 0 && limit == 0) ? result.Skip(0).Take(200).ToList() : result.Skip(skip).Take(limit).ToList();
        }

       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> GetCurrentHistoryByDeviceAndResource(object deviceId, object resourceId)
        {
            var objectDeviceId = ToId(deviceId);
            var objectResourceId = ToId(resourceId);
            var filterDevice = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, objectDeviceId);
            var filterResource = Builders<DeviceHistory>.Filter.Eq(r => r.ResourceId, objectResourceId);
            var filterDate = Builders<DeviceHistory>.Filter.Where(r => r.UnboundFromResourceTime == null);
            var filter = Builders<DeviceHistory>.Filter.And(filterDevice, filterResource, filterDate);

            var result = await Collection.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        public async Task<IDeviceHistory[]> GetByResources(List<object> resourcesIds,
            CustomerRepositoryAccess customerRepositoryAccess = CustomerRepositoryAccess.RequestContextCustomer)
        {
            var ids = resourcesIds.Select(ToId);
            var filterId = Builders<DeviceHistory>.Filter.In(r => r.ResourceId, ids);
            var result = await Collection.Find(filterId).ToListAsync();
            return result.Cast<IDeviceHistory>().ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory[]> GetFullDeviceHistory(object deviceId, object resourceId, RelatedResourceType? resourceType)
        {
            var objectDeviceId = ToId(deviceId);
            var objectResourceId = ToId(resourceId);
            var filterDevice = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, objectDeviceId);
            var filterResource = Builders<DeviceHistory>.Filter.Eq(r => r.ResourceId, objectResourceId);
            var filterType = resourceType != null ? Builders<DeviceHistory>.Filter.Eq(r => r.RelatedResourceType, resourceType) : Builders<DeviceHistory>.Filter.Where(r => true);
            var filter = Builders<DeviceHistory>.Filter.And(filterDevice, filterResource, filterType);

            var result = await Collection.Find(filter).ToListAsync();
            return result.Cast<IDeviceHistory>().ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> GetCurrentDeviceHistoryByDeviceId(object deviceId, RelatedResourceType? resourceType)
        {
            var objectDeviceId = ToId(deviceId);
            var filterDevice = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, objectDeviceId);
            var filterType = resourceType != null ? Builders<DeviceHistory>.Filter.Eq(r => r.RelatedResourceType, resourceType) : Builders<DeviceHistory>.Filter.Where(r => true);
            var filterUnboundTime = Builders<DeviceHistory>.Filter.Eq(r => r.UnboundFromResourceTime, null);
            var filter = Builders<DeviceHistory>.Filter.And(filterDevice, filterType, filterUnboundTime);

            var result = await Collection.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> GetCurrentDeviceHistoryByResourceId(object resourceId, RelatedResourceType? resourceType)
        {
            var objectResourceId = ToId(resourceId);
            var filterDevice = Builders<DeviceHistory>.Filter.Eq(r => r.ResourceId, objectResourceId);
            var filterType = resourceType != null ? Builders<DeviceHistory>.Filter.Eq(r => r.RelatedResourceType, resourceType) : Builders<DeviceHistory>.Filter.Where(r => true);
            var filterUnboundTime = Builders<DeviceHistory>.Filter.Eq(r => r.UnboundFromResourceTime, null);
            var filter = Builders<DeviceHistory>.Filter.And(filterDevice, filterType, filterUnboundTime);

            var result = await Collection.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vehicles"></param>
        /// <param name="employees"></param>
        /// <param name="isBound"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory[]> ChangeBoundHistoryByResorces(IVehicle[] vehicles, IEmployee[] employees, bool isBound)
        {
            var result = new List<IDeviceHistory>();

            foreach (var vehicle in vehicles)
            {
                if (isBound)
                    result.Add(await ChangeBoundHistoryByResorce(vehicle.DeviceId, vehicle.Id, RelatedResourceType.Vehicle));
                else
                    result.AddRange(await UnbindDeviceHistoryByResorce(vehicle.Id));
            }

            foreach (var employee in employees)
            {
                if (isBound)
                    result.Add(await ChangeBoundHistoryByResorce(employee.DeviceId, employee.Id, RelatedResourceType.Employee));
                else
                    result.AddRange(await UnbindDeviceHistoryByResorce(employee.Id));
            }

            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> ChangeBoundHistoryByResorce(object deviceId, object resourceId, RelatedResourceType resourceType)
        {
            var currentHistory = await GetCurrentHistoryByDeviceAndResource(deviceId, resourceId);
            if (currentHistory != null)
                return currentHistory;

            // Отвязываем предыдущие записи в истории
            await UnboundDeviceHistory(deviceId);

            // Добавляем новую запись в историю
            var newDeviceHistory = new DeviceHistory()
            {
                DeviceId = deviceId,
                ResourceId = resourceId,
                RelatedResourceType = resourceType,
                BoundToResourceTime = DateTime.UtcNow
            };
            await Repsert(newDeviceHistory);

            return newDeviceHistory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory> CheckHistoryByDeviceAndResource(object deviceId, object resourceId)
        {
            var objectDeviceId = ToId(deviceId);
            var objectResourceId = ToId(resourceId);
            var filterDevice = Builders<DeviceHistory>.Filter.Eq(r => r.DeviceId, objectDeviceId);
            var filterResource = Builders<DeviceHistory>.Filter.Ne(r => r.ResourceId, objectResourceId);
            var filterDate = Builders<DeviceHistory>.Filter.Where(r => r.UnboundFromResourceTime == null);
            var filter = Builders<DeviceHistory>.Filter.And(filterDevice, filterResource, filterDate);

            var result = await Collection.Find(filter).FirstOrDefaultAsync();
            return result;
        }

        /// <summary>
        /// Изменяем UnboundFromResourceTime
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        public async Task<IDeviceHistory[]> UnbindDeviceHistoryByResorce(object resourceId)
        {
            var result = new List<IDeviceHistory>();
            var histories = (await GetByResource(resourceId, CustomerRepositoryAccess.Global)).Where(h => h.UnboundFromResourceTime == null).ToList();
            foreach (var history in histories)
            {   
                history.UnboundFromResourceTime = DateTime.UtcNow;
                var deviseHistory = await Repsert(history);
                result.Add(deviseHistory);
            }

            return result.ToArray();
        }
    }
}
