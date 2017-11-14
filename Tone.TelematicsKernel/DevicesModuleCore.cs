using AutoMapper;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Annotations;
using Tone.Core.Data;
using Tone.Core.Data.Account;
using Tone.Core.Data.Constants;
using Tone.Core.Enums;
using Tone.Core.Extensions;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.AnalyticSystem.Model.Base;
using Tone.Core.Subsystems.BusinessObjects;
using Tone.Core.Subsystems.BusinessObjects.Model;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.WebContext;
using Tone.Data.Mongo.Base.Extensions;
using Tone.Data.Mongo.Model;
using Tone.Pagination;
using Tone.TelematicsKernel.Helpers;
using GpsValue = Tone.Core.Data.GpsValue;

namespace Tone.TelematicsKernel
{
    public partial class DevicesModuleCore : TelematicsKernelSubsystemModule, IDevicesModuleCore
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public async Task<PlatformResponse<IDevice[]>> GetDevicesDetails()
        {
            rc.DemandAuthorization();

            var devices = await Repositories.Devices.GetAll();
            return new PlatformResponse<IDevice[]>(devices);
        }

        #region GetDevice - Получение устройства по идентификатору

        /// <summary>
        /// Получение устройства по идентификатору
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<DeviceViewModel>> GetDevice(object id, bool isAdmin = false)
        {
            DemandAuthorization();

            var deviceId = Repositories.Devices.ToId(id);
            if (deviceId == null)
                return new PlatformResponse<DeviceViewModel>(ResultCode.BadFields, "DeviceId is wrong");

            var device = isAdmin
                ? await Repositories.Devices.AdminGetDeviceById(deviceId)
                : await Repositories.Devices.GetById(deviceId);

            if (device == null)
                return new PlatformResponse<DeviceViewModel>(ResultCode.NotFound, "Device Not Found");
			
			var result = await GenerateDeviceViewModel(device);
            return new PlatformResponse<DeviceViewModel>(result);
        }

        private async Task<DeviceViewModel> GenerateDeviceViewModel(IDevice device)
        {
            var deviceModel = await GenerateDeviceTelematicsViewModel(device);
            var relatedResource = await GenerateDeviceRelatedResourceViewModel(device, deviceModel.RelatedResource, device.Id);

			// Set Device Parameters
			if (device.Mapping == null || !device.Mapping.Any())
                device = await InitDeviceParameters(device);

	        // Set Default Fuel Settings && Default Fuel Rules Settings
            //if (device.FuelSetting == null)
            //    await InitDefaultFuelSetting(device);

            // Set Default PriorityFieldsConfiguration

	        var fullRecordHelper = new DeviceFullRecordHelper(Repositories.FullRecords);

	        var lastfullRecord = await fullRecordHelper.GetLastFullRecordByDeviceId(device.Id);

            var response = new DeviceViewModel
            {
                DeviceModel = deviceModel,
                DeviceRelatedResource = relatedResource,
                Parameters = device.Mapping.Select(x=>new DeviceParameterViewModel{ Title = x.ViewName, Type = x.SourceName, ViewEnabled = x.ViewEnabled}).ToList(),
				Calibrations = device.Mapping.Where(x=>x is CalibrationParameter).Cast<CalibrationParameter>().Select(x => new CalibrationSettingViewModel { BoolValue = x.IsBool, Title = x.ViewName, Type = x.SourceName, Coefficient1 = x.Coefficient1, Coefficient2 = x.Coefficient2, Enabled = x.IsEnabled, MeasureName = x.Unit, StartValue = x.StartValue, CorrectedValue = x.CorrectedValue}).ToList(),
                FuelSetting = device.FuelSetting,
				Mapping = device.Mapping
            };

            return await Task.Run(() => response);
        }
        
        private async Task<DeviceRelatedResource> GenerateDeviceRelatedResourceViewModel(IDevice device, ResourceViewCard resourceModel, object deviceId)
        {
            var history = (await Repositories.DeviceHistory.GetByParams(new ObjectId(deviceId.ToString())));
            history = await GetDistinctHistory(device, history, deviceId);

            var helper = rc.PlatformComponents.BusinessObjectsSubsystem.CreateResourceViewCardHelper();

            var result = new DeviceRelatedResource()
            {
                Current = Mapper.Map<ResourceViewCard>(resourceModel),
                Last = history.Count > 0 ? await GetHistoryItem(history[0], helper) : null,
                Total = history.Count
            };
            return result;
        }

        #endregion GetDevice - Получение устройства по идентификатору

        #region SetProperty

        ///// <summary>
        ///// SetProperty
        ///// </summary>
        ///// <param name="id"></param>
        ///// <param name="code"></param>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //[Obsolete("Method Do Not Use")]
        //public async Task<PlatformResponse<IExecution>> SetProperty(object id, string code, string value)
        //{
        //    await rc.DemandPermission(Permission.ViewDevice);

        //    var deviceId = Repositories.Devices.ToId(id);
        //    var device = await Repositories.Devices.GetById(deviceId);
        //    var property = device?.Properties.FirstOrDefault(p => p.Code == code && p.Enabled);
        //    if (property == null)
        //        return new PlatformResponse<IExecution>(ResultCode.NotFound);

        //    var propertyValue = property.Type.GetValueBase();
        //    propertyValue.SetValue(value);

        //    var execution = await Repositories.Executions.SetProperty(device, code, propertyValue);
        //    return new PlatformResponse<IExecution>(execution);
        //}

        #endregion SetProperty

        #region ExecuteCommand

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        /// <param name="code"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<IExecution>> ExecuteCommand(object id, string code, List<CommandExecution.CommandArgument> arguments)
        {
            await rc.DemandPermission(Permission.ViewDevice);

            var deviceId = Repositories.Devices.ToId(id);
            var device = await Repositories.Devices.GetById(deviceId);
            var command = device?.Commands.FirstOrDefault(c => c.Code == code);
            if (device == null || command == null || !command.Enabled)
                return new PlatformResponse<IExecution>(ResultCode.NotFound);

            var values = new List<ValueBase>();
            if (command.Arguments.Count > 0)
            {
                for (var i = 0; i < command.Arguments.Count; i++)
                {
                    var commandArgument = command.Arguments[i];
                    var executionArgument = arguments.FirstOrDefault(a => a.Index == i);
                    if (executionArgument == null)
                        continue;

                    var value = commandArgument.Type.GetValueBase();
                    value?.SetValue(executionArgument.Value);
                    values.Add(value);
                }
            }

            var result = await Repositories.Executions.ExecuteCommand(device, code, values.ToArray());
            return new PlatformResponse<IExecution>(result);
        }

        #endregion ExecuteCommand

        #region GetState

        /// <summary>
        /// GetState
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<IRecordState>> GetState(object id)
        {
            DemandAuthorization();

            var deviceId = Repositories.Devices.ToId(id);
            var device = await Repositories.Devices.GetById(deviceId);
            if (device == null)
                return new PlatformResponse<IRecordState>(ResultCode.NotFound);

            var state = await Repositories.DeviceStates.Get(device.Id);
            if (state == null)
                return new PlatformResponse<IRecordState>(null);

            return new PlatformResponse<IRecordState>(state);
        }

        #endregion GetState

        #region GetRecords

        /// <summary>
        /// GetRecords
        /// </summary>
        /// <param name="id"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<IRecord[]>> GetRecords(object id, DateTime? @from, DateTime? @to)
        {
            await rc.DemandPermission(Permission.ViewDeviceHistory);

            var deviceId = Repositories.Devices.ToId(id);
            var device = await Repositories.Devices.GetById(deviceId);
            if (device == null)
                return new PlatformResponse<IRecord[]>(ResultCode.NotFound);

            try
            {
                var records = await Repositories.Records.GetByDevice(device, @from ?? DateTime.MinValue, @to ?? DateTime.MaxValue);
                return new PlatformResponse<IRecord[]>(records);
            }
            catch (Exception)
            {
                return new PlatformResponse<IRecord[]>(ResultCode.WrongCode, "Records schema is incorrect");
            }
        }

