using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.GeoJsonObjectModel;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Microsoft.SqlServer.Types;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public static class GeoJsonGenerator
    {
        private const long SQUARE = 10000000000000;

        public static GeoJsonGeometry<GeoJson2DGeographicCoordinates> GenerateGeoInfo(GeozoneType type, double? bufferThickness, Point[] points)
        {
            if (points == null || !points.Any())
                return null;

            if (!bufferThickness.HasValue)
                bufferThickness = 1;

            // SqlGeography
            var wkt = GenerateSqlGeographyString(type, points);
            var zone = SqlGeography.Parse(wkt);
           
            // Площадь Канады 9937350333384  второй страны по величине.
            // Если направление полигона в Microsoft.SqlServer.Types неправильное, 
            // то площадь будет считатся через экватор и результат будет более 10000000000000, соотвественно его надо развернуть
            if (zone.STArea() > SQUARE)
                zone = zone.ReorientObject();

            // Буферизируем
            zone = zone.STBuffer(bufferThickness.Value);

            var coordinates = GenerateGeoJson2DGeographicCoordinateses(zone);
            var geoZoneCoordinates = new GeoJsonLinearRingCoordinates<GeoJson2DGeographicCoordinates>(coordinates);
            var geoInfo = GeoJson.Polygon(GeoJson.PolygonCoordinates(geoZoneCoordinates));

            return geoInfo;
        }

        /// <summary>
        /// Generate Linear
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static GeoJsonGeometry<GeoJson2DGeographicCoordinates> GenerateGeoInfoLinear(Point[] points)
        {
            var coordinates = GenerateGeoJson2DGeographicCoordinateses(points);
            var geoInfo = GeoJson.LineString(coordinates);
            return geoInfo;
        }

        /// <summary>
        /// Generate Round
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static GeoJsonGeometry<GeoJson2DGeographicCoordinates> GenerateGeoInfoRound(Point[] points)
        {
            var geoInfo = GeoJson.Point(new GeoJson2DGeographicCoordinates(points[0].Longitude, points[0].Latitude));
            return geoInfo;
        }

        /// <summary>
        /// Generate Polygon
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static GeoJsonGeometry<GeoJson2DGeographicCoordinates> GenerateGeoInfoPolygon(Point[] points)
        {
            var coordinates = GenerateGeoJson2DGeographicCoordinateses(points);
            var geoZoneCoordinates = new GeoJsonLinearRingCoordinates<GeoJson2DGeographicCoordinates>(coordinates);
            var geoInfo = GeoJson.Polygon(GeoJson.PolygonCoordinates(geoZoneCoordinates));
            return geoInfo;
        }

        /// <summary>
        /// Generate GeoJson2DGeographicCoordinates Array
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private static GeoJson2DGeographicCoordinates[] GenerateGeoJson2DGeographicCoordinateses(Point[] points)
        {
            var result = points.Select(p => new GeoJson2DGeographicCoordinates(p.Longitude, p.Latitude)).ToArray();
            return result;
        }

        private static GeoJson2DGeographicCoordinates[] GenerateGeoJson2DGeographicCoordinateses(SqlGeography geo)
        {
            var result = new List<GeoJson2DGeographicCoordinates>();
            for (int i = 1; i < geo.STNumPoints() + 1; i++)
                result.Add(new GeoJson2DGeographicCoordinates((geo.STPointN(i).Long.Value > 180) ? geo.STPointN(i).Long.Value - 360 : geo.STPointN(i).Long.Value, geo.STPointN(i).Lat.Value));
            
            return result.ToArray();
        }

        private static string GenerateSqlGeographyString(GeozoneType type, Point[] points)
        {
            switch (type)
            {
                case GeozoneType.Linear:
                    return "LINESTRING (" + string.Join(",", points.Select(i => string.Concat(i.Longitude, ' ', i.Latitude).Replace(",", ".")).ToArray()) + ")";
                case GeozoneType.Round:
                    return "POINT (" + string.Join(",", points.Select(i => string.Concat(i.Longitude, ' ', i.Latitude).Replace(",", ".")).ToArray()) + ")";
                case GeozoneType.Polygon:
                    return "POLYGON ((" + string.Join(",", points.Select(i => string.Concat(i.Longitude, ' ', i.Latitude).Replace(",", ".")).ToArray()) + "))";
                default:
                    return null;
            }
        }
    }
}
