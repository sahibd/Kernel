using Tone.Core.Subsystems;
using Tone.Core.Subsystems.AnalyticSystem.Repository;
using Tone.Core.Subsystems.BusinessObjects.Repositories;
using Tone.Core.Subsystems.Customers.Repositories;
using Tone.Core.Subsystems.Imitation.Repositories;
using Tone.Core.Subsystems.Reports.Repositories;
using Tone.Core.Subsystems.Security.Repositories;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.SegmentStatistic.DriveStyle.BaseCriteria.Data.Mongo;
using Tone.SegmentStatistic.DriveStyle.BaseCriteria.Interfaces;

namespace Tone.TelematicsKernel
{
    public class TelematicsKernelSubsystemRepositories : AbstractSubsystemRepositories, ITelematicsKernelSubsystemRepositories
    {
        /// <summary>
        /// Geozone Repository
        /// </summary>
        public IGeozoneRepository Geozones { get; }

        /// <summary>
        /// Device Repository
        /// </summary>
        public IDeviceRepository Devices { get; }

        /// <summary>
        /// Device History Repository
        /// </summary>
        public IDeviceHistoryRepository DeviceHistory { get; }

        /// <summary>
        /// DeviceGroup Repository
        /// </summary>
        public IDeviceGroupRepository DeviceGroups { get; }

        /// <summary>
        /// Record Repository
        /// </summary>
        public IRecordRepository Records { get; }

        /// <summary>
        /// FullRecord Repository
        /// </summary>
        public IFullRecordRepository FullRecords { get; }

        /// <summary>
        /// Дорожные события
        /// </summary>
        public ITrackEventRepository TrackEvent { get; }

        /// <summary>
        /// DeviceStates Repository
        /// </summary>
        public IStateRepository DeviceStates { get; }

        /// <summary>
        /// Execution Repository
        /// </summary>
        public IExecutionRepository Executions { get; }

        /// <summary>
        /// DriveStyleSettings Repository
        /// </summary>
        public IDriveStyleSettingsRepository DriveStyleSettings { get; }

        /// <summary>
        /// TrackPositions Repository
        /// </summary>
        public IImitationTracksRepository Tracks { get; }

        /// <summary>
        ///
        /// </summary>
        public IEmployeesRepository Employees { get; }

        /// <summary>
        ///
        /// </summary>
        public IVehicleRepository Vehicles { get; }
        
        /// <summary>
        /// Штрафные очки
        /// </summary>
        public IDriveStylePenaltyScoresRepository DriveStylePenaltyScores { get; }

        /// <summary>
        /// Прочие коэффициенты
        /// </summary>
        public IDriveStyleCoefficientsRepository DriveStyleCoefficients { get; }

        /// <summary>
        /// Штрафы по типу ТС
        /// </summary>
        public IDriveStyleVehiclePenaltiesRepository DriveStyleVehiclePenalties { get; }

