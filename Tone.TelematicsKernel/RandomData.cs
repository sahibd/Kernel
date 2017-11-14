using System;
using System.Collections.Generic;
using System.Linq;
using Tone.Core.Annotations;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;

namespace Tone.TelematicsKernel
{
    internal static class RandomData
    {
        private static readonly Random Random = new Random();

        static RandomData()
        {
        }

        public static int GetRandomInt(int minValue, int maxValue) => Random.Next(minValue, maxValue + 1);
        
        public static TimeSpan GetRandomTimeSpan() => new TimeSpan(GetRandomInt(0, 2), GetRandomInt(0, 23), GetRandomInt(0, 59), GetRandomInt(0, 59));
        
        public static IndicatorsModel GetRandomDeviceIndicators([NotNull] RoadEventType[] events)
        {
            // Обязательные индикаторы
            //
            var indicators = new IndicatorsModel
            {
                Mileage = GetRandomInt(20, 300),
                MovementDuration = new TimeSpan(0, GetRandomInt(1, 20), GetRandomInt(0, 60)),
                FuelSpent = GetRandomInt(3, 50),
            };

            // Индикаторы в соответствии с типом события
            //
            if (events.Contains(RoadEventType.IdleEvent))
            {
                indicators.IdlingDuration = GetRandomTimeSpan();
                indicators.IdlingsNumber = GetRandomInt(1, 50);
            }
            if (events.Contains(RoadEventType.SharpTurnSpeedLimit))
            {
                indicators.OverSpeedsInRightTurnsNumber = GetRandomInt(1, 50);
                indicators.OverSpeedsInLeftTurnsNumber = GetRandomInt(1, 50);
            }
            if (events.Contains(RoadEventType.Discharge))
            {
                indicators.FuelDrainAmount = GetRandomInt(1, 50);
                indicators.FuelDrains = GetRandomInt(1, 5);
            }
            if (events.Contains(RoadEventType.TrafficRulesSpeedLimit) || events.Contains(RoadEventType.UserSpeedlimit))
            {
                indicators.OverSpeedsNumber = GetRandomInt(0, 20);
            }
            if (events.Contains(RoadEventType.PanicButton))
            {
                indicators.PanicButtonsNumber = GetRandomInt(0, 10);
            }
            if (events.Contains(RoadEventType.StopEvent))
            {
                indicators.ParkingDuration = GetRandomTimeSpan();
                indicators.ParkingsNumber = GetRandomInt(1, 50);
            }
            if (events.Contains(RoadEventType.SharpBraking))
            {
                indicators.SharpBrakingsNumber = GetRandomInt(0, 20);
            }
            if (events.Contains(RoadEventType.SharpSpeedup))
            {
                indicators.SharpSpeedupsNumber = GetRandomInt(1, 20);
            }
            if (events.Contains(RoadEventType.Refueling))
            {
                indicators.RefuelingsNumber = GetRandomInt(1, 10);
                indicators.RefuelingAmount = GetRandomInt(1, 400);
            }
            if (events.Contains(RoadEventType.SharpTurn))
            {
                indicators.SharpRightTurnsNumber = GetRandomInt(0, 300);
                indicators.SharpLeftTurnsNumber = GetRandomInt(0, 300);
            }

            return indicators;
        }

        public static TripModel[] GetRandomDeviceTrips(TrackPositionRm[] points)
        {
            if (points.Length < 2)
                return new TripModel[0];

            var dateStart = points.First().Time;
            var dateEnd = points.Last().Time;

            int tripsCount = GetRandomInt(1, Math.Min(5, points.Length / 2));
            int pointsToInterval = points.Length / tripsCount;

            var tripsList = new List<TripModel>();

            for (int i = 0; i < tripsCount; i += 1)
            {
                tripsList.Add(new TripModel { DateStart = points[i*pointsToInterval].Time, DateEnd = points[(i + 1) * pointsToInterval - 1].Time });
            }

            tripsList.First().DateStart = dateStart;
            tripsList.Last().DateEnd = dateEnd;

            return tripsList.ToArray();
        }
    }
}