        #endregion GetRecords

        #region LastPositions

        /// <summary>
        /// LastPositions
        /// </summary>
        /// <param name="deviceIds"></param>
        /// <returns></returns>
        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<GetLastPositionsResponse>> LastPositions(IEnumerable<object> deviceIds)
        {
            var response = new GetLastPositionsResponse();
            if (deviceIds == null || !deviceIds.Any())
                return new PlatformResponse<GetLastPositionsResponse>(ResultCode.BadFields, "DeviceId Array is empty");

            var idsArray = deviceIds.Select(id => Repositories.Devices.ToId(id)).ToArray();
            if (idsArray.Any(id => id == null))
                return new PlatformResponse<GetLastPositionsResponse>(ResultCode.BadFields, "Device Id is incorrect");

            var ids = idsArray.Where(id => id != null).ToList();

            // Ищем устройства по переданным идентификаторам
            var devices = await Repositories.Devices.GetByIds(ids);

            // Не найденные устройства добавляем к списку ошибок
            var notFoundDevices = ids.Where(sourceId => devices.Count(foundDevice => sourceId.Equals(foundDevice.Id)) == 0).ToArray();
            foreach (var id in notFoundDevices)
            {
                response.Errors.Add(new GetLastPositionsResponse.Error
                {
                    DeviceId = id.ToString(),
                    Code = GetLastPositionsResponse.ErrorCode.DeviceNotFound
                });
            }

            var fullRecordHelper = new DeviceFullRecordHelper(Repositories.FullRecords);
            foreach (var device in devices)
            {
                var fullRecord = await fullRecordHelper.GetLastFullRecordByDeviceId(device.Id);
                if (fullRecord == null) // || !fullRecord.IsFullRecordCorrect())
                {
                    response.Errors.Add(new GetLastPositionsResponse.Error
                    {
                        DeviceId = device.Id.ToString(),
                        Code = GetLastPositionsResponse.ErrorCode.DeviceStateValueNotFound
                    });
                    continue;
                }

                response.Success.Add(new GetLastPositionsResponse.Location
                {
                    DeviceId = device.Id.ToString(),
                    Lat = fullRecord.Latitude,
                    Lon = fullRecord.Longitude,
                    Course = fullRecord.Course,
                    Spd = fullRecord.Speed,
                    Sat = fullRecord.Satellites,
                    Time = fullRecord.DeviceTime.ToUnixTimestamp()
                });
            }

            return new PlatformResponse<GetLastPositionsResponse>(response);
        }

        #endregion LastPositions
        

        #region GetFirstTrackParts - Full records

        /// <summary>
        /// GetFirstTrackParts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<GetFirstTrackPartsResponse>> GetFirstTrackPartsFullRecords(GetFirstTrackPartsRequest request)
        {
            if (request.DeviceIds == null || !request.DeviceIds.Any())
                return new PlatformResponse<GetFirstTrackPartsResponse>() {Code = ResultCode.Ok, Data = new GetFirstTrackPartsResponse() {TrackParts = new MonitoringTrackPart[] {}}};

            var deviceIds = request.DeviceIds.Select(id => Repositories.Devices.ToId(id)).ToArray();
            var deviceFullRecordHelper = new DeviceFullRecordHelper(Repositories.FullRecords);
            var trackParts = new List<MonitoringTrackPart>();

            foreach (var deviceId in deviceIds)
            {
                var startTime = request.StartTime ?? DateTime.UtcNow;
                //var fullRecords = await Repositories.FullRecords.GetFullRecordsByDeviceTime(deviceId, startTime, DateTime.MaxValue);
                var fullRecords = await deviceFullRecordHelper.GetFullRecordsByDeviceTime(deviceId, startTime, DateTime.MaxValue);

                FullRecord<ObjectId?> lastRecord = fullRecords.Any() ? fullRecords[0] : null;

                // Проходим все записи и формируем historyPoints
                //
                var historyPoints = new List<TrackPoint>();
                foreach (var fullRecord in fullRecords)
                {
                    // Добавляем в историю
                    historyPoints.Add(new TrackPoint
                    {
                        Course = fullRecord.Course ?? 0,
                        Lat = fullRecord.Latitude ?? 0,
                        Lon = fullRecord.Longitude ?? 0,
                        Sat = fullRecord.Satellites ?? 0,
                        Spd = fullRecord.Speed ?? 0,
                        Time = fullRecord.DeviceTime
                    });
                }
                // Если точек нет, то мы обязаны вернуть последнюю, в качестве Last position
                if (!historyPoints.Any())
                {
                    //lastRecord = await Repositories.FullRecords.GetLastFullRecordByDeviceId(deviceId);
                    lastRecord = await deviceFullRecordHelper.GetLastFullRecordByDeviceId(deviceId);

                    // Записываем в качестве единственной точки
                    if (lastRecord != null)
                        historyPoints.Add(new TrackPoint
                        {
                            Course = lastRecord?.Course ?? 0,
                            Lat = lastRecord?.Latitude ?? 0,
                            Lon = lastRecord?.Longitude ?? 0,
                            Sat = lastRecord?.Satellites ?? 0,
                            Spd = lastRecord?.Speed ?? 0,
                            Time = lastRecord?.DeviceTime ?? DateTime.Now
                        });
                }

                // Online Status
                var status = deviceFullRecordHelper.GetOnlineStatus(lastRecord);

                // Формируем итоговую запись по устройству
                trackParts.Add(new MonitoringTrackPart
                {
                    Id = deviceId,
                    Code = MonitoringTrackPartCode.Ok,
                    LastState = new LastDeviceState(),
                    HistoryPoints = historyPoints.OrderBy(r => r.Time).ToArray(),
                    AdditionalOldPoints = new TrackPoint[0],
                    OnlineStatus = status
                });
            }

            var response = new GetFirstTrackPartsResponse
            {
                TrackParts = trackParts.ToArray()
            };
            return new PlatformResponse<GetFirstTrackPartsResponse>(response);
        }

        #endregion GetFirstTrackParts - Full records

        #region GetNextTrackParts -- Old records

        //public async Task<PlatformResponse<GetNextTrackPartsResponse>> GetNextTrackParts(GetNextTrackPartsRequest request)
        //{
        //    var tasks = request.MonitoringRequest.Select(async x => await GetNextTrackPartsCore(x));
        //    var trackPartsResult = (await Task.WhenAll(tasks)).ToList();

        //    List<MonitoringTrackPart> monitoringTrackPartList = new List<MonitoringTrackPart>();
        //    trackPartsResult.ForEach(x => monitoringTrackPartList.AddRange(x));

        //    var response = new GetNextTrackPartsResponse {TrackParts = monitoringTrackPartList.ToArray()};
        //    return new PlatformResponse<GetNextTrackPartsResponse>(response);
        //}

        //private async Task<List<MonitoringTrackPart>> GetNextTrackPartsCore(MonitoringRequestItem monitoringRequestItem)
        //{
        //    var trackParts = new List<MonitoringTrackPart>();

        //    var deviceIds = monitoringRequestItem.DeviceIds.Select(r => Repositories.Devices.ToId(r)).ToArray();
        //    var devices = (await Repositories.Devices.GetByIds(deviceIds)).ToDictionary(d => d.Id);

        //    foreach (var id in monitoringRequestItem.DeviceIds)
        //    {
        //        var deviceId = Repositories.Devices.ToId(id);

        //        IDevice device;
        //        if (!devices.TryGetValue(deviceId, out device))
        //        {
        //            trackParts.Add(new MonitoringTrackPart {Code = MonitoringTrackPartCode.DeviceNotFound});
        //            continue;
        //        }

