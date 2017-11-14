using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Data.Mongo.Model;
using Tone.Web.Base;
using Tone.Web.Base.Helpers;

namespace Tone.TelematicsKernel.Nancy
{
    public class DriveStyleSettingsModuleNancy : NancyTelematicsKernelSubsystemModule
    {
        public DriveStyleSettingsModuleNancy() : base(string.Empty, NancyAuthorization.On)
        {
            #region Post /DriveStyleSettings/Manage - Создание/Обновление конфига DBA
            
            Post["/DriveStyleSettings/Manage", true] = async (ctx, cancellation) =>
            {
                var settings = ReadJsonBody<DriveStyleViewModel>();
                var response = await Modules.DriveStyleSettings.Manage(settings);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Get /DriveStyleSettings/Current - Получение текущего конфига DBA

            Get["/DriveStyleSettings/CurrentSettings", true] = async (ctx, cancellation) =>
            {
                var response = await Modules.DriveStyleSettings.GetDriveStyleCurrentSettings();
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Недокументорованные методы

            #region Post /DriveStyleSettings/ManageScore - Создание/Обновление конфига Штрафные очки
            // [Obsolete("Method Do Not Use")]
            Post["/DriveStyleSettings/ManageScore", true] = async (ctx, cancellation) =>
            {
                var settings = ReadJsonBody<DriveStylePenaltyScoresModel>();
                var response = await Modules.DriveStyleSettings.ManagePenaltyScores(settings);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Post /DriveStyleSettings/ManageCoefficients - Создание/Обновление конфига Прочие коэффициенты
            // [Obsolete("Method Do Not Use")]
            Post["/DriveStyleSettings/ManageCoefficients", true] = async (ctx, cancellation) =>
            {
                var settings = ReadJsonBody<DriveStyleCoefficientsModel>();
                var response = await Modules.DriveStyleSettings.ManageCoefficients(settings);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Post /DriveStyleSettings/ManageVehiclePenalties - Создание/Обновление конфига Штрафы по типу ТС
            // [Obsolete("Method Do Not Use")]
            Post["/DriveStyleSettings/ManageVehiclePenalties", true] = async (ctx, cancellation) =>
            {
                var settings = ReadJsonBody<DriveStyleVehiclePenaltiesModel>();
                var response = await Modules.DriveStyleSettings.ManageVehiclePenalties(settings);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #endregion

            
            #region Get /DriveStyleSettings/History?customerId={customerId}&historyType={historyType}&dateStart={datestart}&dateFinish={dateFinish} - Получение истории конфига DBA по CustomerId
            // historyType: Все -All,  Нарушения - PenaltyScores, Коэффициенты - Coefficients, Типы ТС- VehiclePenalties
            
            Get["/DriveStyleSettings/History", true] = async (ctx, cancellation) =>
            {
                var request = BuildDriveStyleHistoryRequest();
                var response = await Modules.DriveStyleSettings.GetDriveStyleHistory(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion
        }

        private DriveStyleHistoryRequest BuildDriveStyleHistoryRequest()
        {
            var request = new DriveStyleHistoryRequest()
            {
                CustomerId = GetQueryParameter("customerId"),
                HistoryType = GetDriveStyleSettingsHistoryType(),
                DateStart = ReadDateTimeFromQuery("dateStart"),
                DateFinish = ReadDateTimeFromQuery("dateFinish")
            };
            return request;
        }

        private DriveStyleSettingsHistoryType GetDriveStyleSettingsHistoryType()
        {
            var inputType = GetQueryParameter("historyType");
            if (inputType == null)
                return DriveStyleSettingsHistoryType.All;

            DriveStyleSettingsHistoryType type;
            if (Enum.IsDefined(typeof(DriveStyleSettingsHistoryType), inputType) && Enum.TryParse(inputType, true, out type))
                return type;

            return DriveStyleSettingsHistoryType.All;
        }
    }
}
