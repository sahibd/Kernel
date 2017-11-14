using System;
using System.Collections.Generic;
using MongoDB.Bson;
using Tone.Core.Enums;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using TrackEvent = Tone.Core.Data.TrackEvents.TrackEvent;
using System.Linq;

namespace Tone.TelematicsKernel
{
    /// <summary>
    /// Функционал определения дорожных событий
    /// </summary>
    public class RoadEventsDeterminer<T>
    {
        // fixme nika: в данном классе много повторяющегося кода, который необходимо отрефакторить позже

        private FullRecord<ObjectId?> lastRecord = null;

        /// <summary>
        /// Определение возникновения событий в данной точке (точки трека должны передаваться последовательно!)
        /// </summary>
        public List<T> DetermineRoadEvents(FullRecord<ObjectId?> fullRecord,
            IDriveStylePenaltyScores driveStylePenaltyScores, ResourceInfo resourceInfo,
            RoadEventType[] filterEventTypes)
        {
            var events = new List<T>();

            // События, которые могут быть только при включенном зажигании
            if (fullRecord.Ignition != null && fullRecord.Ignition.Value)
            {
                // Превышение пользовательского порога скорости - определяется в аналитических интервалах
//                if (!driveStylePenaltyScores.OverSpeeding.IsRuleUse &&
//                    (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.UserSpeedlimit)))
//                {
//                    var roadEvent = DetermineUserSpeedLimit(fullRecord, driveStylePenaltyScores.OverSpeeding,
//                        resourceInfo);
//                    if (roadEvent != null) events.Add(roadEvent);
//                }

                // Превышение скорости по ПДД - определяется в аналитических интервалах
//                if (driveStylePenaltyScores.OverSpeeding.IsRuleUse &&
//                    (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.TrafficRulesSpeedLimit)))
//                {
//                    var roadEvent = DetermineRulesSpeedLimit(fullRecord,
//                        driveStylePenaltyScores.OverSpeeding.PenaltyScores,
//                        resourceInfo);
//                    if (roadEvent != null) events.Add(roadEvent);
//                }

                // Резкие ускорения - определяется в аналитических интервалах
//                if (driveStylePenaltyScores.SharpAccelerations != null &&
//                    (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.SharpSpeedup)))
//                {
//                    var roadEvent = DetermineSharpAccelerations(fullRecord,
//                        driveStylePenaltyScores.SharpAccelerations.PenaltyScores, resourceInfo);
//                    if (roadEvent != null) events.Add(roadEvent);
//                }

                // Резкие торможения - определяется в аналитических интервалах
//                if (driveStylePenaltyScores.SharpBrakings != null &&
//                    (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.SharpBraking)))
//                {
//                    var roadEvent = DetermineSharpBraking(fullRecord,
//                        driveStylePenaltyScores.SharpBrakings.PenaltyScores, resourceInfo);
//                    if (roadEvent != null) events.Add(roadEvent);
//                }

                // Резкие повороты - определяется в аналитических интервалах
//                if (driveStylePenaltyScores.SharpTurns != null &&
//                    (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.SharpTurn)))
//                {
//                    var roadEvent = DetermineSharpTurnRight(fullRecord, driveStylePenaltyScores.SharpTurns.PenaltyScores,
//                        resourceInfo);
//                    if (roadEvent != null) events.Add(roadEvent);
//                    else // Не может быть одновременно резкий поворот направо и налево
//                    {
//                        roadEvent = DetermineSharpTurnLeft(fullRecord, driveStylePenaltyScores.SharpTurns.PenaltyScores,
//                            resourceInfo);
//                        if (roadEvent != null) events.Add(roadEvent);
//                    }
//                }

                  // Холостой ход - определяется в аналитических интервалах
//                if (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.IdleEvent))
//                {
//                    var roadEvent = DetermineIdleEvent(fullRecord, resourceInfo);
//                    if (roadEvent != null) events.Add(roadEvent);
//                }
            }

            // Нажатие тревожной кнопки
            if ((filterEventTypes == null || filterEventTypes.Contains(RoadEventType.PanicButton)))
            {
                var panicButtonRoadEvent = DeterminePanicButtonEvent(fullRecord, resourceInfo);
                if (panicButtonRoadEvent != null) events.Add(panicButtonRoadEvent);
            }

//            // Стоянки - определяется в аналитических интервалах
//            if (filterEventTypes == null || filterEventTypes.Contains(RoadEventType.StopEvent))
//            {
//                var roadEvent = DetermineParkingEvent(fullRecord, resourceInfo);
//                if (roadEvent != null) events.Add(roadEvent);
//            }

            lastRecord = fullRecord;
            return events;
        }