        /// <summary>
        /// Customer
        /// </summary>
        public ICustomersRepository Customers { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public ITelematicCommandRepository Commands { get; }
        
        /// <summary>
        /// AnalyticDevices Repository
        /// </summary>
        public IAnalyticDeviceRepository AnalyticDevices { get; }

        /// <summary>
        /// Аналитические интервалы
        /// </summary>
        public IAnalyticIntervalResultsRepository AnalyticIntervals { get; }

        /// <summary>
        /// Репозитории DBA
        /// </summary>
        public ICriteriaSettingsRepository CriteriaSettingsRepository { get; }
        public IDriveStyleSettingsRepository DriveStyleSettingsRepository { get; }
        public IDriveStylePenaltyScoresRepository DriveStylePenaltyScoresRepository { get; }
        public IDriveStyleCoefficientsRepository DriveStyleCoefficientsRepository { get; }
        public IDriveStyleVehiclePenaltiesRepository DriveStyleVehiclePenaltiesRepository { get; }
        public IDriveStyleSettingsHistoryRepository DriveStyleSettingsHistoryRepository { get; }

		public IGeozoneInfoRepository GeozoneInfoRepository { get; }

        public IOmGroupsRepository OmGroups { get; }

        public TelematicsKernelSubsystemRepositories(
            IDeviceRepository device,
            IDeviceGroupRepository deviceGroup,
            IGeozoneRepository geozoneRepository,
            IDriveStyleSettingsRepository driveStyleSettings, 
            IEmployeesRepository employees, 
            IVehicleRepository vehicles, 
            IRecordRepository recordRepository,
            IFullRecordRepository fullRecordRepository,
            ITrackEventRepository trackEventRepository,
            IStateRepository deviceState,
            IExecutionRepository executionRepository,
            IImitationTracksRepository tracks,
            IDeviceHistoryRepository deviceHistory,
            IDriveStylePenaltyScoresRepository driveStylePenaltyScores,
            IDriveStyleCoefficientsRepository driveStyleCoefficients,
            IDriveStyleVehiclePenaltiesRepository driveStyleVehiclePenalties,
            ITelematicCommandRepository commands,
            ICustomersRepository customers,
            IAnalyticDeviceRepository analyticDevices,
            ICriteriaSettingsRepository criteriaSettingsRepository,
            IDriveStyleSettingsRepository driveStyleSettingsRepository,
            IDriveStylePenaltyScoresRepository driveStylePenaltyScoresRepository,
            IDriveStyleCoefficientsRepository driveStyleCoefficientsRepository,
            IDriveStyleVehiclePenaltiesRepository driveStyleVehiclePenaltiesRepository,
            IDriveStyleSettingsHistoryRepository driveStyleSettingsHistoryRepository,
            IAnalyticIntervalResultsRepository analyticIntervals,
            IGeozoneInfoRepository geozoneInfoRepository,
            IOmGroupsRepository omGroupsRepository

            )
        {
            Geozones = AddRepository<IGeozoneRepository>(geozoneRepository);
            Devices = AddRepository<IDeviceRepository>(device);
            DeviceGroups = AddRepository<IDeviceGroupRepository>(deviceGroup);
            DeviceStates = AddRepository<IStateRepository>(deviceState);
            DeviceHistory = AddRepository<IDeviceHistoryRepository>(deviceHistory);
            Executions = AddRepository<IExecutionRepository>(executionRepository);
            Records = AddRepository<IRecordRepository>(recordRepository);
            FullRecords = AddRepository<IFullRecordRepository>(fullRecordRepository);
            TrackEvent = AddRepository<ITrackEventRepository>(trackEventRepository);
            Tracks = AddRepository<IImitationTracksRepository>(tracks);
            DriveStyleSettings = AddRepository<IDriveStyleSettingsRepository>(driveStyleSettings);
            Employees = AddRepository<IEmployeesRepository>(employees);
            Vehicles = AddRepository<IVehicleRepository>(vehicles);
            Employees = AddRepository<IEmployeesRepository>(employees);
            Customers = AddRepository<ICustomersRepository>(customers);
            DriveStylePenaltyScores = AddRepository<IDriveStylePenaltyScoresRepository>(driveStylePenaltyScores);
            DriveStyleCoefficients = AddRepository<IDriveStyleCoefficientsRepository>(driveStyleCoefficients);
            DriveStyleVehiclePenalties = AddRepository<IDriveStyleVehiclePenaltiesRepository>(driveStyleVehiclePenalties);
            Commands = AddRepository<ITelematicCommandRepository>(commands);
            AnalyticDevices = AddRepository<IAnalyticDeviceRepository>(analyticDevices);
            AnalyticIntervals = AddRepository<IAnalyticIntervalResultsRepository>(analyticIntervals);

            // Репозитории DBA
            CriteriaSettingsRepository = AddRepository<ICriteriaSettingsRepository>(criteriaSettingsRepository);
            DriveStyleSettingsRepository = AddRepository<IDriveStyleSettingsRepository>(driveStyleSettingsRepository);
            DriveStylePenaltyScoresRepository = AddRepository<IDriveStylePenaltyScoresRepository>(driveStylePenaltyScoresRepository);
            DriveStyleCoefficientsRepository = AddRepository<IDriveStyleCoefficientsRepository>(driveStyleCoefficientsRepository);
            DriveStyleVehiclePenaltiesRepository = AddRepository<IDriveStyleVehiclePenaltiesRepository>(driveStyleVehiclePenaltiesRepository);
            DriveStyleSettingsHistoryRepository = AddRepository<IDriveStyleSettingsHistoryRepository>(driveStyleSettingsHistoryRepository);

			GeozoneInfoRepository = AddRepository<IGeozoneInfoRepository>(geozoneInfoRepository);
            OmGroups = AddRepository<IOmGroupsRepository>(omGroupsRepository);

        }

    }
}