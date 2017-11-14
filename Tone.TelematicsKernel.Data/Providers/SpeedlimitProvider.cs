using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Tone.TelematicsKernel.Data.Providers
{
    public sealed class SpeedlimitProvider
    {
        private static readonly Lazy<SpeedlimitProvider> _instance =
            new Lazy<SpeedlimitProvider>(() => new SpeedlimitProvider(), LazyThreadSafetyMode.ExecutionAndPublication);

        public const string ConnectionString =
            "Data Source=176.112.216.132;Initial Catalog=NaviGeo;User id=nu; Password=sT?8upUs;";

        public static SpeedlimitProvider Instance => _instance.Value;

        public SqlConnection SqlConnection { get; private set; }

        /// <summary>
        /// Число ошибочно обработанных точек
        /// </summary>
        public int TotalExceptionsCount { get; private set; }

        /// <summary>
        /// Ошибочные точки
        /// </summary>
        public List<ErrorPoint> ErrorPoints = new List<ErrorPoint>();

        /// <summary>
        /// Общее время работы
        /// </summary>
        public long TotalTime { get; private set; }

        public class ErrorPoint
        {
            public double Latitude;
            public double Longitude;

            public override string ToString()
            {
                return $"[lat={Latitude}, lon={Longitude}]";
            }
        }

        /// <summary>
        /// Основной метод получения данных по точке
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public int GetEdgeSpeedlimit(double latitude, double longitude)
        {
            if (latitude > -5 && latitude < 5 && longitude > -5 && longitude < 5)
                return 0;

            var totalStopWatch = new Stopwatch();
            totalStopWatch.Start();

            SqlConnection = SqlConnection ?? new SqlConnection();

            try
            {
                var com =
                    $"select top 1 dbo.f_Speedlimit_filter({latitude.ToString(CultureInfo.InvariantCulture)}, {longitude.ToString(CultureInfo.InvariantCulture)})";
                var command = new SqlCommand(com, SqlConnection);

                if (command.Connection.State != ConnectionState.Open)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    command.Connection.ConnectionString = ConnectionString;
                    command.Connection.Open();
                    stopWatch.Stop();
                }
                var speedLimitResult = command.ExecuteScalar();
                if (speedLimitResult is DBNull) return 0; // Точка не найдена в БД

                var integerResult = 0;
                try
                {
                    integerResult = (int) speedLimitResult;
                }
                catch (Exception)
                {
                    TotalExceptionsCount++;
                    ErrorPoints.Add(new ErrorPoint {Latitude = latitude, Longitude = longitude});
                    throw new Exception(
                        $"Ошибка разбора ответа от провайдера Speed limit. Точка [lat={latitude}, lon={longitude}]");
                }
                return integerResult;
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                totalStopWatch.Stop();
                TotalTime += totalStopWatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Сбросить показатели статистики
        /// </summary>
        public void StatsReset()
        {
            TotalExceptionsCount = 0;
            ErrorPoints.Clear();
            TotalTime = 0;
        }

        /// <summary>
        /// Возвратить список ошибочных точек в формате CSV
        /// </summary>
        /// <returns></returns>
        public string GetErrorPointsCsv()
        {
            var res = string.Empty;
            res += "Latitude;Longitude;";
            foreach (var errorPoint in ErrorPoints)
            {
                res += $"{errorPoint.Latitude};{errorPoint.Longitude};\r\n";
            }
            return res;
        }

    }
}
