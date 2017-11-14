using AutoMapper;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Annotations;
using Tone.Core.Data.NewTrackEvent;
using Tone.Core.Data.TrackEvents;
using Tone.Core.Enums;
using Tone.Core.Extensions;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.BusinessObjects.Model;
using Tone.Core.Subsystems.Reports.Model;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.WebContext;
using Tone.Data.Mongo.Base.Extensions;
using Tone.SegmentStatistic.DriveStyle.Criteria;
using Tone.SegmentStatistic.General.Segment;

namespace Tone.TelematicsKernel
{
    public partial class DevicesModuleCore : TelematicsKernelSubsystemModule, IDevicesModuleCore
    {
        /// <summary>
        /// Возврат записей по списку id устройств
        /// </summary>
        /// <param name="request">Запрос с параметрами</param>
        /// <returns>Набор GPS записей c доп. информацией</returns>
        public async Task<PlatformResponse<GpsRecordsResponse>> GetFullRecordsByDevices(RecordsByDevicesRequest request)
        {
            DemandAuthorization();
            DemandValidation(request, necessary: true);

            // Костыль с поворотами
            if (request?.Events != null && request.Events.Contains(RoadEventType.SharpTurn))
            {
                var eventTypes = request.Events.ToList();
                eventTypes.Add(RoadEventType.SharpTurnRight);
                eventTypes.Add(RoadEventType.SharpTurnLeft);
                request.Events = eventTypes.ToArray();
            }

            // ReSharper disable once PossibleNullReferenceException
            var tasks = request.DeviceIds.Select(async id =>
                await GetFullRecordsByDeviceCore(id, request.DateStart.Value, request.DateEnd.Value, request.Events, request.EventId, request.ShowPlayerInfo));

            var deviceRecords = await Task.WhenAll(tasks);

            var response = new GpsRecordsResponse { Devices = deviceRecords };
            return new PlatformResponse<GpsRecordsResponse>(response);
        }

