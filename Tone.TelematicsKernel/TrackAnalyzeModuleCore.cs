using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Data.Account;
using Tone.Core.Enums;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.WebContext;
using Tone.DriveStyle.Criteria;
using Tone.SegmentStatistic.Base.BaseSegment;
using Tone.SegmentStatistic.Base.Segment;
using Tone.SegmentStatistic.DriveStyle.BaseCriteria;
using Tone.SegmentStatistic.DriveStyle.BaseCriteria.Data.Mongo;
using Tone.SegmentStatistic.DriveStyle.BaseCriteria.Interfaces;
using Tone.TelematicsKernel.Data.Providers;
using Tone.TelematicsKernel.Data.Repository.Mongo;
using BaseSegment = Tone.SegmentStatistic.Base.BaseSegment;

namespace Tone.TelematicsKernel
{
    public class TrackAnalyzeModuleCore : TelematicsKernelSubsystemModule, ITrackAnalyzeModuleCore
    {
        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<object>> GetTrackAnalyze(string deviceId, DateTime dateStart, DateTime dateEnd)
        {  
            return new PlatformResponse<object>(null);
        }
    }

    /// <summary>
    /// Временная структура необходимая для дальнейшей интеграции DBA в платформу
    /// </summary>
    internal static class DbaContainerInitializer
    {
        private static TinyIoC.TinyIoCContainer _dbaContainer;

        static DbaContainerInitializer()
        {
            _dbaContainer = new TinyIoC.TinyIoCContainer();

            // Два специализированных репозитория
            _dbaContainer.Register<ICriteriaRepository, CriteriaRepository>().AsSingleton();
            _dbaContainer.Register<ICriteriaSettingsRepository, CriteriaSettingsRepository>().AsSingleton();
            _dbaContainer.Register<IDeviceRepository, DeviceRepository>().AsSingleton();
            _dbaContainer.Register<IRecordRepository, RecordRepository>().AsSingleton();
            _dbaContainer.Register<IConnectionStringProvider, ConnectionStringProvider>().AsSingleton();
            _dbaContainer.Register<IDriveStyleSettingsRepository, DriveStyleSettingsRepository>().AsSingleton();
        }

        public static TinyIoC.TinyIoCContainer Container => _dbaContainer;
    }
}