        //        var propertyId = device.Properties.IndexOf(p => p.Type == DataType.Gps);
        //        if (propertyId < 0)
        //        {
        //            trackParts.Add(new MonitoringTrackPart {Code = MonitoringTrackPartCode.PropertyNotFound});
        //            continue;
        //        }

        //        var records = await Repositories.Records.GetByDevice(device, monitoringRequestItem.LastTrackPointTime, DateTime.MaxValue);

        //        var historyPoints = new List<TrackPoint>();
        //        foreach (var record in records)
        //        {
        //            var gps = record.Values.FirstOrDefault(v => v.Index == propertyId)?.Value as GpsValue;
        //            if (gps == null)
        //                continue;

        //            historyPoints.Add(new TrackPoint
        //            {
        //                Course = gps.Course ?? 0,
        //                Lat = gps.Latitude ?? 0,
        //                Lon = gps.Longitude ?? 0,
        //                Sat = gps.Satellites ?? 0,
        //                Spd = gps.Speed ?? 0,
        //                Time = record.Time
        //            });
        //        }

        //        trackParts.Add(new MonitoringTrackPart
        //        {
        //            Id = deviceId,
        //            Code = MonitoringTrackPartCode.Ok,
        //            LastState = new LastDeviceState(),
        //            HistoryPoints = historyPoints.OrderBy(r => r.Time).ToArray(),
        //            AdditionalOldPoints = new TrackPoint[0]
        //        });
        //    }

        //    return trackParts;
        //}

        #endregion GetNextTrackParts -- Old records

        #region GetNextTrackParts -- Full records

        public async Task<PlatformResponse<GetNextTrackPartsResponse>> GetNextTrackPartsFullRecords(GetNextTrackPartsRequest request)
        {
            if (request.MonitoringRequest == null || !request.MonitoringRequest.Any())
                return new PlatformResponse<GetNextTrackPartsResponse>() {Code = ResultCode.Ok, Data = new GetNextTrackPartsResponse() {TrackParts = new MonitoringTrackPart[] {}}};

            var tasks = request.MonitoringRequest.Select(async x => await GetNextTrackPartsFullRecordsCore(x));
            var trackPartsResult = (await Task.WhenAll(tasks)).ToList();

            List<MonitoringTrackPart> monitoringTrackPartList = new List<MonitoringTrackPart>();
            trackPartsResult.ForEach(x => monitoringTrackPartList.AddRange(x));

            var response = new GetNextTrackPartsResponse {TrackParts = monitoringTrackPartList.ToArray()};
            return new PlatformResponse<GetNextTrackPartsResponse>(response);
        }

        private async Task<List<MonitoringTrackPart>> GetNextTrackPartsFullRecordsCore(MonitoringRequestItem monitoringRequestItem)
        {
            //var deviceIds = monitoringRequestItem.DeviceIds.Select(r => Repositories.Devices.ToId(r)).ToArray();
            var deviceFullRecordHelper = new DeviceFullRecordHelper(Repositories.FullRecords);
            var trackParts = new List<MonitoringTrackPart>();

            foreach (var id in monitoringRequestItem.DeviceIds)
            {
                var deviceId = Repositories.Devices.ToId(id);
                var fullRecords = await Repositories.FullRecords.GetFullRecordsByDeviceTime(deviceId, monitoringRequestItem.LastTrackPointTime);
                var lastRecord = fullRecords.Any() ? fullRecords[0] : null;

                var historyPoints = new List<TrackPoint>();
                foreach (var fullRecord in fullRecords)
                {
                    historyPoints.Add(new TrackPoint
                    {
                        Course = fullRecord.Course ?? 0,
                        Lat = fullRecord.Latitude ?? 0,
                        Lon = fullRecord.Longitude ?? 0,
                        Sat = fullRecord.Satellites ?? 0,
                        Spd = fullRecord.Speed ?? 0,
                        Time = fullRecord.DeviceTime
                    });
                }
                // Если точек нет, то мы обязаны вернуть последнюю, в качестве Last position
                if (!historyPoints.Any())
                    lastRecord = await deviceFullRecordHelper.GetLastFullRecordByDeviceId(deviceId);

                // Online Status
                var status = deviceFullRecordHelper.GetOnlineStatus(lastRecord);

                trackParts.Add(new MonitoringTrackPart
                {
                    Id = deviceId,
                    Code = MonitoringTrackPartCode.Ok,
                    LastState = new LastDeviceState(),
                    HistoryPoints = historyPoints.OrderBy(r => r.Time).ToArray(),
                    AdditionalOldPoints = new TrackPoint[0],
                    OnlineStatus = status
                });
            }

            return trackParts;
        }

        #endregion GetNextTrackParts -- Full records

        #region Record By Device

        ///// <summary>
        ///// Возврат записей по списку id устройств
        ///// </summary>
        ///// <param name="request">Запрос с параметрами</param>
        ///// <returns>Набор GPS записей c доп. информацией</returns>
        //public async Task<PlatformResponse<GpsRecordsResponse>> GetRecordsByDevices(RecordsByDevicesRequest request)
        //{
        //    DemandAuthorization();
        //    DemandValidation(request, necessary: true);

        //    // ReSharper disable once PossibleNullReferenceException
        //    var tasks = request.DeviceIds.Select(async id =>
        //        await GetRecordsByDeviceCore(id, request.DateStart.Value, request.DateEnd.Value, request.Events, request.EventId, request.ShowPlayerInfo));

        //    var deviceRecords = await Task.WhenAll(tasks);

        //    var response = new GpsRecordsResponse {Devices = deviceRecords};
        //    return new PlatformResponse<GpsRecordsResponse>(response);
        //}

        #region Ext Methods

        //private async Task<DeviceRecordsRm> GetRecordsByDeviceCore(object id, DateTime from, DateTime to,
        //    [CanBeNull] RoadEventType[] events, [CanBeNull] string eventId, bool showPlayerInfo)
        //{
        //    var recordsResult = await GetRecordsByDeviceId(Repositories.Devices.ToId(id), from, to, events, eventId);
        //    if (recordsResult.Code != ResultCode.Ok)
        //    {
        //        return DeviceRecordsRm.CreateWithError(id, recordsResult.Code, recordsResult.Message);
        //    }

        //    // В случае запроса на отображение информации для плеера,
        //    // заполняем поездки и индикаторы фиктивными значениями
        //    //
        //    if (showPlayerInfo && recordsResult.Data.Points.Any())
        //    {
        //        // todo заглушка для индикаторов и поездок
        //        recordsResult.Data.Indicators = RandomData.GetRandomDeviceIndicators(events ?? new RoadEventType[0]);
        //        recordsResult.Data.Trips = RandomData.GetRandomDeviceTrips(recordsResult.Data.Points);
        //    }

        //    return recordsResult.Data;
        //}

        //private async Task<PlatformResponse<DeviceRecordsRm>> GetRecordsByDeviceId(object id,
        //    DateTime from, DateTime to, [CanBeNull] RoadEventType[] events, [CanBeNull] string eventId)
        //{
        //    var device = await Repositories.Devices.GetById(id);
        //    if (device == null)
        //        return new PlatformResponse<DeviceRecordsRm>(ResultCode.DeviceNotFound, "Device not found");

        //    // Get property Gps
        //    var propertyId = device.Properties.IndexOf(p => p.Type == DataType.Gps);
        //    if (propertyId < 0)
        //        return new PlatformResponse<DeviceRecordsRm>(ResultCode.GpsValueNotFound, "Device's GPS property not found");

        //    // Get Records
        //    var records = await Repositories.Records.GetByDevice(device, from, to);

        //    // Generate DeviceRecords
        //    var deviceRecords = CreateDeviceRecords(records, device.Id, propertyId, events, eventId);
        //    return new PlatformResponse<DeviceRecordsRm>(deviceRecords);
        //}