        private async Task<DeviceRecordsRm> GetFullRecordsByDeviceCore(object id, DateTime from, DateTime to,
            [CanBeNull] RoadEventType[] eventsTypeFilter, [CanBeNull] string eventId, bool showPlayerInfo)
        {
            //Get Full Records -- Запрашиваем телематические данные
            var deviceId = Repositories.Devices.ToId(id);
            var fullRecords = await Repositories.FullRecords.GetFullRecordsByDeviceTime(from, to, deviceId);
            fullRecords = fullRecords.DistinctBy(x => x.DeviceTime).ToList();
            var resourceEventInfo = await GetResourceByDeviceId(deviceId);

            // Поиск дорожных событий из БД в соответствии с заданными параметрами
            List<ITrackEvent> trackEvents = new List<ITrackEvent>();
            if (eventId != null)
            {
                var eventObjectId = Repositories.TrackEvent.ToId(eventId);
                var trackEvent = await Repositories.TrackEvent.GetById(eventObjectId);
                trackEvents.Add(trackEvent);
            }
            else
            {
                // В связи с переходом на runtime-определение дорожных событий, кроме запрошенных явно, данный код закомментирован
                //trackEvents = await Repositories.TrackEvent.GetByInterval(from, to, deviceId);
            }

            if (fullRecords.Count > 0)
            {
                // Агрегация
                var aggregator = new GeneralSegmentStatisticAggregator(Repositories.AnalyticIntervals);
                var analyticInvervalResult = await aggregator.Aggregate(from, to, deviceId);

                // Стиль вождения
                var driveStylePenaltyScores = await Repositories.DriveStylePenaltyScores.GetPenaltyScores();

                var recordsResult = GetFullRecordsByDeviceId(Repositories.Devices.ToId(id),
                    eventsTypeFilter, analyticInvervalResult?.Events ?? new BaseEventPart[] { }, eventId, fullRecords, trackEvents,
                    driveStylePenaltyScores, resourceEventInfo);

                if (recordsResult.Code != ResultCode.Ok)
                {
                    return DeviceRecordsRm.CreateWithError(id, recordsResult.Code, recordsResult.Message);
                }

                // В случае запроса на отображение информации для плеера,
                // заполняем поездки и индикаторы фиктивными значениями
                if (showPlayerInfo)
                {
                    // Если точек не найдено - возвращаем пустую статистику
                    if (!recordsResult.Points.Any())
                    {
                        recordsResult.Indicators = new IndicatorsModel(); // По умолчанию все поля будут null
                        recordsResult.Trips = new TripModel[] { };
                        return recordsResult;
                    }

                    recordsResult.Indicators = new IndicatorsModel();

	                if (analyticInvervalResult?.IntervalSummary != null)
	                {
		                recordsResult.Indicators.FuelSpent = analyticInvervalResult.IntervalSummary.FuelSpentTotal;
		                recordsResult.Indicators.MovementDuration =
			                analyticInvervalResult.IntervalSummary.TripsTotalDurationTimespan;
		                recordsResult.Indicators.Mileage =
			                Math.Round(analyticInvervalResult.IntervalSummary.TotalMileage / 100d) /
			                10d; // Преобразование к километрам из метров

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.Discharge))
		                {
			                recordsResult.Indicators.FuelDrainAmount = analyticInvervalResult.IntervalSummary.DrainTotalAmount;
			                recordsResult.Indicators.FuelDrains = analyticInvervalResult.IntervalSummary.DrainTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.Refueling))
		                {
			                recordsResult.Indicators.RefuelingAmount =
				                analyticInvervalResult.IntervalSummary.RefuelingTotalAmount;
			                recordsResult.Indicators.RefuelingsNumber =
				                analyticInvervalResult.IntervalSummary.RefuelingTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.IdleEvent))
		                {
			                recordsResult.Indicators.IdlingsNumber = analyticInvervalResult.IntervalSummary.IdleTotalCount;
			                recordsResult.Indicators.IdlingDuration =
				                analyticInvervalResult.IntervalSummary.IdleTotalDurationTimespan;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.StopEvent))
		                {
			                recordsResult.Indicators.ParkingsNumber = analyticInvervalResult.IntervalSummary.StopsTotalCount;
			                recordsResult.Indicators.ParkingDuration =
				                analyticInvervalResult.IntervalSummary.StopsTotalDurationTimespan;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.SharpTurnLeft) ||
		                    eventsTypeFilter.Contains(RoadEventType.SharpTurn))
		                {
			                recordsResult.Indicators.SharpLeftTurnsNumber =
				                analyticInvervalResult.IntervalSummary.SharpLeftTurnTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.SharpTurnRight) ||
		                    eventsTypeFilter.Contains(RoadEventType.SharpTurn))
		                {
			                recordsResult.Indicators.SharpRightTurnsNumber =
				                analyticInvervalResult.IntervalSummary.SharpRightTurnTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.SharpSpeedup))
		                {
			                recordsResult.Indicators.SharpSpeedupsNumber =
				                analyticInvervalResult.IntervalSummary.SharpAccelerationTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.SharpBraking))
		                {
			                recordsResult.Indicators.SharpBrakingsNumber =
				                analyticInvervalResult.IntervalSummary.SharpBrakingTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.TrafficRulesSpeedLimit) ||
		                    eventsTypeFilter.Contains(RoadEventType.UserSpeedlimit))
		                {
			                recordsResult.Indicators.OverSpeedsNumber =
				                analyticInvervalResult.IntervalSummary.OverSpeedTotalCount;
		                }

		                if (eventsTypeFilter == null || eventsTypeFilter.Contains(RoadEventType.PanicButton))
		                {
			                recordsResult.Indicators.PanicButtonsNumber =
				                recordsResult.Points?.Count(p => p.Events != null &&
				                                                 p.Events.Any(e => e.EventType == RoadEventType.PanicButton));
		                }
	                }

	                recordsResult.Trips = analyticInvervalResult?.TripsParts?.Select(trip => new TripModel { DateStart = trip.PartStart.Time, DateEnd = trip.PartEnd.Time }).OrderBy(x => x.DateStart).ToArray();
                }

                return recordsResult;
            }
            else
            {
                return new DeviceRecordsRm()
                {
                    Code = ResultCode.Ok,
                    DeviceId = deviceId,
                    Indicators = new IndicatorsModel(),
                    Trips = new TripModel[] { },
                    Points = new TrackPositionRm[] { }
                };
            }
        }

        private async Task<ResourceInfo> GetResourceByDeviceId(object deviceId)
        {
            // fixme nika: позже откорректировать на поиск ресурса 1 или 2 уровня и оставить данный код только в модуле Ресурсов

            var employee = await Repositories.Employees.GetEmployeeByDevice(deviceId);
            if (employee != null)
            {
                var viewCardHelper = rc.PlatformComponents.BusinessObjectsSubsystem.CreateResourceViewCardHelper();
                var employeeViewCard = await viewCardHelper.GenerateEmployeeResourceViewCard(employee);
                return new ResourceInfo
                {
                    Id = employee.Id,
                    Name = employee.GetName(),
                    PrimaryTitle = employeeViewCard.PrimaryTitle,
                    SecondaryTitle = employeeViewCard.SecondaryTitle,
                    Type = "Employee"
                };
            }

            var vehicle = await Repositories.Vehicles.GetVehicleByDevice(deviceId);
            if (vehicle != null)
            {
                var viewCardHelper = rc.PlatformComponents.BusinessObjectsSubsystem.CreateResourceViewCardHelper();
                var vehicleViewCard = await viewCardHelper.GenerateVehicleResourceViewCard(vehicle);
                return new ResourceInfo
                {
                    Id = vehicle.Id,
                    Name = vehicle.Name,
                    PrimaryTitle = vehicleViewCard.PrimaryTitle,
                    SecondaryTitle = vehicleViewCard.SecondaryTitle,
                    Type = "Vehicle"
                };
            }

            return null;
        }

        private DeviceRecordsRm GetFullRecordsByDeviceId(object deviceId, [CanBeNull] RoadEventType[] eventsTypeFilter, 
            BaseEventPart[] analyticIntervalEvents, [CanBeNull] string eventIdFilter, List<FullRecord<ObjectId?>> fullRecords, 
            List<ITrackEvent> trackEvents, IDriveStylePenaltyScores driveStylePenaltyScores, ResourceInfo resourceEventInfo)
        {
            // Create DeviceRecords (Outgoing model)
            var deviceRecords = CreateDeviceFullRecords(deviceId, fullRecords, trackEvents, eventsTypeFilter, analyticIntervalEvents, 
                eventIdFilter, driveStylePenaltyScores, resourceEventInfo);
            return deviceRecords;
        }   

        /// <summary>
        ///
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="fullRecords"></param>
        /// <param name="trackEvents">
        ///     Все дорожные события на данном периоде
        ///     sysoevp todo: Оптимизировать в будущем и передавать только те дорожные события, которые соответствуют фильтру
        /// </param>
        /// <param name="eventsFilter"></param>
        /// <param name="analyticIntervalEvents"></param>
        /// <param name="eventId"></param>
        /// <param name="driveStylePenaltyScores"></param>
        /// <param name="resourceEventInfo"></param>
        /// <returns></returns>
        private DeviceRecordsRm CreateDeviceFullRecords(object deviceId, List<FullRecord<ObjectId?>> fullRecords, List<ITrackEvent> trackEvents, 
            [CanBeNull] RoadEventType[] eventsFilter, BaseEventPart[] analyticIntervalEvents, [CanBeNull] string eventId, 
            IDriveStylePenaltyScores driveStylePenaltyScores, ResourceInfo resourceEventInfo)
        {
            var deviceRecords = DeviceRecordsRm.Create(deviceId);

            var list = new List<TrackPositionRm>();
            var roadEventsDeterminer = new RoadEventsDeterminer<TrackEventRm>();
            foreach (var fullRecord in fullRecords)
            {
                // Преобразуем TrackEvents в TrackEventsRm
                var trackEventsRm = new List<TrackEventRm>();

                if (fullRecord.HasRoadEvents == true)
                    if (eventId == null)
                    {
                        trackEventsRm = trackEvents?
                            // Только те события, которые проходят фильтр, либо все события, если events == null
                            // todo Maksim R. вынести запрос в репозиторий для оптимизации
                            .Where(
                                e =>
                                    ((eventsFilter?.Any(eventInFilter => eventInFilter == e.EventType) ?? false) || eventsFilter == null) // фильтр по типу события
                                    && e.RelatedRecordId.Equals(fullRecord.Id)) // соответствие идентификатору
                            .Select(TrackEventRm.Create).ToList();
                    }
                    else
                    {
                        object eventObjectId = eventId.ToObjectId();
                        trackEventsRm = trackEvents?
                            // Только те события, которые проходят фильтр, либо все события, если events == null
                            // todo Maksim R. вынести запрос в репозиторий для оптимизации
                            .Where(e => e.Id.Equals(eventObjectId))
                            .Select(TrackEventRm.Create).ToList();
                    }



                // События из анатилического интервала
                //
                var fullRecordEvents = analyticIntervalEvents?.Where(x => x.PartStart.Time == fullRecord.DeviceTime).ToList() ?? new List<BaseEventPart>();
                // События из анатилического интервала - начавшиеся до начала запрошенного интервала вставляются в первую точку (исключительная ситуация)
                if (Equals(fullRecord, fullRecords.FirstOrDefault()))
                {
                    var earlyFullRecordEvents = analyticIntervalEvents?.Where(x => x.PartStart.Time < fullRecord.DeviceTime).ToList() ?? new List<BaseEventPart>();
                    fullRecordEvents.AddRange(earlyFullRecordEvents);
                }
                var speedLimit = driveStylePenaltyScores.OverSpeeding.OverSpeedingValue;
                foreach (var fullRecordEvent in fullRecordEvents)
                {
                    var analyticIntervalEvent = MapTrackEvent(fullRecord, fullRecordEvent, resourceEventInfo, speedLimit);
                    if ((analyticIntervalEvent != null) && (eventsFilter == null || eventsFilter.Any(x => x == analyticIntervalEvent.EventType)))
                        trackEventsRm.Add(analyticIntervalEvent);
                }

                // Определение градиента для точки
                int? gradation = null;
                var overspeeds = analyticIntervalEvents?.Where(x =>
                        x.EventType == AnalyticEventType.OverSpeedingCriteria && x.PartStart.Time <= fullRecord.DeviceTime &&
                        x.PartEnd.Time >= fullRecord.DeviceTime).ToList() ?? new List<BaseEventPart>();
                foreach (var overspeed in overspeeds)
                {
                    var overspeedCriteria = overspeed as CriteriaThresholdEventPart;
                    if (overspeedCriteria != null)
                    {
                        if (!overspeedCriteria.IsUserThreshold && (gradation == null || gradation.Value < overspeedCriteria.SettingsIntervalNumber))
                        {
                            gradation = overspeedCriteria.SettingsIntervalNumber;
                        }
                    }
                }

                // Идентификация остальных дорожных событий на выдаче, согласно настройкам отображения
                var runtimeDeterminedTrackEventsRm = roadEventsDeterminer.DetermineRoadEvents(fullRecord, driveStylePenaltyScores, resourceEventInfo, eventsFilter);
                trackEventsRm.AddRange(runtimeDeterminedTrackEventsRm);

                list.Add(new TrackPositionRm
                {
                    Time = fullRecord.DeviceTime,
                    Lat = fullRecord.Latitude,
                    Lon = fullRecord.Longitude,
                    Course = fullRecord.Course,
                    Spd = fullRecord.Speed,
                    Sat = fullRecord.Satellites,
                    Events = (trackEventsRm.Count > 0) ? trackEventsRm.ToArray() : null,
                    Gradation = gradation
                });
            }

            deviceRecords.Points = list.OrderBy(d => d.Time).ToArray();
            return deviceRecords;
        }

        // Метод маппинга BaseEventPart из аналитического интервала в объект для выдачи
        private TrackEventRm MapTrackEvent(FullRecord<ObjectId?> fullRecord, BaseEventPart fullRecordEvent, ResourceInfo resourceInfo, double speedLimit)
        {
            var eventType = RoadEventType.Undefined;
            var actualValue = string.Empty;
            var conditionalValue = string.Empty;
            int? gradation = null;

            switch (fullRecordEvent.EventType)
            {
                case AnalyticEventType.Idle:
                    {
                        eventType = RoadEventType.IdleEvent;
                        actualValue = fullRecordEvent.PartStart.Speed.ToString("0.## км/ч");
                        break;
                    }
                case AnalyticEventType.Stop:
                    eventType = RoadEventType.StopEvent;
                    break;

                case AnalyticEventType.OverAccelerationCriteria:
                    {
                        var criteria = fullRecordEvent as CriteriaThresholdEventPart;
                        if (criteria != null)
                        {
                            actualValue = (criteria.CriteriaThresholdValue).ToString("0.## mg");
                            conditionalValue = (criteria.CriteriaThreshold).ToString("0.## mg");
                            var eventInfo = OverAccelerationCriteria.GetCriteriaStaticInformationByCriteriaCode(criteria?.TypeCriteriaCode);
                            if (eventInfo.AccelerationDirection == AccelerationDirection.X && eventInfo.Sign < 0)
                                eventType = RoadEventType.SharpBraking;
                            if (eventInfo.AccelerationDirection == AccelerationDirection.X && eventInfo.Sign > 0)
                                eventType = RoadEventType.SharpSpeedup;
                            if (eventInfo.AccelerationDirection == AccelerationDirection.Y && eventInfo.Sign < 0)
                                eventType = RoadEventType.SharpTurnLeft;
                            if (eventInfo.AccelerationDirection == AccelerationDirection.Y && eventInfo.Sign > 0)
                                eventType = RoadEventType.SharpTurnRight;
                            gradation = criteria?.SettingsIntervalNumber;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    }
                case AnalyticEventType.OverSpeedingCriteria:
                    {
                        var criteria = fullRecordEvent as CriteriaThresholdEventPart;
                        if (criteria != null)
                        {
                            actualValue = criteria.CriteriaThresholdValue.ToString("0.## км/ч");
                            conditionalValue = criteria.IsUserThreshold ? speedLimit.ToString("0.## км/ч") : fullRecord.xgeo_Speedlimit.ToString("0.## км/ч");
                            gradation = criteria.SettingsIntervalNumber;
                            eventType = criteria.IsUserThreshold ? RoadEventType.UserSpeedlimit : RoadEventType.TrafficRulesSpeedLimit;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    }
                default:
                    return null;
            }

            var roadEvent = new TrackEventRm
            {
                ActualValue = actualValue,
                Address = fullRecord.xgeo_Address,
                Border = TrackEvent.TrackEventBorder.Single,
                ConditionValue = conditionalValue,
                EventType = eventType,
                Gradation = gradation,
                ResourcePrimaryTitle = resourceInfo?.PrimaryTitle,
                ResourceSecondaryTitle = resourceInfo?.SecondaryTitle
            };

            // На данный момент градиент должен отображаться только для превышений по ПДД
            if (roadEvent.EventType != RoadEventType.TrafficRulesSpeedLimit)
                roadEvent.Gradation = null;

            return roadEvent;
        }

        /// <summary>
        /// Получение подробной информации по дорожному событию
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<RoadEventViewModel>> GetSingleRoadEvent(object eventId)
        {
            DemandAuthorization();

            var roadEventId = eventId.ToNullableObjectId();
            var trackEvent = await Repositories.TrackEvent.GetById(roadEventId);
            if (trackEvent == null)
                return new PlatformResponse<RoadEventViewModel>(ResultCode.NotFound, "Road event not found");

            RoadEventViewModel response = Mapper.Map<RoadEventViewModel>(trackEvent);

            string objectName = null;

            // Имя связанного с событием объекта, который в MVP1 может быть только геозона
            if ((trackEvent.EventType == RoadEventType.GeozoneEntrance) ||
                (trackEvent.EventType == RoadEventType.GeozoneExit) ||
                (trackEvent.EventType == RoadEventType.GeozoneStanding))
            {
                var geozoneId = trackEvent.GetObjectIdValue();
                if (geozoneId != null)
                {
                    var geozone = await rc.PlatformComponents.BusinessObjectsSubsystem.Repositories.Geozones.GetById(
                        rc.PlatformComponents.BusinessObjectsSubsystem.Repositories.Geozones.ToId(geozoneId));
                    if (geozone != null)
                    {
                        objectName = geozone.Name;
                    }
                }
            }

            //
            // Получаем связанный ресурс (сотрудника или ТС)
            //
            RelatedResourceInfoForRoadEvent relatedResourceInfo = null;
            if (trackEvent.RelatedResourceId != null)
            {
                //recordTrackEvent.RelatedResourceId = "59297f080a2aa657004d4ef6";
                relatedResourceInfo = await GetRelatedResourceInfoForRoadEvent(trackEvent.RelatedResourceId, typeof(Vehicle));
                if (relatedResourceInfo == null)
                    relatedResourceInfo = await GetRelatedResourceInfoForRoadEvent(trackEvent.RelatedResourceId, typeof(Employee));
            }

            response.ObjectName = objectName;
            response.RelatedResourceName = relatedResourceInfo?.ResourceName;
            response.RelatedResourceType = relatedResourceInfo?.ResourceSubtype;

            return new PlatformResponse<RoadEventViewModel>(response);
        }

        private async Task<RelatedResourceInfoForRoadEvent> GetRelatedResourceInfoForRoadEvent(object resourceId, Type resourceType)
        {
            var resourceObjectId = resourceId.ToNullableObjectId();

            RelatedResourceInfoForRoadEvent resourceInfo = null;

            if (resourceType == typeof(Vehicle))
            {
                var vehicle = await Repositories.Vehicles.GetInfoForRoadEventById(resourceObjectId);
                if (vehicle != null)
                {
                    resourceInfo = new RelatedResourceInfoForRoadEvent
                    {
                        ResourceId = resourceObjectId.ToObjectId(),
                        ResourceName = vehicle.Name,
                        ResourceSubtype = null
                    };
                }
            }
            if (resourceType == typeof(Employee))
            {
                var employee = await Repositories.Employees.GetInfoForRoadEventById(resourceObjectId);
                if (employee != null)
                {
                    resourceInfo = new RelatedResourceInfoForRoadEvent
                    {
                        ResourceId = resourceObjectId.ToObjectId(),
                        ResourceName =
                            employee.Firstname + ((!string.IsNullOrEmpty(employee.Lastname)) ? " " : string.Empty) +
                            employee.Lastname,
                        ResourceSubtype = employee.Type?.ToString()
                    };
                }
            }

            return resourceInfo;
        }
    }

    public class RelatedResourceInfoForRoadEvent
    {
        /// <summary>
        /// Идентификатор ресурса
        /// </summary>
        public ObjectId ResourceId;

        /// <summary>
        /// Имя ресурса
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// Название подтипа ресурса - для сотрудника - водитель/курьер
        /// </summary>
        public string ResourceSubtype;
    }
}