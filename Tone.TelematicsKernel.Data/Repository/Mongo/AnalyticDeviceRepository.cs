using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core.Data;
using Tone.Core.Provider;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Base.Extensions;
using Device = Tone.Data.Mongo.Model.Device;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class AnalyticDeviceRepository : RepositoryBaseObjectId<IDevice, Device>, IAnalyticDeviceRepository
    {
        private const string VirtualTrackerType = "VirtualTracker";

        public AnalyticDeviceRepository(IConnectionStringProvider provider)
            : base(Config.DeviceCollection, provider.ConnectionString)
        {
        }

        public override async Task<IDevice> Repsert(IDevice item)
        {
            await base.Repsert(item);
            return item;
        }

        /// <summary>
        /// Получить виртуальное телематическое устройство по полю Code
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        public async Task<IDevice> FindDeviceByCode(string deviceCode)
        {
            var device = await Collection.Find(d => d.Code == deviceCode).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        /// <summary>
        /// Получить виртуальное телематическое устройство по полю Imei
        /// </summary>
        /// <param name="imei"></param>
        /// <returns></returns>
        public async Task<IDevice> FindDeviceByImei(string imei)
        {
            var device = await Collection.Find(d => d.Imei == imei).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        public async Task<IDevice[]> GetByIds(IEnumerable<ObjectId> ids)
        {
            return await GetByIds(ids.Cast<object>().ToArray());
        }

        /// <summary>
        /// Получить список всех виртуальных телематических устройств в системе
        /// </summary>
        /// <returns></returns>
        public async Task<IDevice[]> GetAllVirtualTrackers()
        {
            var devices = await Collection.Find(d => d.Type == VirtualTrackerType).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }

        /// <summary>
        /// Получить виртуальное телематическое устройство в системе
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IDevice> GetVirtualTracker(object id)
        {
            var deviceId = id.ToNullableObjectId();
            if (deviceId == null) return null;

            var device = await Collection.Find(d => d.Id == deviceId && d.Type == VirtualTrackerType).FirstOrDefaultAsync();
            return device;
        }

        /// <summary>
        /// Получить список всех виртуальных телематических устройств в системе по Code
        /// </summary>
        /// <returns></returns>
        public async Task<IDevice[]> GetTrackersByCode(object id, string code)
        {
            var deviceId = id.ToNullableObjectId();
            if (deviceId == null) return null;

            var filter = Builders<Device>.Filter.And(Builders<Device>.Filter.Ne(d => d.Id, deviceId), Builders<Device>.Filter.Eq(d => d.Code, code));
            var devices = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }

        /// <summary>
        /// Получить список всех виртуальных телематических устройств в системе по Code
        /// </summary>
        /// <returns></returns>
        public async Task<IDevice[]> GetTrackersByCode(string code)
        {
            var filter = Builders<Device>.Filter.Eq(d => d.Code, code);
            var devices = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }

        public async Task<IDevice> FindVirtualTracker(object deviceId)
        {
            var id = (ObjectId)ToId(deviceId);
            var device = await Collection.Find(d => d.Id == id && d.Type == VirtualTrackerType).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        ///// <summary>
        ///// Получить список ТУ в системе по фильтру
        ///// </summary>
        ///// <param name="request"></param>
        ///// <param name="deviceIds"></param>
        ///// <param name="deviceIdsExclude"></param>
        ///// <returns></returns>
        ////public async Task<IDevice[]> GetDeviceByParam(DeviceFilterRequest request, List<object> deviceIds = null, bool deviceIdsExclude = false)
        ////{
        ////    if (request == null)
        ////        request = DeviceFilterRequest.GetDefaultFilter();

        ////    var name = request.NameFilter;
        ////    var skip = request.Skip;
        ////    var limit = request.Limit;

        ////    // Фильтрация по заданным идентификаторам
        ////    var filterDeviceIds = Builders<Device>.Filter.Where(x => true);
        ////    if (deviceIds != null && deviceIds.Any())
        ////    {
        ////        var keys = deviceIds.Cast<ObjectId?>().ToArray();
        ////        filterDeviceIds = deviceIdsExclude ? Builders<Device>.Filter.Nin(x => x.Id, keys) : Builders<Device>.Filter.In(x => x.Id, keys);
        ////    }

        ////    // Необходимо осуществлять в дополнении к другим полям фильтра фильтрацию - поиск по вхождению строки в следующие поля:
        ////    var filterName = Builders<Device>.Filter.Where(x => true);
        ////    if (!string.IsNullOrEmpty(name))
        ////    {
        ////        // Код
        ////        var filterCode = Builders<Device>.Filter.Where(r => r.Code.ToLower().Contains(name.ToLower()));
        ////        // Тип устройства
        ////        var filterType = Builders<Device>.Filter.Where(r => r.Type.ToLower().Contains(name.ToLower()));
        ////        // IMEI
        ////        var filterImei = Builders<Device>.Filter.Where(r => r.Imei.ToLower().Contains(name.ToLower()));
        ////        // Модель устройства
        ////        var filterModel = Builders<Device>.Filter.Where(r => r.Model.ToLower().Contains(name.ToLower()));
        ////        // Производитель устройства
        ////        var filterManufacturer =
        ////            Builders<Device>.Filter.Where(r => r.Manufacturer.ToLower().Contains(name.ToLower()));
        ////        // Sim Card
        ////        var filterSim = Builders<Device>.Filter.Where(r => r.SimCard.ToLower().Contains(name.ToLower()));

        ////        filterName = Builders<Device>.Filter.Or(filterCode, filterType, filterImei, filterModel, filterManufacturer, filterSim);
        ////    }

        ////    var filter = Builders<Device>.Filter.And(filterDeviceIds, filterName);
        ////    var devices = await Collection.Find(filter).Skip(skip).Limit(limit).ToListAsync();

        ////    return devices.Cast<IDevice>().OrderBy(r => r.Code).ToArray();
        ////}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<IDeviceMetadata> GetDeviceMetadata(string code)
        {
            var res = await Collection.Aggregate()
                .Match(x => x.Code == code)
                .Project<DeviceMetadata>(new BsonDocument
                {
                    {"_id", 0},
                    {"ProtocolVersion", "$DeviceMetadata.ProtocolVersion"},
                    {"Credentials", "$DeviceMetadata.Credentials"}
                })
                .FirstOrDefaultAsync().ConfigureAwait(false);

            /*
            var res2 = await Collection.Find(x => x.Code == code)
                .Project( x => new DeviceMetadata { ProtocolVersion = x.DeviceMetadata.ProtocolVersion})
                .FirstOrDefaultAsync().ConfigureAwait(false);
                */
            return res;
        }

        public async Task<string[]> GetDevicesNeedUpdate(DateTime serverTime)
        {
            try
            {
                //var res = await Collection.Find(Builders<Device>.Filter.Gt(x => x.DeviceMetadata.LastUpdate, serverTime.ToUniversalTime())).ToListAsync();
                var res = await Collection.AsQueryable().Where(x => x.DeviceMetadata.LastUpdate > serverTime).Select(x => new { x.Code, x.DeviceMetadata.LastUpdate }).ToListAsync();
                foreach (var t in res)
                {
                    Console.WriteLine($"Code: {t.Code} ServerTime: {serverTime} LastUpdateTime: {t.LastUpdate} Now: {DateTime.UtcNow}");
                }

                return res.Select(x => x.Code).ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task UpdateGeozoneEntrances(object deviceId, Dictionary<string, DateTime> geozonesEntrances)
        {
            var id = ToId(deviceId);
            var filterId = Builders<Device>.Filter.Where(r => r.Id.Equals(id));
            var update = Builders<Device>.Update.Set(d => d.GeozonesEntrances, geozonesEntrances);
            var result = await Collection.UpdateOneAsync(filterId, update);
        }

        public async Task<Dictionary<string, DateTime>> GetGeozoneEntrances(object deviceId)
        {
            var id = ToId(deviceId);
            var filterId = Builders<Device>.Filter.Where(r => r.Id.Equals(id));
            var fields = Builders<Device>.Projection.Include("GeozonesEntrances");
            var deviceProjection = await Collection.Find(filterId).Project<Device>(fields).FirstOrDefaultAsync();
            return deviceProjection?.GeozonesEntrances;
        }

        public async Task UpdateStopTime(object deviceId, DateTime? recordDeviceTime)
        {
            var id = ToId(deviceId);
            var filterId = Builders<Device>.Filter.Where(r => r.Id.Equals(id));
            var update = Builders<Device>.Update.Set(d => d.StopTime, recordDeviceTime);
            var result = await Collection.UpdateOneAsync(filterId, update);
        }

        public async Task<DateTime?> GetStopTime(object deviceId)
        {
            var id = ToId(deviceId);
            var filterId = Builders<Device>.Filter.Where(r => r.Id.Equals(id));
            var fields = Builders<Device>.Projection.Include("StopTime");
            var deviceProjection = await Collection.Find(filterId).Project<Device>(fields).FirstOrDefaultAsync();
            return deviceProjection?.StopTime;
        }

        public async Task<List<string>> GetAllDeviceCodes()
        {
            var devices = await Collection.Find(d => true).Project(x => x.Code).ToListAsync();
            return devices;
        }
    }
}