        //private DeviceRecordsRm CreateDeviceRecords(IRecord[] records, object deviceId,
        //    int propertyId, [CanBeNull] RoadEventType[] eventsFilter, [CanBeNull] string eventId)
        //{
        //    var deviceRecords = DeviceRecordsRm.Create(deviceId);

        //    var list = new List<TrackPositionRm>();
        //    foreach (var record in records)
        //    {
        //        var gps = record.Values.FirstOrDefault(v => v.Index == propertyId)?.Value as GpsValue;

        //        // Преобразуем TrackEvents в TrackEventsRm
        //        TrackEventRm[] trackEventsRm = null;

        //        if (eventId == null)
        //        {
        //            trackEventsRm = record.TrackEvents?
        //                // Только те события, которые проходят фильтр, либо все события, если events == null
        //                // todo Maksim R. вынести запрос в репозиторий для оптимизации
        //                .Where(
        //                    e =>
        //                        (eventsFilter?.Any(eventInFilter => eventInFilter == e.ParseType()) ?? false) ||
        //                        eventsFilter == null)
        //                .Select(TrackEventRm.Create).ToArray();
        //        }
        //        else
        //        {
        //            object eventObjectId = eventId.ToObjectId();
        //            trackEventsRm = record.TrackEvents?
        //                // Только те события, которые проходят фильтр, либо все события, если events == null
        //                // todo Maksim R. вынести запрос в репозиторий для оптимизации
        //                .Where(e => e.Guid.Equals(eventObjectId))
        //                .Select(TrackEventRm.Create).ToArray();
        //        }

        //        if (gps != null)
        //        {
        //            list.Add(new TrackPositionRm
        //            {
        //                Time = record.Time,
        //                Lat = gps.Latitude,
        //                Lon = gps.Longitude,
        //                Course = gps.Course,
        //                Spd = gps.Speed,
        //                Sat = gps.Satellites,
        //                Events = (trackEventsRm?.Length > 0) ? trackEventsRm : null
        //            });
        //        }
        //    }

        //    deviceRecords.Points = list.OrderBy(d => d.Time).ToArray();
        //    return deviceRecords;
        //}

        #endregion Ext Methods

        #endregion Record By Device

        #region GetByParam

        /// <summary>
        /// Получение списка значений свойств.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="top"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<DeviceRecordsInfo>> GetRecordsByParam(object id, int? top, int? skip)
        {
            var validate = new ValidateResult("RecordsByDevice");

            var deviceId = Repositories.Devices.ToId(id);
            if (deviceId == null)
            {
                validate.Message(ValidationField.Id, "Id is wrong");
                return new PlatformResponse<DeviceRecordsInfo>(ResultCode.BadFields, validate.ToTextBlock());
            }

            var args = new object[] {skip, top};
            var numberDefinedParams = args.Count(q => q != null);

            if (numberDefinedParams > 0 && numberDefinedParams < 2)
            {
                if (top == null)
                    validate.Message(ValidationField.Parameter, "'Top' parameter is missing");
                if (skip == null)
                    validate.Message(ValidationField.Parameter, "'Skip' parameter is missing");
                if (!validate.IsValid)
                    return new PlatformResponse<DeviceRecordsInfo>(ResultCode.BadFields, validate.ToTextBlock());
            }

            // Проверка параметров
            if (numberDefinedParams == 2)
            {
                if (skip < 0)
                    validate.Message(ValidationField.Parameter, "Skip parameter is incorrect (should be equal or greater than 0)");
                if (top <= 0)
                    validate.Message(ValidationField.Parameter, "Limit parameter is incorrect (should be greater than 0)");

                if (!validate.IsValid)
                    return new PlatformResponse<DeviceRecordsInfo>(ResultCode.BadFields, validate.ToTextBlock());
            }

            //fixme : зачем так делать???
            var device = await Repositories.Devices.GetById(deviceId);
            if (device == null)
                return new PlatformResponse<DeviceRecordsInfo>(ResultCode.DeviceNotFound, "Device not found");

            var records = await Repositories.Records.GetByParam(device, top, skip);
            var recordsCount = await Repositories.Records.CountByDeviceId(id);

            var result = new DeviceRecordsInfo {Records = records, TotalCount = recordsCount};
            return new PlatformResponse<DeviceRecordsInfo>(result);
        }

        #endregion GetByParam

        #region GetDevices - Получение списка устройств по фильтру

        /// <summary>
        /// Получение списка устройств по фильтру
        /// </summary>
        /// <returns></returns>
        public async Task<PlatformResponse<DevicesPaginationResult>> GetDevices(DeviceFilterRequest request,
            PlatformPaginationRequest paginationRequest)
        {
            DemandAuthorization();
            DemandValidation(request, true);

            var paginationDevices = await Repositories.Devices.GetDevicesPagination(paginationRequest, request.Status);
            var devicesResponse = await GenerateDeviceTelematicsResponse(paginationDevices.Items);

            var result = new DevicesPaginationResult(devicesResponse.ToArray(), paginationDevices.PageInfo);
            return new PlatformResponse<DevicesPaginationResult>(result);
        }

