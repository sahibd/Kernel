using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.TelematicsKernel.Data.Repository.Mongo;
using Tone.TelematicsKernel.Data.Repository.Mongo.GeoProviders;

namespace Tone.TelematicsKernel.ConsoleUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            int geocoderDistance = 100;

            TinyIoC.TinyIoCContainer container = new TinyIoC.TinyIoCContainer();
            container.Register<IConnectionStringProvider, ConnectionStringProvider>();
            container.Register<IGeocodeInfoRepository, GeocodeInfoRepository>();
            container.Register<ISafetyInfoRepository, SafetyInfoRepository>();
            container.Register<ISpeedLimitRepository, SpeedLimitRepository>();
            container.Register<IGeozoneRepository, GeozoneRepository>();

            ISafetyInfoRepository safetyRepo2 = container.Resolve<ISafetyInfoRepository>();
            IGeocodeInfoRepository geoCodeRepo = container.Resolve<IGeocodeInfoRepository>();
            ISafetyInfoRepository safetyRepo = container.Resolve<ISafetyInfoRepository>();
            ISpeedLimitRepository speedLmtRepo = container.Resolve<ISpeedLimitRepository>();
            IGeozoneRepository geozoneRepo = container.Resolve<IGeozoneRepository>();

            //geozoneRepo.GetById()

            double lat, lon;
            lat = 55.6710586547852;
            lon = 37.477409362793;

            if (!CheckCoordinates(lon, lat))
            {
                Console.WriteLine("Координаты не верны");
                Console.ReadKey();
                return;
            }

            var geoResult = geoCodeRepo.GetGeocodeInfo(lon, lat, geocoderDistance).Result;
            Console.WriteLine($"Geocode addree: city {geoResult.City} street {geoResult.Street} house {geoResult.Soorugenie}");

            var safetyResult = safetyRepo.GetSafetyInfo(lon, lat, geocoderDistance).Result;

            Console.WriteLine($"Safety objects count: {safetyResult.Count}");

            foreach(var t in safetyResult)
            {
                Console.WriteLine(t.SСode);
            }

            var speedLmtResult = speedLmtRepo.GetSpeedLimit(lon, lat, 50).Result;

            Console.WriteLine($"Speed limit: {speedLmtResult}");

            Console.ReadKey();
        }

        private static bool CheckCoordinates(double longitude, double latitude)
        {
            const double y_min = 41.18;
            const double y_max = 73;
            const double x_min = 17.6;
            const double x_max = 180;
            if (latitude < y_min || latitude > y_max || longitude < x_min || longitude > x_max) return false;
            else return true;
        }
    }
}
