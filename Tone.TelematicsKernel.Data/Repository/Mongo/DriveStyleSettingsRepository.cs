using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Tone.Core.Enums;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{   
    public class DriveStyleSettingsRepository : RepositoryBaseObjectId<IDriveStyleSettings, DriveStyleSettings>, IDriveStyleSettingsRepository
    {
        private readonly ConcurrentDictionary<UnitTypeEnum, IDriveStyleSettings> _driveStyleSettings = new ConcurrentDictionary<UnitTypeEnum, IDriveStyleSettings>();

        public DriveStyleSettingsRepository(IConnectionStringProvider provider)
            : base(Config.DriveStyleSettingCollection, provider.ConnectionString)
        {
        }

        /// <summary>
        /// Создание нового DriveStyleSettings
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public async Task<IDriveStyleSettings> ManageDriveStyleSettings(IDriveStyleSettings settings)
        {
            var result = await Repsert(settings);
            return result;
        }

        /// <summary>
        /// Получение текущего DriveStyleSettings
        /// </summary>
        /// <returns></returns>
        public async Task<IDriveStyleSettings> GetDriveStyleSettings()
        {
            var setting = await Collection.Find(d => true).FirstOrDefaultAsync().ConfigureAwait(false);
            return setting;
        }

        /// <summary>
        /// Получение текущего DriveStyleSettings
        /// </summary>
        /// <returns></returns>
        //public async Task<IDriveStyleSettings> GetDriveStyleSettings(object customerId)
        //{
        //    var setting = await Collection.Find(x => x.).FirstOrDefaultAsync().ConfigureAwait(false);
        //    return setting;
        //}

        public async Task<IDriveStyleSettings> TakeSettingsAsync(UnitTypeEnum unitType, bool update = false)
        {
            if (!update && _driveStyleSettings.ContainsKey(unitType)) return _driveStyleSettings[unitType];

            var settings = await FindSettingsAsync(unitType);
            if (settings == null)
                throw new ArgumentOutOfRangeException(nameof(unitType));

            _driveStyleSettings[unitType] = settings;
            return _driveStyleSettings[unitType];
        }

        public IDriveStyleSettings TakeSettings(UnitTypeEnum unitType, bool update = false)
        {
            if (!update && _driveStyleSettings.ContainsKey(unitType)) return _driveStyleSettings[unitType];

            var settings = FindSettings(unitType);

            if (settings == null)
                throw new ArgumentOutOfRangeException(nameof(unitType));

            _driveStyleSettings[unitType] = settings;

            return _driveStyleSettings[unitType];
        }

        public IDriveStyleSettings FindSettings(UnitTypeEnum unitType)
        {
            return Collection.Find(c => c.UnitType == unitType).FirstOrDefault();
        }

        public async Task<IDriveStyleSettings> FindSettingsAsync(UnitTypeEnum unitType)
        {
            return await Collection.Find(c => c.UnitType == unitType).FirstOrDefaultAsync();
        }

        public async Task<IDriveStyleSettings> FindSettings(string unitType)
        {
            UnitTypeEnum unitTypeAsEnum;
            if (!Enum.TryParse(unitType, out unitTypeAsEnum))
                throw new ArgumentOutOfRangeException(nameof(unitType));

            return await FindSettingsAsync(unitTypeAsEnum);
        }

        public async Task<IDriveStyleSettings> GetMappedSettings(IDriveStylePenaltyScoresRepository driveStylePenaltyScoresRepository,
            IDriveStyleCoefficientsRepository driveStyleCoefficientsRepository, IDriveStyleVehiclePenaltiesRepository driveStyleVehiclePenaltiesRepository)
        {
            var driveStylePenaltyScores = await driveStylePenaltyScoresRepository.GetPenaltyScores();
            var driveStyleCoefficients = await driveStyleCoefficientsRepository.GetCoefficients();
            var driveStyleVehiclePenalties = await driveStyleVehiclePenaltiesRepository.GetVehiclePenalties();

            return DriveStyleSettings.Map(driveStylePenaltyScores, driveStyleCoefficients, driveStyleVehiclePenalties);
        }
    }
}