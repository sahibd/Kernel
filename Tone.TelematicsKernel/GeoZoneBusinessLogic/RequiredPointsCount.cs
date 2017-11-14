using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel.Model;

namespace Tone.TelematicsKernel.GeoZoneBusinessLogic
{
    /// <summary>
    /// Тип геозоны
    /// </summary>
    internal static class GeozoneTypePointCount
    {
        private static readonly Dictionary<GeozoneType, RequiredPointsCount> PointCountDictionary = new Dictionary<GeozoneType, RequiredPointsCount>();

        static GeozoneTypePointCount()
        {
            // Линейная - две и более точек
            PointCountDictionary.Add(GeozoneType.Linear, new RequiredPointsCount {MinPointsCount = 2, MaxPointsCount = int.MaxValue });
            // Круглая - Точечная - одна точка
            PointCountDictionary.Add(GeozoneType.Round, new RequiredPointsCount {MinPointsCount = 1, MaxPointsCount = 1});
            // Многоугольник - 4 и более точек
            PointCountDictionary.Add(GeozoneType.Polygon, new RequiredPointsCount {MinPointsCount = 4, MaxPointsCount = int.MaxValue});
        }

        internal static RequiredPointsCount GetGeozoneTypePointCount(GeozoneType? geozoneType)
        {
            if (!geozoneType.HasValue)
                return new RequiredPointsCount { MaxPointsCount = 0, MinPointsCount = 0 };

            return PointCountDictionary.ContainsKey(geozoneType.Value) ? PointCountDictionary[geozoneType.Value] : null;
        }
    }

    internal class RequiredPointsCount
    {
        public int MinPointsCount { get; set; }
        public int MaxPointsCount { get; set; }
    }
}