        private async Task<DeviceTelematicsViewModel[]> GenerateDeviceTelematicsResponse(IDevice[] devices)
        {
            var result = new List<DeviceTelematicsViewModel>();

            foreach (var device in devices)
            {
                var deviceModel = await GenerateDeviceTelematicsViewModel(device);
                result.Add(deviceModel);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task<DeviceTelematicsViewModel> GenerateDeviceTelematicsViewModel(IDevice device)
        {
            var deviceViewModel = Mapper.Map<DeviceTelematicsViewModel>(device);
            if (device.ResourceId == null)
                return deviceViewModel;

            deviceViewModel.RelatedResource = await FindRelatedResource(device);
            if (deviceViewModel.RelatedResource == null)
                return deviceViewModel;

            deviceViewModel.RelatedResourceName = deviceViewModel.RelatedResource.PrimaryTitle;
            deviceViewModel.IsRelated = true;
            return deviceViewModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private async Task<ResourceViewCard> FindRelatedResource(IDevice device)
        {
            if (!device.ResourceType.HasValue)
                return null;

            var helper = rc.PlatformComponents.BusinessObjectsSubsystem.CreateResourceViewCardHelper();

            // Employee Resource
            if (device.ResourceType == RelatedResourceType.Employee)
            {
                var employee = await Repositories.Employees.GetEmployeeById(device.ResourceId, device.Id);
                if (employee != null)
                    return await helper.GenerateEmployeeResourceViewCard(employee);
            }

            // Vehicle Resource
            if (device.ResourceType == RelatedResourceType.Vehicle)
            {
                var vehicle = await Repositories.Vehicles.GetById(device.ResourceId);
                if (vehicle != null)
                    return await helper.GenerateVehicleResourceViewCard(vehicle);
            }

            return null;
        }

        #endregion GetDevices - Получение списка устройств по фильтру

        #region GetDeviceHistory

        /// <summary>
        /// Получение истории объектов, привязанных к ТМУ ранее
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<DeviceHistoryViewModel>> GetDeviceHistory(DeviceHistoryRequest request)
        {
            DemandAuthorization();
            DemandValidation(request, true);

            var deviceId = Repositories.Devices.ToId(request.DeviceId);
            if (deviceId == null)
                return new PlatformResponse<DeviceHistoryViewModel>(ResultCode.BadFields, "DeviceId is wrong");

            var device = await Repositories.Devices.GetById(deviceId);
            if (device == null)
                return new PlatformResponse<DeviceHistoryViewModel>(ResultCode.NotFound, "Device Not Found");

            var history = await Repositories.DeviceHistory.GetUnboundDeviceHistory(request.DeviceId, request.Skip, request.Limit);

            var resourceItemList = await GetHistoryItems(history, request.DeviceId);
            var result = new DeviceHistoryViewModel() {ResourceItems = resourceItemList};
            return new PlatformResponse<DeviceHistoryViewModel>(result);
        }

        /// <summary>
        /// Расшифровка истории в список
        /// </summary>
        /// <param name="history"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        private async Task<List<ResourceViewCard>> GetHistoryItems(List<IDeviceHistory> history, object deviceId)
        {
            // Удаляем повторения
            //history = await GetDistinctHistory(history, deviceId);

            var helper = rc.PlatformComponents.BusinessObjectsSubsystem.CreateResourceViewCardHelper();
            var result = new List<ResourceViewCard>();
            foreach (var deviceHistory in history)
            {
                // Получаем информацию о ресурсе
                var resourceItem = await GetHistoryItem(deviceHistory, helper);
                if (resourceItem != null)
                    result.Add(resourceItem);
            }

            return result.ToList();
        }

        // Исключаем повторения и текущий ресурс
        private async Task<List<IDeviceHistory>> GetDistinctHistory(IDevice device, List<IDeviceHistory> history, object deviceId)
        {
            if (history.Count == 0)
                return history;

            var currentResource = await FindRelatedResource(device);
            var result = new List<IDeviceHistory>();

            foreach (var deviceHistory in history)
            {
                if (deviceHistory.ResourceId == null || result.Any(x => x.ResourceId.ToString() == deviceHistory.ResourceId.ToString()) // повторения
                    || (deviceHistory.ResourceId.ToString() == currentResource?.Id.ToString())) // текущий связный ресурс
                    continue;

                result.Add(deviceHistory);
            }

            return result;
        }

        // Получаем информацию о ресурсе
        private async Task<ResourceViewCard> GetHistoryItem(IDeviceHistory history, IResourceViewCardHelper helper)
        {
            var vehilce = await Repositories.Vehicles.GetById(history.ResourceId);
            if (vehilce != null)
                return await helper.GenerateVehicleResourceViewCard(vehilce);

            var employee = await Repositories.Employees.GetById(history.ResourceId);
            if (employee != null)
                return await helper.GenerateEmployeeResourceViewCard(employee);

            return null;
        }

        #endregion GetDeviceHistory

        #region DeviceUpdate

        /// <summary>
        /// Обновление настроек ТМУ
        /// </summary>
        /// <param name="request"></param>
        /// <param name="isAdmin"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<IDevice>> DeviceUpdate(DeviceRequestModel request, bool isAdmin = false)
        {
            DemandAuthorization();
            DemandValidation(request, true);

            if (isAdmin && rc.Account != null)
            {
                var isDeveloperRole = await rc.Account.CheckPermission(Permission.Developer, false);
                if (!isDeveloperRole)
                    return new PlatformResponse<IDevice>(ResultCode.BadAuthSettings, "Account do not has permission");
            }

            // Device
            var deviceId = Repositories.Devices.ToId(request.Id);
            var device = isAdmin ? await Repositories.Devices.GetDeviceById(deviceId) : await Repositories.Devices.GetById(deviceId);
            if (device == null)
                return new PlatformResponse<IDevice>(ResultCode.NotFound, "Device Not Found");
            
            if (!isAdmin)
            {
                // Change DeviceHistory = Device - Resource
                var currentHistory = await Repositories.DeviceHistory.GetCurrentHistoryByDeviceAndResource(deviceId, request.ResourceId);
                if (currentHistory == null)
                {
                    var deviceRelation = await UdpateDeviceRelations(deviceId, request.ResourceId);
                    if (deviceRelation.Code != ResultCode.Ok)
                        return new PlatformResponse<IDevice>(deviceRelation.Code, "Error update relation Device and Resource");
                    
                }

                // Change RelatedResource
                device.ResourceId = request.ResourceId != null ? Repositories.Devices.ToId(request.ResourceId) : null;
                device.ResourceType = request.ResourceId != null ? request.ResourceType : null;
            }

            #region Manage Device Properties

            //var currentDevice = Mapper.Map(request, device);
            if (!string.IsNullOrEmpty(request.Type) || request.IsAcceptNull)
                device.Type = request.Type;

            if (!string.IsNullOrEmpty(request.Imei) || request.IsAcceptNull)
                device.Imei = request.Imei;

            if (!string.IsNullOrEmpty(request.Model) || request.IsAcceptNull)
                device.Model = request.Model;

            if (!string.IsNullOrEmpty(request.Manufacturer) || request.IsAcceptNull)
                device.Manufacturer = request.Manufacturer;

            if (!string.IsNullOrEmpty(request.SimCard) || request.IsAcceptNull)
                device.SimCard = request.SimCard;

            if (rc.Account != null)
                device.UpdateAccountId = rc.Account.Id;

            device.UpdateDate = DateTime.UtcNow;

            //if (device.FuelSetting == null)
            //    device.FuelSetting = FuelSetting.Create();

            // Device Fuel Settings Update
            if (request.FuelSetting != null && request.FuelSetting.FuelSources.Any())
            {
				if(request.FuelSetting.UseFuelLevel &&  !request.FuelSetting.FuelSources.Any(x=>x.IsPriority))
					return new PlatformResponse<IDevice>(ResultCode.BadFields, "Priority fuel source not set");

				if (request.FuelSetting.FuelSources != null)
                {
                    device.FuelSetting.FuelSources = DeviceBuilder.BuildFuelSources(device, request.FuelSetting.FuelSources);
                }

                if (request.FuelSetting.FuelRules != null)
                {
                    device.FuelSetting.FuelRules = DeviceBuilder.BuildFuelRules(request.FuelSetting.FuelRules);
                }
            }
			
            // todo BuildDeviceParameters в дальнейшем нужно будет заменить эти методы на 1
            //
            if (device.Mapping == null)
                device.Mapping = new List<DeviceParameterBase>();

            if (request.Mapping != null && request.Mapping.Any())
                device.Mapping = DeviceBuilder.BuildDeviceParameters(device, request.Mapping);

            if (request.Parameters != null && request.Parameters.Any())
                device.Mapping = DeviceBuilder.BuildDeviceParameters(device, request.Parameters);

            if (request.Calibrations != null && request.Calibrations.Any())
                device.Mapping = DeviceBuilder.BuildDeviceParameters(device, request.Calibrations);

            #endregion

            var result = await Repositories.Devices.Repsert(device);
            return new PlatformResponse<IDevice>(result, ResultCode.Ok);
        }

        private async Task<DeviceRelationresult> UdpateDeviceRelations(object deviceId, object newResourceId)
        {   
            IEmployee employee = null;
            IVehicle vehicle = null;
            
            // Переменная для запоминания ранее установленного ресурса в истории
            var resourceId = Repositories.DeviceHistory.ToId(newResourceId);
            if (resourceId != null)
            {
                employee = await Repositories.Employees.GetById(resourceId);
                vehicle = await Repositories.Vehicles.GetById(resourceId);
            }
            if (resourceId != null && employee == null && vehicle == null)
                return new DeviceRelationresult() {Code = ResultCode.NotFound, ResourceType = null};
            
            // Unbind Resources ***

            // DeviceHistory By DeviceId
            var deviceHistoryByDevice = await Repositories.DeviceHistory.GetCurrentDeviceHistoryByDeviceId(deviceId, null);
            // DeviceHistory By ResourceId
            var deviceHistoryByResource = await Repositories.DeviceHistory.GetCurrentDeviceHistoryByResourceId(resourceId, null);
            
            // Изменяем DeviceHistory и отвязываем Объекты
            if (deviceHistoryByDevice != null)
            {
                deviceHistoryByDevice.UnboundFromResourceTime = DateTime.UtcNow;
                await Repositories.DeviceHistory.Repsert(deviceHistoryByDevice);

                // Отвязываем Ресурсы
                await UnbindResource(deviceHistoryByDevice.ResourceId);
            }

            if (deviceHistoryByResource != null)
            {
                deviceHistoryByResource.UnboundFromResourceTime = DateTime.UtcNow;
                await Repositories.DeviceHistory.Repsert(deviceHistoryByResource);
                
                // Отвязываем Ресурсы
                await UnbindResource(deviceHistoryByResource.ResourceId);
            }

            // Bind Resources ***
            var relatedResourceType = RelatedResourceType.Vehicle;
            
            // Если указано новое ТС - его нужно привязать
            if (vehicle != null)
            {
                vehicle.DeviceId = Repositories.Devices.ToId(deviceId);
                resourceId = (await Repositories.Vehicles.Repsert(vehicle)).Id;
                relatedResourceType = RelatedResourceType.Vehicle;
            }
            // Если указан новый сотрудник - его нужно привязать
            if (employee != null)
            {
                employee.DeviceId = Repositories.Devices.ToId(deviceId);
                resourceId = (await Repositories.Employees.Repsert(employee)).Id;
                relatedResourceType = RelatedResourceType.Employee;
            }

            // Добавляем запись в deviceHistory
            if (resourceId != null)
            {
                var newDeviceHistory = new DeviceHistory()
                {
                    DeviceId = deviceId,
                    ResourceId = resourceId,
                    RelatedResourceType = relatedResourceType,
                    BoundToResourceTime = DateTime.UtcNow
                };

                // Добавляем новую запись в историю
                await Repositories.DeviceHistory.Repsert(newDeviceHistory);
            }


            return new DeviceRelationresult() {Code = ResultCode.Ok, ResourceType = relatedResourceType};
        }

        private async Task RemoveDeviceRelation(object deviceId)
        {
            var employeeOld = await Repositories.Employees.GetEmployeeByDevice(deviceId);
            if (employeeOld != null)
            {
                employeeOld.DeviceId = null;
                await Repositories.Employees.Repsert(employeeOld);
                return;
            }
            var vehicleOld = await Repositories.Vehicles.GetVehicleByDevice(deviceId);
            if (vehicleOld != null)
            {
                vehicleOld.DeviceId = null;
                await Repositories.Vehicles.Repsert(vehicleOld);
            }
        }

        /// <summary>
        /// Добавление телематической команды
        /// </summary>
        public async Task<PlatformResponse> AddCommand(SendCommandRequest request)
        {
            DemandAuthorization();
            DemandValidation(request, necessary: true);

            var deviceObjectId = Repositories.Devices.ToId(request.DeviceId);
            var device = await Repositories.Devices.GetById(deviceObjectId);
            if (device == null)
                return new PlatformResponse(ResultCode.NotFound, "Device not found");

            if (rc.Account != null)
            {
                var command = new Tone.Data.Mongo.Model.TelematicCommand
                {
                    AccountId = rc.Account.Id.ToString(),
                    Command = request.CommandText,
                    DeviceCode = device.Code,
                    DeviceId = request.DeviceId,
                    Id = ObjectId.Empty,
                    Status = Status.NotExecuted,
                    StatusTime = DateTime.Now.ToUniversalTime()
                };
                await Repositories.Commands.Repsert(command);

                return new PlatformResponse(ResultCode.Ok);
            }

            return new PlatformResponse(ResultCode.Error, "Account is empty");
        }

        #endregion DeviceUpdate

        #region GetDevicesForCustomes

        ///// <summary>
        ///// Получение списка устройств по фильтру для Customers
        ///// </summary>
        ///// <returns></returns>
        //public async Task<PlatformResponse<DeviceCustomersViewModel[]>> GetDevicesForCustomers(DeviceCustomerRequest request)
        //{
        //    DemandAuthorization();
        //    DemandValidation(request, true);

        //    IDevice[] devices = await Repositories.Devices.GetDevicesForCustomers(request);
        //    var devicesResponse = await GenerateDeviceCustomersViewModel(devices);
        //    return new PlatformResponse<DeviceCustomersViewModel[]>(devicesResponse);
        //}

        private async Task<DeviceCustomersViewModel[]> GenerateDeviceCustomersViewModel(IDevice[] devices)
        {
            var tasks = devices.Select(async device => await GenerateDeviceCustomerViewModel(device));
            return await Task.WhenAll(tasks);
        }

        private async Task<DeviceCustomersViewModel> GenerateDeviceCustomerViewModel(IDevice device)
        {
            var responseItem = Mapper.Map<DeviceCustomersViewModel>(device);
            responseItem.Customer = await FindCustomer(device.CustomerId);
            if (responseItem.Customer != null)
                responseItem.IsCustomerBound = true;

            return responseItem;
        }

        private async Task<Customer> FindCustomer(object customerId)
        {
            var id = Repositories.Customers.ToId(customerId);
            if (id == null || (ObjectId) id == MongoObjectId.EmptyObjectId)
                return null;
            var customer = await Repositories.Customers.GetCustomerById(id);

            return (Customer) customer;
        }

        #endregion GetDevicesForCustomes

        #region UpdateDeviceCustomerBinding

        /// <summary>
        /// Привязка настроек ТМУ к Клиенту
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<IDevice>> UpdateDeviceCustomerBinding(DeviceCustomerBindingRequest request)
        {
            DemandAuthorization();
            DemandValidation(request, true);

            // Device
            var deviceId = Repositories.Devices.ToId(request.DeviceId);
            var device = await Repositories.Devices.GetDeviceById(deviceId);
            if (device == null)
                return new PlatformResponse<IDevice>(ResultCode.NotFound, "Device Not Found");

            device.CustomerId = ObjectId.Empty;
            // Customer
            if (request.CustomerId != null)
            {
                var customerId = Repositories.Customers.ToId(request.CustomerId);
                var customer = await Repositories.Customers.GetCustomerById(customerId);
                if (customer == null)
                    return new PlatformResponse<IDevice>(ResultCode.NotFound, "Customer Not Found");

                device.CustomerId = (ObjectId) customerId;
            }

            var result = await Repositories.Devices.UpdateDevice(device);

            // Unbind Device from related objects
            if (request.CustomerId == null || request.CustomerId.ToString() == ObjectId.Empty.ToString())
                await ClearDeviceBinding(deviceId);

            return new PlatformResponse<IDevice>(result, ResultCode.Ok);
        }

        #endregion UpdateDeviceCustomerBinding

        #region UpdateMultipleDeviceCustomerBinding
        public async Task<PlatformResponse<DeviceCustomersViewModel[]>> UpdateMultipleDeviceCustomerBinding(MultipleDeviceCustomerBindingRequest request)
        {
            DemandAuthorization();
            DemandValidation(request, true);

            var customerId = Repositories.Customers.ToId(request.CustomerId);
            var customer = await Repositories.Customers.GetCustomerById(customerId);
            if (customer == null)
                return new PlatformResponse<DeviceCustomersViewModel[]>(ResultCode.NotFound, "Customer Not Found");

            var devices = new List<IDevice>();

            // Bind Devices
            if (request.BindingDeviceIds != null && request.BindingDeviceIds.Any())
            {
                var bindingDevices = await Repositories.Devices.GetDevicesByIds(request.BindingDeviceIds);
                if (bindingDevices != null && bindingDevices.Any())
                {
                    var result = await Repositories.Devices.UpdateMultipleDeviceCustomerBinding(customerId, bindingDevices, true);
                    devices.AddRange(result);
                }
            }

            // Unbind Devices
            if (request.UnbindingDeviceIds != null && request.UnbindingDeviceIds.Any())
            {
                //var ids2 = request.UnbindingDeviceIds.Select(d => Repositories.Devices.ToId(d)).ToList();
                var unbindingDevices = await Repositories.Devices.GetDevicesByIds(request.UnbindingDeviceIds);
                if (unbindingDevices != null && unbindingDevices.Any())
                {
                    var result = await Repositories.Devices.UpdateMultipleDeviceCustomerBinding(customerId, unbindingDevices, false);
                    devices.AddRange(result);

                    foreach (var device in unbindingDevices)
                        await ClearDeviceBinding(device.Id);
                }
            }

            var devicesResponse = await GenerateDeviceCustomersViewModel(devices.ToArray());
            return new PlatformResponse<DeviceCustomersViewModel[]>(devicesResponse);
        }

        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task ClearDeviceBinding(object deviceId)
        {
            // Unbind Device from Vehicle
            await Repositories.Vehicles.ClearVehicleDevice(deviceId, CustomerRepositoryAccess.Global);
            // Unbind Device from Employee
            await Repositories.Employees.ClearEmployeeDevice(deviceId, CustomerRepositoryAccess.Global);
            // Clear DeviceHistory
            await Repositories.DeviceHistory.UnboundDeviceHistory(deviceId);
        }

        #region Set Default CalibrationSettings

        ///// <summary>
        ///// Установка Параметров устройства по-умолчанию
        ///// </summary>
        ///// <param name="device"></param>
        ///// <returns></returns>
        //private async Task<List<CalibrationSettings>> SetDefaultCalibrationSettings(IDevice device)
        //{
        //    if (device == null)
        //        return new List<CalibrationSettings>();

        //    #region Set Default CalibrationSettings - Obsoilete

        //    //device.CalibrationSettings = new List<CalibrationSettings>
        //    //{
        //    //    new CalibrationSettings() {Title = "Скорость объекта", Type = DeviceParameterType.Speed},
        //    //    new CalibrationSettings() {Title = "Направление движения", Type = DeviceParameterType.Course},
        //    //    new CalibrationSettings() {Title = "Тревожная кнопка", Type = DeviceParameterType.PanicButton, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Температура двигателя", Type = DeviceParameterType.Temperature1},
        //    //    new CalibrationSettings() {Title = "Температура салона", Type = DeviceParameterType.Temperature2},
        //    //    new CalibrationSettings() {Title = "Обороты двигателя", Type = DeviceParameterType.Rpm},
        //    //    new CalibrationSettings() {Title = "Пробег", Type = DeviceParameterType.Mileage},
        //    //    new CalibrationSettings() {Title = "U резервной батареи", Type = DeviceParameterType.BatteryLevel},
        //    //    new CalibrationSettings() {Title = "U бортовой сети", Type = DeviceParameterType.Voltage},
        //    //    new CalibrationSettings() {Title = "Расход топлива", Type = DeviceParameterType.FuelSpend},
        //    //    new CalibrationSettings() {Title = "Уровень топлива", Type = DeviceParameterType.FuelLevel1},
        //    //    new CalibrationSettings() {Title = "Уровень топлива CAN", Type = DeviceParameterType.FuelLevelCan},
        //    //    new CalibrationSettings() {Title = "Зажигание", Type = DeviceParameterType.Ignition, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Все двери", Type = DeviceParameterType.DoorsSignal, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Капот", Type = DeviceParameterType.HoodSignal, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Багажник", Type = DeviceParameterType.TrunkSignal, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Левая задняя дверь", Type = DeviceParameterType.LeftBackDoor, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Левая передняя дверь", Type = DeviceParameterType.LeftFrontDoor, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Правая задняя дверь", Type = DeviceParameterType.RightBackDoor, Coefficient1 = 0},
        //    //    new CalibrationSettings() {Title = "Правая передняя дверь", Type = DeviceParameterType.RightFrontDoor, Coefficient1 = 0}
        //    //};

        //    #endregion Set Default CalibrationSettings

        //    device.CalibrationSettings = new List<CalibrationSettings>();
        //    device = await Repositories.Devices.Repsert(device);
        //    return device.CalibrationSettings;
        //}

        //private async Task<List<CalibrationSettings>> SetCalibrationSettings(IDevice device)
        //{
        //    var deviceFullRecordHelper = new DeviceFullRecordHelper(Repositories.FullRecords);
        //    var fullRecords = await deviceFullRecordHelper.GetLastFullRecordByDeviceId(device.Id);
        //    if (fullRecords == null)
        //        return device.CalibrationSettings;

        //    #region Set CalibrationSettings

        //    // Скорость объекта
        //    var value = GetCalibrationSettingsValue(device, DeviceParameterType.Speed);
        //    if (value != null && fullRecords.Speed.HasValue)
        //        value.StartValue = (double) fullRecords.Speed;
        //    // Направление движения
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Course);
        //    if (value != null && fullRecords.Course.HasValue)
        //        value.StartValue = (double) fullRecords.Course;
        //    // Тревожная кнопка
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.PanicButton);
        //    if (value != null && fullRecords.PanicButton.HasValue)
        //        value.BoolValue = fullRecords.PanicButton;
        //    // Температура двигателя
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Temperature1);
        //    if (value != null && fullRecords.Temperature1.HasValue)
        //        value.StartValue = fullRecords.Temperature1.Value;
        //    // Температура салона
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Temperature2);
        //    if (value != null && fullRecords.Temperature2.HasValue)
        //        value.StartValue = fullRecords.Temperature2.Value;
        //    // Обороты двигателя
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Rpm);
        //    if (value != null && fullRecords.Rpm.HasValue)
        //        value.StartValue = fullRecords.Rpm.Value;
        //    // Пробег
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Mileage);
        //    if (value != null && fullRecords.Mileage.HasValue)
        //        value.StartValue = fullRecords.Mileage.Value;
        //    // Заряд батареи
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.BatteryLevel);
        //    if (value != null && fullRecords.BatteryLevel.HasValue)
        //        value.StartValue = fullRecords.BatteryLevel.Value;
        //    // Питание
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Voltage);
        //    if (value != null && fullRecords.Voltage.HasValue)
        //        value.StartValue = fullRecords.Voltage.Value;
        //    // Расход топлива
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.FuelSpend);
        //    if (value != null && fullRecords.FuelSpent.HasValue)
        //        value.StartValue = fullRecords.FuelSpent.Value;
        //    // Уровень топлива
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.FuelLevel1);
        //    if (value != null && fullRecords.FuelLevel.HasValue)
        //        value.StartValue = fullRecords.FuelLevel.Value;
        //    // Уровень топлива CAN
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.FuelLevelCan);
        //    if (value != null && fullRecords.FuelCAN.HasValue)
        //        value.StartValue = fullRecords.FuelCAN.Value;
        //    // Зажигание
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.Ignition);
        //    if (value != null && fullRecords.Ignition.HasValue)
        //        value.BoolValue = fullRecords.Ignition.Value;
        //    // Все двери
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.DoorsSignal);
        //    if (value != null && fullRecords.DoorsSignal.HasValue)
        //        value.BoolValue = fullRecords.DoorsSignal.Value;
        //    // Капот
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.HoodSignal);
        //    if (value != null && fullRecords.HoodSignal.HasValue)
        //        value.BoolValue = fullRecords.HoodSignal.Value;
        //    // Багажник
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.TrunkSignal);
        //    if (value != null && fullRecords.TrunkSignal.HasValue)
        //        value.BoolValue = fullRecords.TrunkSignal.Value;
        //    // Левая задняя дверь
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.LeftBackDoor);
        //    if (value != null && fullRecords.LeftBackDoor.HasValue)
        //        value.BoolValue = fullRecords.LeftBackDoor.Value;
        //    // Левая передняя дверь
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.LeftFrontDoor);
        //    if (value != null && fullRecords.LeftFrontDoor.HasValue)
        //        value.BoolValue = fullRecords.LeftFrontDoor.Value;
        //    // Правая задняя дверь
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.RightBackDoor);
        //    if (value != null && fullRecords.RightBackDoor.HasValue)
        //        value.BoolValue = fullRecords.RightBackDoor.Value;
        //    // Правая передняя дверь
        //    value = GetCalibrationSettingsValue(device, DeviceParameterType.RightFrontDoor);
        //    if (value != null && fullRecords.RightFrontDoor.HasValue)
        //        value.BoolValue = fullRecords.RightFrontDoor.Value;

        //    foreach (var settings in device.CalibrationSettings)
        //        settings.CorrectedValue = settings.StartValue * settings.Coefficient1 + settings.Coefficient2;

        //    #endregion Set CalibrationSettings

        //    await Repositories.Devices.Repsert(device);
        //    return device.CalibrationSettings;
        //}

        //private CalibrationSettings GetCalibrationSettingsValue(IDevice device, DeviceParameterType type)
        //{
        //    return device.CalibrationSettings.FirstOrDefault(c => c.Type == type);
        //}

        #endregion

        /// <summary>
        /// Удаление устройства
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<object>> AdminDelete(object deviceId)
        {
            DemandAuthorization();

            var id = Repositories.Devices.ToId(deviceId);
            var currentDevice = await Repositories.Devices.GetDeviceById(id);
            if (currentDevice == null)
                return new PlatformResponse(ResultCode.NotFound);

            // Delete Device
            await Repositories.Devices.AdminDelete(id);
            // Remove Device Id From Employees
            await Repositories.Employees.ClearEmployeeDevice(id, CustomerRepositoryAccess.Global);
            // Remove Device Id From Vehicle
            await Repositories.Vehicles.ClearVehicleDevice(id, CustomerRepositoryAccess.Global);

            return new PlatformResponse(ResultCode.Ok);
        }

        ///// <summary>
        ///// http://jira.t1-group.ru:8080/browse/T1-3305
        ///// T-One BE: GET /Telematics/Devices/Details/id список параметров ТМУ выходит за рамки MVP-1.
        ///// </summary>
        ///// <param name="device"></param>
        ///// <returns></returns>
        //public List<DeviceParameter> SetParametersHidden(IDevice device)
        //{
        //    return device.ParametersViewSettings.Where(p => p.Type != DeviceParameterType.Latitude
        //                                                    && p.Type != DeviceParameterType.Longitude
        //                                                    && p.Type != DeviceParameterType.Address
        //                                                    && p.Type != DeviceParameterType.Speedlimit
        //                                                    && p.Type != DeviceParameterType.Satellites
        //                                                    && p.Type != DeviceParameterType.Mileage
        //                                                    && p.Type != DeviceParameterType.LeftBackDoor
        //                                                    && p.Type != DeviceParameterType.LeftFrontDoor
        //                                                    && p.Type != DeviceParameterType.RightBackDoor
        //                                                    && p.Type != DeviceParameterType.RightFrontDoor).ToList();
        //}

        /// <summary>
        /// Установка Параметров устройства по-умолчанию
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<IDevice> InitDeviceParameters(IDevice device)
        {
            device.Mapping = new List<DeviceParameterBase>();
            device = await Repositories.Devices.Repsert(device);
            return device;
        }

        /// <summary>
        /// Установка Методы фильтрации топливных данных
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        //private async Task<IDevice> InitDefaultFuelSetting(IDevice device)
        //{
        //    if (device == null)
        //        return null;
            
        //    device.FuelSetting = FuelSetting.Create();
        //    device = await Repositories.Devices.Repsert(device);
        //    return device;
        //}

        private bool CheckDuplicateFuelSettingsName(FuelSetting fuelSetting)
        {
            if (fuelSetting == null || fuelSetting.FuelSources == null)
                return true;

            var countOriginal = fuelSetting.FuelSources.Select(x => x.SourceName).ToArray().Length;
            var countDupLicate = fuelSetting.FuelSources.Select(x => x.SourceName).Distinct().ToArray().Length;

            return countOriginal == countDupLicate;
        }

        /// <summary>
        /// Получение списка устройств по фильтру - Admin
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paginationRequest"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<AdminDevicesPaginationResult>> AdminGetDevices(DeviceCustomerRequest request, PlatformPaginationRequest paginationRequest)
        {
            DemandAuthorization();
            DemandValidation(request, true);

            PlatformPaginationResponse<IDevice> paginationDevices = await Repositories.Devices.AdminGetDevices(request, paginationRequest);
            var devicesResponse =  await GenerateDeviceCustomersViewModel(paginationDevices.Items);

            var result = new AdminDevicesPaginationResult(devicesResponse.ToArray(), paginationDevices.PageInfo);
            return new PlatformResponse<AdminDevicesPaginationResult>(result);
            
        }

        /// <summary>
        /// Отвязка ТС или сотрудника
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns></returns>
        private async Task UnbindResource(object resourceId)
        {
            var unbindableVehicle = await Repositories.Vehicles.GetById(resourceId);
            if (unbindableVehicle != null)
            {
                unbindableVehicle.DeviceId = null;
                await Repositories.Vehicles.Repsert(unbindableVehicle);
            }
            var unbindableEmployee = await Repositories.Employees.GetById(resourceId);
            if (unbindableEmployee != null)
            {
                unbindableEmployee.DeviceId = null;
                await Repositories.Employees.Repsert(unbindableEmployee);
            }
        }

        public struct DeviceRelationresult
        {
            public ResultCode Code { get; set; }
            public RelatedResourceType? ResourceType { get; set; }
        }
    }
}

