using Tone.Core.Subsystems;
using Tone.Core.Subsystems.TelematicsKernel;

namespace Tone.TelematicsKernel
{
    public class TelematicsKernelSubsystemModules : AbstractSubsystemModules, ITelematicsKernelSubsystemModules
    {
        /// <summary>
        /// Модуль Геозоны
        /// </summary>
        public IGeozoneModuleCore Geozones { get; }

        /// <summary>
        /// Модуль ТУ
        /// </summary>
        public IDevicesModuleCore Devices { get; }

        /// <summary>
        /// Модуль TrackAnalyze
        /// </summary>
        public ITrackAnalyzeModuleCore TrackAnalyzes { get; }

        /// <summary>
        /// Модуль ConfigDBA
        /// </summary>
        public IDriveStyleSettingsModuleCore DriveStyleSettings { get; }
        
        public TelematicsKernelSubsystemModules(
            IGeozoneModuleCore geozones, 
            IDevicesModuleCore devices, 
            ITrackAnalyzeModuleCore trackAnalyzes, 
            IDriveStyleSettingsModuleCore driveStyleSettings)
        {
            Geozones = AddModule<IGeozoneModuleCore>(geozones);
            Devices = AddModule<IDevicesModuleCore>(devices);
            TrackAnalyzes = AddModule<ITrackAnalyzeModuleCore>(trackAnalyzes);
            DriveStyleSettings = AddModule<IDriveStyleSettingsModuleCore>(driveStyleSettings);
        }
    }
}
