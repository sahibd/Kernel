using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core.AnalyticSystem.Model.Base;
using Tone.Core.Data;
using Tone.Core.Data.Constants;
using Tone.Core.Enums;
using Tone.Pagination;
using Tone.Core.Provider;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.BusinessObjects;
using Tone.Core.Subsystems.BusinessObjects.Model;
using Tone.Core.Subsystems.Customers;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Base.Extensions;
using Device = Tone.Data.Mongo.Model.Device;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class DeviceRepository : CustomerRepositoryBase<IDevice, Device>, IDeviceRepository
    {
        private const string VirtualTrackerType = "VirtualTracker";

        public DeviceRepository(IConnectionStringProvider provider)
            : base(Config.DeviceCollection, provider.ConnectionString)
        {
        }

        public override async Task<IDevice> Repsert(IDevice item)
        {
            await base.Repsert(item);
            return item;
        }

        /// <summary>
        /// Получить ТМУ в системе по полю Code
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        public async Task<IDevice> FindDeviceByCode(string deviceCode)
        {
            var device = await Collection.Find(d => d.Code == deviceCode && d.CustomerId == CustomerId).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        /// <summary>
        /// Получить ТМУ в системе по полю IMEI
        /// </summary>
        /// <param name="imei"></param>
        /// <returns></returns>
        public async Task<IDevice> FindDeviceByImei(string imei)
        {
            var device = await Collection.Find(d => d.CustomerId == CustomerId && d.Imei == imei).FirstOrDefaultAsync().ConfigureAwait(false);
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
            var devices = await Collection.Find(d => d.CustomerId == CustomerId && d.Type == VirtualTrackerType).ToListAsync().ConfigureAwait(false);
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
            if (deviceId == null)
                return null;

            var device = await Collection.Find(d => d.Id == deviceId && d.Type == VirtualTrackerType && d.CustomerId == CustomerId).FirstOrDefaultAsync();
            return device;
        }

        /// <summary>
        /// Получить список всех виртуальных телематических устройств в системе по Code
        /// </summary>
        /// <returns></returns>
        public async Task<IDevice[]> GetTrackersByCode(object id, string code)
        {
            var deviceId = id.ToNullableObjectId();
            if (deviceId == null)
                return null;

            var filterCustomer = BuildCustomerFilter();
            var filter = Builders<Device>.Filter.And(Builders<Device>.Filter.Ne(d => d.Id, deviceId), Builders<Device>.Filter.Eq(d => d.Code, code), filterCustomer);
            var devices = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }

        /// <summary>
        /// Получить список всех виртуальных телематических устройств в системе по Code
        /// </summary>
        /// <returns></returns>
        public async Task<IDevice[]> GetTrackersByCode(string code)
        {
            var filterCustomer = BuildCustomerFilter();
            var filterCode = Builders<Device>.Filter.Eq(d => d.Code, code);
            var filter = Builders<Device>.Filter.And(filterCode, filterCustomer);
            var devices = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }

        public async Task<IDevice> FindVirtualTracker(object deviceId)
        {
            var id = (ObjectId) ToId(deviceId);
            var device = await Collection.Find(d => d.Id == id && d.Type == VirtualTrackerType && d.CustomerId == CustomerId).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        /// <summary>
        /// Получить список ТУ в системе по фильтру
        /// </summary>
        /// <param name="request"></param>
        /// <param name="excludeDeviceIds">Устройства удаляющиеся из поиска</param>
        /// <param name="includeDeviceIds">Устройства добавляющиеся в поиск</param>
        /// <param name="addDeviceIds">Устройства добавляющиеся в выдачу</param>
        /// <returns></returns>
        public async Task<PlatformPaginationResponse<IDevice>> GetDeviceByParam(PlatformPaginationRequest request,
            List<object> excludeDeviceIds = null, List<object> includeDeviceIds = null, List<object> addDeviceIds = null)
        {
            // Customer Filter
            var filterCustomer = BuildCustomerFilter();

            // Name Filter
            var filterName = BuildFilterByName(request.SearchFilter);

            // Фильтрация по заданным идентификаторам
            var filterDeviceIds = Builders<Device>.Filter.Where(x => true);

            if (excludeDeviceIds != null && excludeDeviceIds.Any())
            {
                var keys = excludeDeviceIds.Cast<ObjectId?>().ToArray();
                filterDeviceIds = Builders<Device>.Filter.And(filterDeviceIds, Builders<Device>.Filter.Nin(x => x.Id, keys));
            }

            if (includeDeviceIds != null && includeDeviceIds.Any())
            {
                var keys = includeDeviceIds.Cast<ObjectId?>().ToArray();
                filterDeviceIds = Builders<Device>.Filter.And(filterDeviceIds, Builders<Device>.Filter.In(x => x.Id, keys));
            }

            // Фильтрация по идентификаторам добавляемым в выборку
            var filterAddDeviceIds = Builders<Device>.Filter.Where(x => false);
            if (addDeviceIds != null && addDeviceIds.Any())
            {
                var keys = addDeviceIds.Cast<ObjectId?>().ToArray();
                filterAddDeviceIds = Builders<Device>.Filter.And(filterCustomer, Builders<Device>.Filter.In(x => x.Id, keys));
            }

            var filter1 = Builders<Device>.Filter.And(filterCustomer, filterDeviceIds, filterName);
            var filter = Builders<Device>.Filter.Or(filter1, filterAddDeviceIds);
            var fields = Builders<Device>.Projection
                .Exclude("Commands")
                .Exclude("GeozonesEntrances")
                .Exclude("DeviceMetadata")
                .Exclude("ExtendedInfo")
                .Exclude("DeviceMetadata")
                .Exclude("CurrentExecutions")
                .Exclude("LastExecuted")
                .Exclude("FuelSetting")
                .Exclude("PriorityFieldsConfiguration")
                .Exclude("StopTime")
                .Exclude("UpdateDate")
                .Exclude("UpdateAccountId")
                .Exclude("Parameters")
                .Exclude("CalibrationSettings")
                .Exclude("ParametersViewSettings")
                .Exclude("BoolCalibrationSettings")
                .Exclude("MappingConfiguration")
                .Exclude("MappingSettings");
            
            var query = Collection.Find(filter);

            if (!string.IsNullOrWhiteSpace(request.OrderBy))
            {
                var sort = PlatformPaginationBuilder.Sort<Device>(request);
                query = query.Sort(sort);
            }

            var totalItem = await query.CountAsync().ConfigureAwait(false);
            var result = await query.Project<Device>(fields).Skip(request.Skip).Limit(request.Limit).ToListAsync().ConfigureAwait(false);

            var pagesMetadata = request.GetPaginationMetadata(totalItem);
            return new PlatformPaginationResponse<IDevice>(result.Cast<IDevice>().ToArray(), pagesMetadata);
        }

        public async Task<IDeviceMetadata> GetDeviceMetadata(string code)
        {
            var res = await Collection.Aggregate()
                .Match(x => x.Code == code && x.CustomerId == CustomerId)
                .Project<DeviceMetadata>(new BsonDocument
                {
                    {"_id", 0},
                    {"ProtocolVersion", "$DeviceMetadata.ProtocolVersion"},
                    {"Credentials", "$DeviceMetadata.Credentials"}
                })
                .FirstOrDefaultAsync().ConfigureAwait(false);

            return res;
        }

        public async Task<string[]> GetDevicesNeedUpdate(DateTime serverTime)
        {
            try
            {
                var res =
                    await Collection.AsQueryable()
                        .Where(x => x.DeviceMetadata.LastUpdate > serverTime && x.CustomerId == CustomerId)
                        .Select(x => new {x.Code, x.DeviceMetadata.LastUpdate})
                        .ToListAsync();
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

        private FilterDefinition<Device> BuildCustomerFilter() => Builders<Device>.Filter.Eq(d => d.CustomerId, CustomerId);

        //public async Task<IDevice[]> GetDevicesForCustomers(DeviceCustomerRequest request)
        //{
        //    if (request == null)
        //        request = DeviceCustomerRequest.GetDefaultFilter();

        //    var filterName = BuildFilterByName(request.NameFilter);
        //    var filterRelation = BuildFilterRelation(request.Status);
        //    var filterCustomer = BuildFilterCustomer(request.CustomerId);
        //    var filter = Builders<Device>.Filter.And(filterCustomer, filterRelation, filterName);

        //    var devices = await Collection.Find(filter).Skip(request.Skip).Limit(request.Limit).ToListAsync();
        //    return devices.Cast<IDevice>().ToArray();
        //}

        /// <summary>
        /// Получение Device без фильтра по Account.CustomerId
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IDevice> GetDeviceById(object id)
        {
            var deviceId = (ObjectId) ToId(id);
            var device = await Collection.Find(d => d.Id == deviceId).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        /// <summary>
        /// Заменить одну запись
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<IDevice> UpdateDevice(IDevice device)
        {
            if (device == null)
                return null;

            var id = ToId(device.Id);
            var filter = Builders<Device>.Filter.Eq(d => d.Id, id);
            var curentDevice = (Device) device;

            await Collection.ReplaceOneAsync(filter, curentDevice, new UpdateOptions {IsUpsert = true}).ConfigureAwait(false);
            return device;
        }

        #region Build Filters

        private FilterDefinition<Device> BuildFilterByName(string name)
        {
            // Необходимо осуществлять в дополнении к другим полям фильтра фильтрацию - поиск по вхождению строки в следующие поля:
            if (string.IsNullOrEmpty(name))
                return Builders<Device>.Filter.Where(x => true);

            // Код
            var filterCode = Builders<Device>.Filter.Where(r => r.Code.ToLower().Contains(name.ToLower()));
            // Тип устройства
            var filterType = Builders<Device>.Filter.Where(r => r.Type.ToLower().Contains(name.ToLower()));
            // IMEI
            var filterImei = Builders<Device>.Filter.Where(r => r.Imei.ToLower().Contains(name.ToLower()));
            // Модель устройства
            var filterModel = Builders<Device>.Filter.Where(r => r.Model.ToLower().Contains(name.ToLower()));
            // Производитель устройства
            var filterManufacturer = Builders<Device>.Filter.Where(r => r.Manufacturer.ToLower().Contains(name.ToLower()));
            // Sim Card
            var filterSim = Builders<Device>.Filter.Where(r => r.SimCard.ToLower().Contains(name.ToLower()));
            // Result Filter
            return Builders<Device>.Filter.Or(filterCode, filterType, filterImei, filterModel, filterManufacturer, filterSim);
        }

        private FilterDefinition<Device> BuildFilterRelation(RelationStatus? status)
        {
            if (status == null || status == RelationStatus.All)
                return Builders<Device>.Filter.Where(x => true);

            var filter = status == RelationStatus.Related
                ? Builders<Device>.Filter.Where(x => x.CustomerId != null && x.CustomerId != MongoObjectId.EmptyObjectId)
                : Builders<Device>.Filter.Where(x => x.CustomerId == null || x.CustomerId == MongoObjectId.EmptyObjectId);

            return filter;
        }

        private FilterDefinition<Device> BuildFilterCustomer(object customerId)
        {
            if (customerId==null || string.IsNullOrEmpty(customerId.ToString()))
                return Builders<Device>.Filter.Where(x => true);

            var id = ToId(customerId);
            return Builders<Device>.Filter.Eq(x => x.CustomerId, id);
        }

        #endregion Build Filters

        public async Task<IDevice[]> UpdateMultipleDeviceCustomerBinding(object customerId, IDevice[] devices, bool isBind)
        {
            var result = new List<IDevice>();
            foreach (var device in devices)
            {
                if (isBind)
                    device.CustomerId = (ObjectId) customerId;
                else
                    device.CustomerId = MongoObjectId.EmptyObjectId;

                var updatedDevice = await UpdateDevice(device);
                result.Add(updatedDevice);
            }

            return result.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IDevice[]> GetDevicesByIds(object[] ids)
        {
            var keys = ids.Select(x => ToId(x)).ToArray();
            var filter = Builders<Device>.Filter.In(x => x.Id, keys);
            var result = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
            return result.Cast<IDevice>().ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        public async Task<CustomerAccountDevicesViewModel> GetCustomerDevicesViewModel(object customerId)
        {
            var id = (ObjectId) customerId;
            var filterTotal = Builders<Device>.Filter.Where(r => true);
            var filterCustomer = Builders<Device>.Filter.Eq(r => r.CustomerId, id);

            var total = await Collection.Find(filterTotal).CountAsync();
            var customers = await Collection.Find(filterCustomer).CountAsync();
            return new CustomerAccountDevicesViewModel() {Total = total, Customer = customers};
        }

        /// <summary>
        /// Получение Device без фильтра по Account.CustomerId
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<IDevice> GetDeviceWithCustomerById(object deviceId)
        {
            var id = (ObjectId) ToId(deviceId);
            var filterCustomer = BuildCustomerFilter();
            var filterId = Builders<Device>.Filter.Eq(r => r.Id, id);
            var filter = Builders<Device>.Filter.And(filterCustomer, filterId);

            var device = await Collection.Find(filter).FirstOrDefaultAsync().ConfigureAwait(false);
            return device;
        }

        /// <summary>
        /// Получение объекта по идентификатору для Админки
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<IDevice> AdminGetDeviceById(object deviceId)
        {
            if (deviceId == null)
                return null;

            var id = ToId(deviceId);
            var result = await Collection.Find(c => c.Id.Equals(id)).FirstOrDefaultAsync().ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Модуль тарировки и фильтрации топливных данных
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<FuelSetting> GetDeviceFuelSetting(object deviceId)
        {
            var device = await AdminGetDeviceById(deviceId);
            return device?.FuelSetting;
        }


        /// <summary>
        /// Получение фильтра
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<List<FuelRule<object>>> GetDeviceFuelRules(object deviceId)
        {
            var device = await AdminGetDeviceById(deviceId);
            return device?.FuelSetting?.FuelRules;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceCode"></param>
        /// <param name="sensorMappingConfigurations"></param>
        /// <returns></returns>
        public async Task UpdateDeviceConfig(string deviceCode, IEnumerable<SensorMappingConfiguration> sensorMappingConfigurations)
        {
            var filter = Builders<Device>.Filter.Eq(x => x.Code, deviceCode);
            var update = Builders<Device>.Update.Combine(
                Builders<Device>.Update.Set(x => x.UpdateDate, DateTime.UtcNow),
                Builders<Device>.Update.Set(x => x.MappingConfiguration, sensorMappingConfigurations)
            );
            await Collection.UpdateOneAsync(filter, update);
        }

        /// <summary>
        /// Получение Тарировки
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<List<FuelSource>> GetDeviceFuelSources(object deviceId)
        {
            var device = await AdminGetDeviceById(deviceId);
            return device?.FuelSetting?.FuelSources;
        }

        /// <summary>
        /// Получить список ТУ в системе - Admin
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paginationRequest"></param>
        /// <returns></returns>
        public async Task<PlatformPaginationResponse<IDevice>> AdminGetDevices(DeviceCustomerRequest request, PlatformPaginationRequest paginationRequest)
        {
            // Customer Filter
            var currentCustomerId = BuildFilterCustomer(request.CustomerId);
            // Name Filter
            var filterName = BuildFilterByName(paginationRequest.SearchFilter);
            // Relation Filter
            var filterRelation = BuildFilterRelation(request.Status);
            // Filter
            var filter = Builders<Device>.Filter.And(currentCustomerId, filterRelation, filterName);

            var query = Collection.Find(filter);

            if (!string.IsNullOrWhiteSpace(paginationRequest.OrderBy))
            {
                var sort = PlatformPaginationBuilder.Sort<Device>(paginationRequest);
                query = query.Sort(sort);
            }

            var totalItem = await query.CountAsync().ConfigureAwait(false);
            var result = await query.Skip(paginationRequest.Skip).Limit(paginationRequest.Limit).ToListAsync().ConfigureAwait(false);

            var pagesMetadata = paginationRequest.GetPaginationMetadata(totalItem);
            return new PlatformPaginationResponse<IDevice>(result.Cast<IDevice>().ToArray(), pagesMetadata);
        }


        public async Task AdminDelete(object deviceId)
        {
            var id = (ObjectId?)ToId(deviceId);
           var result= await Collection.DeleteOneAsync(x => x.Id == id).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vehicles"></param>
        /// <param name="employees"></param>
        /// <returns></returns>
        public async Task<IDevice[]> SetDeviceResources(IVehicle[] vehicles, IEmployee[] employees)
        {
            var result = new List<IDevice>();

            foreach (var vehicle in vehicles)
                result.Add(await SetResource(vehicle.DeviceId, vehicle.Id, RelatedResourceType.Vehicle));
            foreach (var employee in employees)
                result.Add(await SetResource(employee.DeviceId, employee.Id, RelatedResourceType.Employee));

            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDevice> SetResource(object deviceId, object resourceId, RelatedResourceType resourceType)
        {
            var device = await GetDeviceById(deviceId);
            if (device != null)
            {
                device.ResourceId = ToId(resourceId);
                device.ResourceType = resourceType;
                await Repsert(device);
            }
            return device;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<IDevice> ClearResource(object deviceId)
        {
            if (deviceId == null)
                return null;

            var device = await GetDeviceById(deviceId);
            if (device != null)
            {
                device.ResourceId = null;
                device.ResourceType = null;
                await Repsert(device);
            }
            return device;
        }

        public async Task<IDevice[]> ClearDeviceResources()
        {
            var devices = await Collection.Find(Builders<Device>.Filter.Where(x => true)).ToListAsync().ConfigureAwait(false);
            foreach (var device in devices)
            {
                device.ResourceId = null;
                device.ResourceType = null;
                await Repsert(device);
            }
           
            return devices.Cast<IDevice>().ToArray();
        }

        /// <summary>
        /// Для миграций ресурсов
        /// </summary>
        /// <param name="withRelatedSource"></param>
        /// <returns></returns>
        public async Task<IDevice[]> GetAllDevices(bool withRelatedSource)
        {
            var filter = withRelatedSource ? Builders<Device>.Filter.Ne(x => x.ResourceId, null) : Builders<Device>.Filter.Where(x => true);
            var fields = Builders<Device>.Projection
               .Exclude("Commands")
               .Exclude("GeozonesEntrances")
               .Exclude("DeviceMetadata")
               .Exclude("ExtendedInfo")
               .Exclude("DeviceMetadata")
               .Exclude("CurrentExecutions")
               .Exclude("LastExecuted")
               .Exclude("FuelSetting")
               .Exclude("PriorityFieldsConfiguration")
               .Exclude("StopTime")
               .Exclude("UpdateDate")
               .Exclude("UpdateAccountId")
               .Exclude("Parameters")
               .Exclude("CalibrationSettings")
               .Exclude("ParametersViewSettings")
               .Exclude("BoolCalibrationSettings")
               .Exclude("MappingConfiguration")
               .Exclude("MappingSettings");

            var devices = await Collection.Find(filter).Project<Device>(fields).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="resourceId"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public async Task<IDevice> SetRelatedResourceCard(object deviceId, object resourceId, RelatedResourceType resourceType)
        {
            var device = await GetDeviceById(deviceId);
            if (device != null)
            {
                device.ResourceId = ToId(resourceId);
                device.ResourceType = resourceType;
                await Repsert(device);
            }
            return device;
        }


        /// <summary>
        /// Получить список ТУ в системе по фильтру
        /// </summary>
        /// <param name="request"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<PlatformPaginationResponse<IDevice>> GetDevicesPagination(PlatformPaginationRequest request, RelationStatus status)
        {
            // Customer Filter
            var filterCustomer = BuildCustomerFilter();

            // Name Filter
            var filterName = BuildFilterByName(request.SearchFilter);

            // Фильтрация по заданным идентификаторам
            var filterDeviceStatus = Builders<Device>.Filter.Where(x => true);
            if (status == RelationStatus.Related)
                filterDeviceStatus = Builders<Device>.Filter.Ne(x => x.ResourceId, null);
            if (status == RelationStatus.NotRelated)
                filterDeviceStatus = Builders<Device>.Filter.Eq(x => x.ResourceId, null);


            var filter = Builders<Device>.Filter.And(filterCustomer, filterDeviceStatus, filterName);
            var fields = Builders<Device>.Projection
                .Exclude("Commands")
                .Exclude("GeozonesEntrances")
                .Exclude("DeviceMetadata")
                .Exclude("ExtendedInfo")
                .Exclude("DeviceMetadata")
                .Exclude("CurrentExecutions")
                .Exclude("LastExecuted")
                .Exclude("FuelSetting")
                .Exclude("PriorityFieldsConfiguration")
                .Exclude("StopTime")
                .Exclude("UpdateDate")
                .Exclude("UpdateAccountId")
                .Exclude("Parameters")
                .Exclude("MappingConfiguration")
                .Exclude("MappingSettings")
                .Exclude("RelatedResource");

            var query = Collection.Find(filter);

            if (!string.IsNullOrWhiteSpace(request.OrderBy))
            {
                var sort = PlatformPaginationBuilder.Sort<Device>(request);
                query = query.Sort(sort);
            }

            var totalItem = await query.CountAsync().ConfigureAwait(false);
            var result = await query.Project<Device>(fields).Skip(request.Skip).Limit(request.Limit).ToListAsync().ConfigureAwait(false);

            var pagesMetadata = request.GetPaginationMetadata(totalItem);
            return new PlatformPaginationResponse<IDevice>(result.Cast<IDevice>().ToArray(), pagesMetadata);
        }

        /// <summary>
        /// Для миграций ресурсов
        /// </summary>
        /// <returns></returns>
        public async Task<IDevice[]> GetAllDevicesWithFuelSetting()
        {
            var filter = Builders<Device>.Filter.Where(x => true);
            var fields = Builders<Device>.Projection
               .Exclude("Commands")
               .Exclude("GeozonesEntrances")
               .Exclude("DeviceMetadata")
               .Exclude("ExtendedInfo")
               .Exclude("DeviceMetadata")
               .Exclude("CurrentExecutions")
               .Exclude("LastExecuted")
               .Exclude("PriorityFieldsConfiguration")
               .Exclude("StopTime")
               .Exclude("UpdateDate")
               .Exclude("UpdateAccountId")
               .Exclude("Parameters")
               .Exclude("CalibrationSettings")
               .Exclude("ParametersViewSettings")
               .Exclude("BoolCalibrationSettings")
               .Exclude("MappingConfiguration")
               .Exclude("MappingSettings");

            var devices = await Collection.Find(filter).Project<Device>(fields).ToListAsync().ConfigureAwait(false);
            return devices.Cast<IDevice>().ToArray();
        }
    }
}