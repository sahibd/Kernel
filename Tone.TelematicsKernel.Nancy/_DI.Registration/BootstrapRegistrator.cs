using System.ComponentModel;
using Tone.AnalyticProcessorCore.Data.Repository;
using Tone.Core;
using Tone.Core.Subsystems.AnalyticSystem.Repository;
using Tone.Core.Subsystems.BusinessObjects.Providers;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;
using Tone.SegmentStatistic.DriveStyle.BaseCriteria.Data.Mongo;
using Tone.TelematicsKernel.Data.Providers.GeoProviders;
using Tone.TelematicsKernel.Data.Repository.Mongo;
using Tone.TelematicsKernel.Data.Repository.Mongo.GeoProviders;
using Tone.TelematicsKernel.MappingProfiles;
using Tone.Web.Base;
using Tone.Web.Bootstrap;

namespace Tone.TelematicsKernel.Nancy
{
    public class BootstrapRegistrator : Tone.Web.Bootstrap.RegistratorBase, ISubsystemBootstrapRegistrator
    {
        /// <summary>
        /// В данном методе должны быть зарегистрированы ключевые части подсистемы, такие как:
        /// - ISecuritySubsystem
        /// - ISecuritySubsystemModules
        /// - ISecuritySubsystemRepositories
        /// - ISecuritySubsystemProviders
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterSubsystem()
        {
            Container.Register<ITelematicsKernelSubsystem, TelematicsKernelSubsystem>().AsMultiInstance();
            Container.Register<ITelematicsKernelSubsystemModules, TelematicsKernelSubsystemModules>().AsMultiInstance();
            Container.Register<ITelematicsKernelSubsystemRepositories, TelematicsKernelSubsystemRepositories>().AsMultiInstance();
            Container.Register<ITelematicsKernelSubsystemProviders, TelematicsKernelSubsystemProviders>().AsMultiInstance();
        }

        /// <summary>
        /// В данном методе должны быть зарегистрированы все используемые репозитории
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterRepositories()
        {
            // Репозитории (Подсистема Виртуальная телематика)
            if (Settings.RepositoriesRealization == RepositoriesRealization.Mongo)
            {
                Container.Register<IGeozoneRepository, GeozoneRepository>().AsMultiInstance();
                Container.Register<IGeozoneInfoRepository, GeozoneInfoRepository>().AsSingleton();
                Container.Register<IStateRepository, StateRepository>().AsSingleton();
                Container.Register<IRecordRepository, RecordRepository>().AsSingleton();
                Container.Register<IFullRecordRepository, FullRecordRepository>().AsSingleton();
                Container.Register<ITrackEventRepository, TrackEventRepository>().AsSingleton();
                Container.Register<IDeviceRepository, DeviceRepository>().AsMultiInstance();
                Container.Register<IDeviceHistoryRepository, DeviceHistoryRepository>().AsSingleton();
                Container.Register<IExecutionRepository, ExecutionRepository>().AsSingleton();
                Container.Register<IDeviceGroupRepository, DeviceGroupRepository>().AsSingleton();
                Container.Register<ITelematicCommandRepository, TelematicCommandRepository>().AsSingleton();
                Container.Register<IAnalyticDeviceRepository, AnalyticDeviceRepository>().AsSingleton();
                Container.Register<ITrackEventRepository, TrackEventRepository>().AsSingleton();

                // GeoCoding
                Container.Register<IGeocodeInfoRepository, GeocodeInfoRepository>().AsSingleton();
                Container.Register<ISafetyInfoRepository, SafetyInfoRepository>().AsSingleton();
                Container.Register<ISpeedLimitRepository, SpeedLimitRepository>().AsSingleton();

                // Репозитории DBA
                Container.Register<ICriteriaSettingsRepository, CriteriaSettingsRepository>().AsSingleton();
                Container.Register<IDriveStyleSettingsRepository, DriveStyleSettingsRepository>().AsSingleton();
                Container.Register<IDriveStylePenaltyScoresRepository, DriveStylePenaltyScoresRepository>().AsMultiInstance();
                Container.Register<IDriveStyleCoefficientsRepository, DriveStyleCoefficientsRepository>().AsMultiInstance();
                Container.Register<IDriveStyleVehiclePenaltiesRepository, DriveStyleVehiclePenaltiesRepository>().AsMultiInstance();
                Container.Register<IDriveStyleSettingsHistoryRepository, DriveStyleSettingsHistoryRepository>().AsMultiInstance();
            }
        }

        /// <summary>
        /// В данном методе должны быть зарегистрированы все модули подсистемы
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterModules()
        {
            Container.Register<IGeozoneModuleCore, GeozoneModuleCore>().AsMultiInstance();
            Container.Register<IDevicesModuleCore, DevicesModuleCore>().AsMultiInstance();
            Container.Register<ITrackAnalyzeModuleCore, TrackAnalyzeModuleCore>().AsMultiInstance();
            Container.Register<IDriveStyleSettingsModuleCore, DriveStyleSettingsModuleCore>().AsMultiInstance();
        }

        /// <summary>
        /// В данном методе должны быть зарегистрированы все провайдеры
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterProviders()
        {
            Container.Register<IGeocodingProvider, GeocodingProvider>().AsSingleton();
        }

        /// <summary>
        /// В данном методе должны быть зарегистрированы все службы
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterServices()
        {
            // Службы отсутствуют
        }

        /// <summary>
        /// В данном методе должны быть зарегистрированы все профили маппера
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterMappings()
        {
            Container.Register<PlatformAutomapperProfile, TelematicDeviceMappingProfile>("TelematicDevice").AsSingleton();
            Container.Register<PlatformAutomapperProfile, GeozoneMappingProfile>("Geozone").AsSingleton();
            Container.Register<PlatformAutomapperProfile, DriveStyleMappingProfile>("DriveStyleMapping").AsSingleton();
        }

        /// <summary>
        /// В данном методе должны быть зарегистрированы все прочие элементы
        ///
        /// Метод выполняется после выполнения BuildUp, в котором задается контейнер и настройки
        /// </summary>
        public void RegisterMisc()
        {
            Container.Register<IGeozone, Tone.Data.Mongo.Model.Geozone>();
        }
    }
}