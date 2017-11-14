using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Tone.Core;
using Tone.Core.Data.Account;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.WebContext;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel
{
    public class DriveStyleSettingsModuleCore : TelematicsKernelSubsystemModule, IDriveStyleSettingsModuleCore
    {
        #region  ManageDriveStyleSettings

        /// <summary>
        /// Установка нового конфига
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        [Obsolete("Настройки хранятся в другом виде и используется другой метод")]
        public async Task<PlatformResponse<IDriveStyleSettings>> ManageDriveStyleSettings(IDriveStyleSettings settings)
        {
            rc.DemandAuthorization();

            var validate = new ValidateResult("DriveStyleSettings");

            var settingsExists = await Repositories.DriveStyleSettings.GetDriveStyleSettings();

            if (settingsExists == null && settings.UnitType == null)
                validate.Message(ValidationField.Parameter, "UnitType is missing");
            if (settingsExists == null && settings.EventWeightAccelerationDefaultDuration == null)
                validate.Message(ValidationField.Parameter, "EventWeightAccelerationDefaultDuration is missing");
            if (settingsExists == null && settings.EventWeightBreakingDefaultDuration == null)
                validate.Message(ValidationField.Parameter, "EventWeightBreakingDefaultDuration is missing");
            if (settingsExists == null && settings.EventWeightSideDefaultDuration == null)
                validate.Message(ValidationField.Parameter, "EventWeightSideDefaultDuration is missing");
            if (settingsExists == null && settings.EventWeightOverspeeding == null)
                validate.Message(ValidationField.Parameter, "EventWeightOverspeeding is missing");
            if (settingsExists == null && settings.EventWeightAcceleration == null)
                validate.Message(ValidationField.Parameter, "EventWeightAcceleration is missing");
            if (settingsExists == null && settings.EventWeightBraking == null)
                validate.Message(ValidationField.Parameter, "EventWeightBraking is missing");
            if (settingsExists == null && settings.EventWeightSideAcceleration == null)
                validate.Message(ValidationField.Parameter, "EventWeightSideAcceleration is missing");
            if (settingsExists == null && settings.EventDurationAcceleration == null)
                validate.Message(ValidationField.Parameter, "EventDurationAcceleration is missing");
            if (settingsExists == null && settings.EventDurationOverspeeding == null)
                validate.Message(ValidationField.Parameter, "EventDurationOverspeeding is missing");
            if (settingsExists == null && settings.Speed == null)
                validate.Message(ValidationField.Parameter, "Speed is missing");

            if (!validate.IsValid)
                return new PlatformResponse<IDriveStyleSettings>(ResultCode.BadFields, validate.ToTextBlock());

            if (settingsExists != null)
                ChangeDriveStyleSettings(settingsExists, settings);
            else
                settingsExists = settings;

            var result = await Repositories.DriveStyleSettings.ManageDriveStyleSettings(settingsExists);
            return new PlatformResponse<IDriveStyleSettings>(result);
        }

        /// <summary>
        /// Получение текущего конфига
        /// </summary>
        /// <returns></returns>
        [Obsolete("Настройки хранятся в другом виде и используется другой метод")]
        public async Task<PlatformResponse<IDriveStyleSettings>> GetDriveStyleSettings()
        {
            rc.DemandAuthorization();

            var result = await Repositories.DriveStyleSettings.GetDriveStyleSettings();
            if (result == null)
                return new PlatformResponse<IDriveStyleSettings>(ResultCode.NotFound, "DriveStyleSettings not found");

            return new PlatformResponse<IDriveStyleSettings>(result);
        }

        #endregion

        #region ChangeDriveStyleSettings

        private void ChangeDriveStyleSettings(IDriveStyleSettings settingExists, IDriveStyleSettings settings)
        {
            settingExists.UnitType = settings.UnitType;

            if (settings.EventWeightAccelerationDefaultDuration != null)
                settingExists.EventWeightAccelerationDefaultDuration = settings.EventWeightAccelerationDefaultDuration;

            if (settings.EventWeightBreakingDefaultDuration != null)
                settingExists.EventWeightBreakingDefaultDuration = settings.EventWeightBreakingDefaultDuration;

            if (settings.EventWeightSideDefaultDuration != null)
                settingExists.EventWeightSideDefaultDuration = settings.EventWeightSideDefaultDuration;

            if (settings.EventWeightOverspeeding != null)
                settingExists.EventWeightOverspeeding = settings.EventWeightOverspeeding;

            if (settings.EventWeightAcceleration != null)
                settingExists.EventWeightAcceleration = settings.EventWeightAcceleration;

            if (settings.EventWeightBraking != null)
                settingExists.EventWeightBraking = settings.EventWeightBraking;

            if (settings.EventWeightSideAcceleration != null)
                settingExists.EventWeightSideAcceleration = settings.EventWeightSideAcceleration;

            if (settings.EventDurationAcceleration != null)
                settingExists.EventDurationAcceleration = settings.EventDurationAcceleration;

            if (settings.EventDurationOverspeeding != null)
                settingExists.EventDurationOverspeeding = settings.EventDurationOverspeeding;

            if (settings.Speed != null)
                settingExists.Speed = settings.Speed;
        }

        #endregion


        /// <summary>
        /// Создание нового конфига
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<DriveStyleViewModel>> Manage(DriveStyleViewModel request)
        {
            rc.DemandAuthorization();
            
            var scores = Mapper.Map<DriveStylePenaltyScores>(request.PenaltyScores) as IDriveStylePenaltyScores;
            scores = await Repositories.DriveStylePenaltyScores.ManagePenaltyScores(scores);

            var coefficients = Mapper.Map<DriveStyleCoefficients>(request.Coefficients) as IDriveStyleCoefficients;
            coefficients = await Repositories.DriveStyleCoefficients.ManageCoefficients(coefficients);

            var vehiclePenalties = Mapper.Map<DriveStyleVehiclePenalties>(request.VehiclePenalties) as IDriveStyleVehiclePenalties;
            vehiclePenalties = await Repositories.DriveStyleVehiclePenalties.ManageVehiclePenalties(vehiclePenalties);

            var result = new DriveStyleViewModel()
            {
                PenaltyScores = Mapper.Map<DriveStylePenaltyScoresModel>(scores),
                Coefficients = Mapper.Map<DriveStyleCoefficientsModel>(coefficients),
                VehiclePenalties = Mapper.Map<DriveStyleVehiclePenaltiesModel>(vehiclePenalties),
            };

            var history = BuildriveStyleSettingsHistory(scores, coefficients, vehiclePenalties);
            history = await Repositories.DriveStyleSettingsHistoryRepository.SaveDriveStyleSettingsHistory(history);

            return new PlatformResponse<DriveStyleViewModel>(result);
        }


        /// <summary>
        /// Получение текущего конфига
        /// </summary>
        /// <returns></returns>
        public async Task<PlatformResponse<DriveStyleViewModel>> GetDriveStyleCurrentSettings()
        {
            DemandAuthorization();

            // Штрафные очки
            var scores = await Repositories.DriveStylePenaltyScores.GetPenaltyScores();
            if (scores == null)
                scores = (await ManagePenaltyScores(DriveStylePenaltyScoresModel.Create())).Data;

            // Прочие коэффициенты
            var coefficients = await Repositories.DriveStyleCoefficients.GetCoefficients();
            if (coefficients == null)
                coefficients = (await ManageCoefficients(DriveStyleCoefficientsModel.Create())).Data;

            // Штрафы по типу ТС
            var vehiclePenalties = await Repositories.DriveStyleVehiclePenalties.GetVehiclePenalties();
            if (vehiclePenalties == null)
                vehiclePenalties = (await ManageVehiclePenalties(DriveStyleVehiclePenaltiesModel.Create())).Data;

            var result = new DriveStyleViewModel()
            {
                PenaltyScores = Mapper.Map<DriveStylePenaltyScoresModel>(scores),
                Coefficients = Mapper.Map<DriveStyleCoefficientsModel>(coefficients),
                VehiclePenalties = Mapper.Map<DriveStyleVehiclePenaltiesModel>(vehiclePenalties),
            };

            return new PlatformResponse<DriveStyleViewModel>(result);
        }

        /// <summary>
        /// Штрафные очки
        /// </summary>
        /// <param name="scoresModel"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<IDriveStylePenaltyScores>> ManagePenaltyScores(DriveStylePenaltyScoresModel scoresModel)
        {
            var scores = Mapper.Map<DriveStylePenaltyScores>(scoresModel) as IDriveStylePenaltyScores;
            var result = await Repositories.DriveStylePenaltyScores.ManagePenaltyScores(scores);

            var history = await Repositories.DriveStyleSettingsHistoryRepository.SavePenaltyScores(scores);
            return new PlatformResponse<IDriveStylePenaltyScores>(result);
        }

        /// <summary>
        /// Прочие коэффициенты
        /// </summary>
        /// <param name="coefficientsModel"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<IDriveStyleCoefficients>> ManageCoefficients(DriveStyleCoefficientsModel coefficientsModel)
        {
            var coefficients = Mapper.Map<DriveStyleCoefficients>(coefficientsModel) as IDriveStyleCoefficients;
            var result = await Repositories.DriveStyleCoefficients.ManageCoefficients(coefficients);
            var history = await Repositories.DriveStyleSettingsHistoryRepository.SaveCoefficients(coefficients);

            return new PlatformResponse<IDriveStyleCoefficients>(result);
        }


        /// <summary>
        /// Штрафы по типу ТС
        /// </summary>
        /// <param name="vehiclePenaltiesModel"></param>
        /// <returns></returns>
        public async Task<PlatformResponse<IDriveStyleVehiclePenalties>> ManageVehiclePenalties(DriveStyleVehiclePenaltiesModel vehiclePenaltiesModel)
        {
            var vehiclePenalties = Mapper.Map<DriveStyleVehiclePenalties>(vehiclePenaltiesModel) as IDriveStyleVehiclePenalties;
            var result = await Repositories.DriveStyleVehiclePenalties.ManageVehiclePenalties(vehiclePenalties);
            var history = await Repositories.DriveStyleSettingsHistoryRepository.SaveVehiclePenalties(vehiclePenalties);

            return new PlatformResponse<IDriveStyleVehiclePenalties>(result);
        }


        /// <summary>
        /// Получение истории
        /// </summary>
        /// <returns></returns>
        public async Task<PlatformResponse<IDriveStyleSettingsHistory[]>> GetDriveStyleHistory(DriveStyleHistoryRequest request)
        {
            DemandAuthorization();
            DemandValidation(request,true);
            var result = await Repositories.DriveStyleSettingsHistoryRepository.GetDriveStyleSettingsHistory(request);

            return new PlatformResponse<IDriveStyleSettingsHistory[]>(result);
        }


        private IDriveStyleSettingsHistory BuildriveStyleSettingsHistory(IDriveStylePenaltyScores scores, IDriveStyleCoefficients coefficients, IDriveStyleVehiclePenalties vehiclePenalties)
        {
            return new DriveStyleSettingsHistory()
            {
                AccountId = rc.Account?.Id,
                EmployeeId = (rc.Account?.Profile as PersonProfile)?.EmployeeId,
                DateUpdated = DateTime.UtcNow,
                HistoryType = DriveStyleSettingsHistoryType.All,
                DriveStylePenaltyScores = scores,
                DriveStyleCoefficients = coefficients,
                DriveStyleVehiclePenalties = vehiclePenalties
            };
        }
    }
}