#region Obsolete GetDevices2
///// <summary>
///// Получение списка устройств по фильтру
///// </summary>
///// <returns></returns>
//public async Task<PlatformResponse<DevicesPaginationResult>> GetDevices2(DeviceFilterRequest request,
//    PlatformPaginationRequest paginationRequest)
//{
//    DemandAuthorization();
//    DemandValidation(request, necessary: true);

//    PlatformPaginationResponse<IDevice> paginationDevices;
//    switch (request.Status)
//    {
//        case RelationStatus.NotRelated:
//            {
//                var vehiclesDevicesIds = await Repositories.Vehicles.GetRelatedDeviceIds();
//                var employeeDevicesIds = await Repositories.Employees.GetRelatedDeviceIds();
//                var relatedDeviceIds = new List<object>();
//                relatedDeviceIds.AddRange(vehiclesDevicesIds);
//                relatedDeviceIds.AddRange(employeeDevicesIds);
//                relatedDeviceIds = relatedDeviceIds.Distinct().ToList();

//                paginationDevices = await Repositories.Devices.GetDeviceByParam(paginationRequest, excludeDeviceIds: relatedDeviceIds);
//                break;
//            }
//        case RelationStatus.Related:
//            {
//                var searchedVehiclesDevicesIds = await Repositories.Vehicles.GetRelatedDeviceIdsByName(paginationRequest.SearchFilter);
//                var searchedEmployeeDevicesIds = await Repositories.Employees.GetRelatedDeviceIdsByName(paginationRequest.SearchFilter);
//                var searchedDeviceIds = new List<object>();
//                searchedDeviceIds.AddRange(searchedVehiclesDevicesIds);
//                searchedDeviceIds.AddRange(searchedEmployeeDevicesIds);
//                searchedDeviceIds = searchedDeviceIds.Distinct().ToList();

//                var vehiclesDevicesIds = await Repositories.Vehicles.GetRelatedDeviceIds();
//                var employeeDevicesIds = await Repositories.Employees.GetRelatedDeviceIds();
//                var relatedDeviceIds = new List<object>();
//                relatedDeviceIds.AddRange(vehiclesDevicesIds);
//                relatedDeviceIds.AddRange(employeeDevicesIds);
//                relatedDeviceIds = relatedDeviceIds.Distinct().ToList();

//                paginationDevices = await Repositories.Devices.GetDeviceByParam(paginationRequest, includeDeviceIds: relatedDeviceIds,
//                    addDeviceIds: searchedDeviceIds);
//                break;
//            }
//        default: // = RelationStatus.All:
//            {
//                paginationDevices = await Repositories.Devices.GetDeviceByParam(paginationRequest);
//                break;
//            }
//    }

//    var devicesResponse = await GenerateDeviceTelematicsResponse(paginationDevices.Items);

//    var result = new DevicesPaginationResult(devicesResponse.ToArray(), paginationDevices.PageInfo);
//    return new PlatformResponse<DevicesPaginationResult>(result);
//}
#endregion