        /// <summary>
        /// Проверка возникновения события превышения пользовательского порога скорости
        /// </summary>
        private T DetermineUserSpeedLimit(FullRecord<ObjectId?> fullRecord, ScoresOverSpeeding driveStyleSettings,
            ResourceInfo resourceInfo)
        {
            var roadEvent = default(T);

            if (fullRecord.Speed > driveStyleSettings.OverSpeedingValue &&
                (lastRecord == null || lastRecord.Speed <= driveStyleSettings.OverSpeedingValue))
            {
                var eventValues = new EventValues()
                {
                    ActualValue = fullRecord.Speed,
                    ConditionValue = driveStyleSettings.OverSpeedingValue,
                    Gradation = null,
                    Type = RoadEventType.UserSpeedlimit
                };
                roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }

            return roadEvent;
        }

        /// <summary>
        /// Проверка возникновения события превышения скорости по ПДД
        /// </summary>
        private T DetermineRulesSpeedLimit(FullRecord<ObjectId?> fullRecord, DriveStyleIntervalScale driveStyleSettings,
            ResourceInfo resourceInfo)
        {
            // Если нет превышения - то и проверять нечего
            var overspeed = fullRecord.Speed != null ? fullRecord.Speed.Value - fullRecord.xgeo_Speedlimit : 0;
            if (overspeed <= 0 || fullRecord.xgeo_Speedlimit == 0)
                return default(T);

            var roadEvent = default(T);

            // Проверяем интервалы от большему (с большему скоростью) к меньшему
            // Четвертый интервал
            if (driveStyleSettings.Interval4 != null &&
                overspeed > driveStyleSettings.Interval4.Start &&
                overspeed <= driveStyleSettings.Interval4.Finish &&
                (lastRecord == null ||
                 lastRecord.Speed - fullRecord.xgeo_Speedlimit <= driveStyleSettings.Interval4.Start))
            {
                var eventValues = new EventValues()
                {
                    ActualValue = fullRecord.Speed,
                    ConditionValue = fullRecord.xgeo_Speedlimit,
                    Gradation = 4,
                    Type = RoadEventType.TrafficRulesSpeedLimit
                };
                roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            else
            {
                // Третий интервал
                if (driveStyleSettings.Interval3 != null &&
                    overspeed > driveStyleSettings.Interval3.Start &&
                    overspeed <= driveStyleSettings.Interval3.Finish &&
                    (lastRecord == null ||
                     lastRecord.Speed - fullRecord.xgeo_Speedlimit <= driveStyleSettings.Interval3.Start))
                {
                    var eventValues = new EventValues()
                    {
                        ActualValue = fullRecord.Speed,
                        ConditionValue = fullRecord.xgeo_Speedlimit,
                        Gradation = 3,
                        Type = RoadEventType.TrafficRulesSpeedLimit
                    };
                    roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                }
                else
                {
                    // Второй интервал
                    if (driveStyleSettings.Interval2 != null &&
                        overspeed > driveStyleSettings.Interval2.Start &&
                        overspeed <= driveStyleSettings.Interval2.Finish &&
                        (lastRecord == null ||
                         lastRecord.Speed - fullRecord.xgeo_Speedlimit <= driveStyleSettings.Interval2.Start))
                    {
                        var eventValues = new EventValues()
                        {
                            ActualValue = fullRecord.Speed,
                            ConditionValue = fullRecord.xgeo_Speedlimit,
                            Gradation = 2,
                            Type = RoadEventType.TrafficRulesSpeedLimit
                        };
                        roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                    }
                    else
                    {
                        // Первый интервал не смотрим, т.к. за него не предусмотрено наказания
                        if (driveStyleSettings.Interval1 != null &&
                            overspeed > driveStyleSettings.Interval1.Start &&
                            overspeed <= driveStyleSettings.Interval1.Finish &&
                            (lastRecord == null ||
                             lastRecord.Speed - fullRecord.xgeo_Speedlimit <= driveStyleSettings.Interval1.Start))
                        {
                            var eventValues = new EventValues()
                            {
                                ActualValue = fullRecord.Speed,
                                ConditionValue = fullRecord.xgeo_Speedlimit,
                                Gradation = 1,
                                Type = RoadEventType.TrafficRulesSpeedLimit
                            };
                            roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                        }
                    }
                }
            }

            return roadEvent;
        }

        /// <summary>
        /// Проверка возникновения события нажатия тревожной кнопки
        /// </summary>
        private T DeterminePanicButtonEvent(FullRecord<ObjectId?> fullRecord, ResourceInfo resourceInfo)
        {
            if (fullRecord.PanicButton != null && fullRecord.PanicButton.Value
                && (lastRecord == null || lastRecord.PanicButton == null || !lastRecord.PanicButton.Value))
            {
                var eventValues = new EventValues()
                {
                    ActualValue = null,
                    ConditionValue = null,
                    Gradation = null,
                    Type = RoadEventType.PanicButton
                };
                return CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            return default(T);
        }

        /// <summary>
        /// Проверка возникновения события резкого ускорения (изменение по оси X в плюс)
        /// </summary>
        private T DetermineSharpAccelerations(FullRecord<ObjectId?> fullRecord,
            DriveStyleIntervalScale driveStyleSettings, ResourceInfo resourceInfo)
        {
            var roadEvent = default(T);

            // Умножение на 1000 переводит g. в mg. (изменение по оси X в плюс)
            var acceleration = fullRecord.AccelerationX * 1000;

            // Проверяем интервалы от большего (с большей скоростью) к меньшему
            // Четвертый интервал
            if (driveStyleSettings.Interval4 != null &&
                acceleration >= driveStyleSettings.Interval4.Start &&
                acceleration < driveStyleSettings.Interval4.Finish)
            {
                var eventValues = new EventValues()
                {
                    ActualValue = acceleration,
                    ConditionValue = driveStyleSettings.Interval4.Start,
                    Gradation = 3,
                    Type = RoadEventType.SharpSpeedup
                };
                roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            else
            {
                // Третий интервал
                if (driveStyleSettings.Interval3 != null &&
                    acceleration >= driveStyleSettings.Interval3.Start &&
                    acceleration < driveStyleSettings.Interval3.Finish)
                {
                    var eventValues = new EventValues()
                    {
                        ActualValue = acceleration,
                        ConditionValue = driveStyleSettings.Interval3.Start,
                        Gradation = 2,
                        Type = RoadEventType.SharpSpeedup
                    };
                    roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                }
                else
                {
                    // Второй интервал
                    if (driveStyleSettings.Interval2 != null &&
                        acceleration >= driveStyleSettings.Interval2.Start &&
                        acceleration < driveStyleSettings.Interval2.Finish)
                    {
                        var eventValues = new EventValues()
                        {
                            ActualValue = acceleration,
                            ConditionValue = driveStyleSettings.Interval2.Start,
                            Gradation = 1,
                            Type = RoadEventType.SharpSpeedup
                        };
                        roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                    }
                    else
                    {
                        // Первый интервал не проверяем
                        /*if (driveStyleSettings.Interval1 != null &&
                            acceleration >= driveStyleSettings.Interval1.Start &&
                            acceleration < driveStyleSettings.Interval1.Finish)
                        {
                            var eventValues = new EventValues()
                            {
                                ActualValue = acceleration,
                                ConditionValue = driveStyleSettings.Interval1.Start,
                                Gradation = 0,
                                Type = RoadEventType.SharpSpeedup
                            };
                            roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                        } */
                    }
                }
            }

            return roadEvent;
        }

        /// <summary>
        /// Проверка возникновения события резкого торможения (изменение по оси X в минус)
        /// </summary>
        private T DetermineSharpBraking(FullRecord<ObjectId?> fullRecord, DriveStyleIntervalScale driveStyleSettings,
            ResourceInfo resourceInfo)
        {
            var roadEvent = default(T);

            // Умножение на 1000 переводит g. в mg. (изменение по оси X в минус)
            var acceleration = -fullRecord.AccelerationX * 1000;

            // Проверяем интервалы от большему (с большему скоростью) к меньшему
            // Четвертый интервал
            if (driveStyleSettings.Interval4 != null &&
                acceleration >= driveStyleSettings.Interval4.Start &&
                acceleration < driveStyleSettings.Interval4.Finish)
            {
                var eventValues = new EventValues()
                {
                    ActualValue = -acceleration,
                    ConditionValue = driveStyleSettings.Interval4.Start,
                    Gradation = 3,
                    Type = RoadEventType.SharpBraking
                };
                roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            else
            {
                // Третий интервал
                if (driveStyleSettings.Interval3 != null &&
                    acceleration >= driveStyleSettings.Interval3.Start &&
                    acceleration < driveStyleSettings.Interval3.Finish)
                {
                    var eventValues = new EventValues()
                    {
                        ActualValue = -acceleration,
                        ConditionValue = driveStyleSettings.Interval3.Start,
                        Gradation = 2,
                        Type = RoadEventType.SharpBraking
                    };
                    roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                }
                else
                {
                    // Второй интервал
                    if (driveStyleSettings.Interval2 != null &&
                        acceleration >= driveStyleSettings.Interval2.Start &&
                        acceleration < driveStyleSettings.Interval2.Finish)
                    {
                        var eventValues = new EventValues()
                        {
                            ActualValue = -acceleration,
                            ConditionValue = driveStyleSettings.Interval2.Start,
                            Gradation = 1,
                            Type = RoadEventType.SharpBraking
                        };
                        roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                    }
                    else
                    {
                        // Первый интервал не проверяем
                        /*if (driveStyleSettings.Interval1 != null &&
                            acceleration >= driveStyleSettings.Interval1.Start &&
                            acceleration < driveStyleSettings.Interval1.Finish)
                        {
                            var eventValues = new EventValues()
                            {
                                ActualValue = -acceleration,
                                ConditionValue = driveStyleSettings.Interval1.Start,
                                Gradation = 0,
                                Type = RoadEventType.SharpBraking
                            };
                            roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                        } */
                    }
                }
            }

            return roadEvent;
        }

        /// <summary>
        /// Проверка возникновения события резкого поворота направо (изменение по оси Y в плюс)
        /// </summary>
        private T DetermineSharpTurnRight(FullRecord<ObjectId?> fullRecord, DriveStyleIntervalScale driveStyleSettings,
            ResourceInfo resourceInfo)
        {
            var roadEvent = default(T);

            // Умножение на 1000 переводит g. в mg. (изменение по оси Y в плюс)
            var acceleration = fullRecord.AccelerationY * 1000;

            // Проверяем интервалы от большему (с большему скоростью) к меньшему
            // Четвертый интервал
            if (driveStyleSettings.Interval4 != null &&
                acceleration >= driveStyleSettings.Interval4.Start &&
                acceleration < driveStyleSettings.Interval4.Finish)
            {
                var eventValues = new EventValues()
                {
                    ActualValue = acceleration,
                    ConditionValue = driveStyleSettings.Interval4.Start,
                    Gradation = 3,
                    Type = RoadEventType.SharpTurnRight
                };
                roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            else
            {
                // Третий интервал
                if (driveStyleSettings.Interval3 != null &&
                    acceleration >= driveStyleSettings.Interval3.Start &&
                    acceleration < driveStyleSettings.Interval3.Finish)
                {
                    var eventValues = new EventValues()
                    {
                        ActualValue = acceleration,
                        ConditionValue = driveStyleSettings.Interval3.Start,
                        Gradation = 2,
                        Type = RoadEventType.SharpTurnRight
                    };
                    roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                }
                else
                {
                    // Второй интервал
                    if (driveStyleSettings.Interval2 != null &&
                        acceleration >= driveStyleSettings.Interval2.Start &&
                        acceleration < driveStyleSettings.Interval2.Finish)
                    {
                        var eventValues = new EventValues()
                        {
                            ActualValue = acceleration,
                            ConditionValue = driveStyleSettings.Interval2.Start,
                            Gradation = 1,
                            Type = RoadEventType.SharpTurnRight
                        };
                        roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                    }
                    else
                    {
                        // Первый интервал не проверяем
                        /*if (driveStyleSettings.Interval1 != null &&
                            acceleration >= driveStyleSettings.Interval1.Start &&
                            acceleration < driveStyleSettings.Interval1.Finish)
                        {
                            var eventValues = new EventValues()
                            {
                                ActualValue = acceleration,
                                ConditionValue = driveStyleSettings.Interval1.Start,
                                Gradation = 0,
                                Type = RoadEventType.SharpTurnRight
                            };
                            roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                        } */
                    }
                }
            }

            return roadEvent;
        }

        /// <summary>
        /// Проверка возникновения события резкого поворота налево (изменение по оси Y в минус)
        /// </summary>
        private T DetermineSharpTurnLeft(FullRecord<ObjectId?> fullRecord, DriveStyleIntervalScale driveStyleSettings,
            ResourceInfo resourceInfo)
        {
            var roadEvent = default(T);

            // Умножение на 1000 переводит g. в mg. (изменение по оси Y в минус)
            var acceleration = -fullRecord.AccelerationY * 1000;

            // Проверяем интервалы от большему (с большему скоростью) к меньшему
            // Четвертый интервал
            if (driveStyleSettings.Interval4 != null &&
                acceleration >= driveStyleSettings.Interval4.Start &&
                acceleration < driveStyleSettings.Interval4.Finish)
            {
                var eventValues = new EventValues()
                {
                    ActualValue = acceleration,
                    ConditionValue = driveStyleSettings.Interval4.Start,
                    Gradation = 3,
                    Type = RoadEventType.SharpTurnLeft
                };
                roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            else
            {
                // Третий интервал
                if (driveStyleSettings.Interval3 != null &&
                    acceleration >= driveStyleSettings.Interval3.Start &&
                    acceleration < driveStyleSettings.Interval3.Finish)
                {
                    var eventValues = new EventValues()
                    {
                        ActualValue = acceleration,
                        ConditionValue = driveStyleSettings.Interval3.Start,
                        Gradation = 2,
                        Type = RoadEventType.SharpTurnLeft
                    };
                    roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                }
                else
                {
                    // Второй интервал
                    if (driveStyleSettings.Interval2 != null &&
                        acceleration >= driveStyleSettings.Interval2.Start &&
                        acceleration < driveStyleSettings.Interval2.Finish)
                    {
                        var eventValues = new EventValues()
                        {
                            ActualValue = acceleration,
                            ConditionValue = driveStyleSettings.Interval2.Start,
                            Gradation = 1,
                            Type = RoadEventType.SharpTurnLeft
                        };
                        roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                    }
                    else
                    {
                        // Первый интервал не проверяем
                        /*if (driveStyleSettings.Interval1 != null &&
                            acceleration >= driveStyleSettings.Interval1.Start &&
                            acceleration < driveStyleSettings.Interval1.Finish)
                        {
                            var eventValues = new EventValues()
                            {
                                ActualValue = acceleration,
                                ConditionValue = driveStyleSettings.Interval1.Start,
                                Gradation = 0,
                                Type = RoadEventType.SharpTurnLeft
                            };
                            roadEvent = CreateRoadEvent(fullRecord, eventValues, resourceInfo);
                        }*/
                    }
                }
            }

            return roadEvent;
        }

        /// <summary>
        /// Проверка возникновения события стоянки
        /// </summary>
        private T DetermineParkingEvent(FullRecord<ObjectId?> fullRecord, ResourceInfo resourceInfo)
        {
            if (fullRecord.Ignition != null && !fullRecord.Ignition.Value
                && (lastRecord == null || lastRecord.Ignition != null && lastRecord.Ignition.Value))
            {
                var eventValues = new EventValues()
                {
                    ActualValue = null,
                    ConditionValue = null,
                    Gradation = null,
                    Type = RoadEventType.StopEvent
                };
                return CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            return default(T);
        }

        /// <summary>
        /// Проверка возникновения события нажатия тревожной кнопки
        /// </summary>
        private T DetermineIdleEvent(FullRecord<ObjectId?> fullRecord, ResourceInfo resourceInfo)
        {
            if (fullRecord.Ignition != null && fullRecord.Ignition.Value && fullRecord.Speed < 5
                &&
                !(lastRecord == null || lastRecord.Ignition != null && lastRecord.Ignition.Value && lastRecord.Speed < 5))
            {
                var eventValues = new EventValues()
                {
                    ActualValue = null,
                    ConditionValue = null,
                    Gradation = null,
                    Type = RoadEventType.IdleEvent
                };
                return CreateRoadEvent(fullRecord, eventValues, resourceInfo);
            }
            return default(T);
        }



        private T CreateRoadEvent(FullRecord<ObjectId?> fullRecord, EventValues eventValues, ResourceInfo resourceInfo)
        {
            // fixme nika: пересмотреть этот момент
            if (typeof(T) == typeof(TrackEventRm))
            {
                var roadEvent = new TrackEventRm
                {
                    ActualValue = eventValues.GetActualValue(),
                    Address = fullRecord.xgeo_Address,
                    Border = TrackEvent.TrackEventBorder.Single,
                    ConditionValue = eventValues.GetConditionValue(),
                    EventType = eventValues.Type,
                    Gradation = eventValues.Gradation,
                    ResourcePrimaryTitle = resourceInfo?.PrimaryTitle,
                    ResourceSecondaryTitle = resourceInfo?.SecondaryTitle
                };
                return (T) Activator.CreateInstance(typeof(T), new object[] {roadEvent});
            }
            if (typeof(T) == typeof(ResourcesRoadEventsRm))
            {
                var roadEvent = new ResourcesRoadEventsRm
                {
                    ActualValue = eventValues.GetActualValue(),
                    Address = fullRecord.xgeo_Address,
                    ConditionalValue = eventValues.GetConditionValue(),
                    EventTime = fullRecord.DeviceTime,
                    RelatedResourceId = resourceInfo?.Id,
                    RelatedResourceType = resourceInfo?.Type,
                    RelatedResourceName = resourceInfo?.Name,
                    RoadEventType = eventValues.Type,
                    Gradation = eventValues.Gradation,
                    Latitude = fullRecord.Latitude,
                    Longitude = fullRecord.Longitude,
                };
                return (T) Activator.CreateInstance(typeof(T), new object[] {roadEvent});
            }

            throw new Exception(
                $"RoadEventsDeterminer can't create {typeof(T).Name} object, supports only TrackEventRm or ResourcesRoadEventsRm");
        }

        class EventValues
        {
            public object ActualValue { get; set; }
            public object ConditionValue { get; set; }
            public int? Gradation { get; set; }
            public RoadEventType Type { get; set; }

            public string GetConditionValue()
            {
                return ConditionValue == null ? null : $"{ConditionValue} {GetMeasure()}";
            }

            public string GetActualValue()
            {
                return ActualValue == null ? null : $"{ActualValue} {GetMeasure()}";
            }

            // fixme nika: с наименованием нужно придумать более элегантное решение
            string GetMeasure()
            {
                switch (Type)
                {
                    case RoadEventType.UserSpeedlimit:
                    case RoadEventType.TrafficRulesSpeedLimit:
                        return "км/ч";
                    case RoadEventType.PanicButton:
                        return string.Empty;
                    case RoadEventType.SharpBraking:
                    case RoadEventType.SharpTurn:
                    case RoadEventType.SharpTurnRight:
                    case RoadEventType.SharpTurnLeft:
                    case RoadEventType.SharpSpeedup:
                        return "mg.";
                }
                return string.Empty;
            }
        }
    }

    public class ResourceInfo
    {
        public object Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string PrimaryTitle { get; set; }
        public string SecondaryTitle { get; set; }
    }
}
