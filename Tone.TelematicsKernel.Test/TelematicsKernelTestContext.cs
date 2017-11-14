using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Test;

namespace Tone.TelematicsKernel.Test
{
    public class TelematicsKernelTestContext : TestContext
    {
        /// <summary>
        /// Device Module
        /// </summary>
        public IDevicesModuleCore DeviceModule => Rc.PlatformComponents.TelematicsKernelSubsystem.Modules.Devices;

        /// <summary>
        /// Device Repository
        /// </summary>
        public IDeviceRepository DevicesRepository => Rc.PlatformComponents.TelematicsKernelSubsystem.Repositories.Devices;

        /// <summary>
        /// Record Repository
        /// </summary>
        public IRecordRepository RecordRepository => Rc.PlatformComponents.TelematicsKernelSubsystem.Repositories.Records;

        /// <summary>
        /// GeozoneModuleCore
        /// </summary>
        public IGeozoneModuleCore GeozoneModule => Rc.PlatformComponents.TelematicsKernelSubsystem.Modules.Geozones;

        /// <summary>
        /// Geozone Repository
        /// </summary>
        public IGeozoneRepository GeozoneRepository => Rc.PlatformComponents.TelematicsKernelSubsystem.Repositories.Geozones;
        
        /// <summary>
        /// Создание экземпляра на основе имеющихся параметров
        /// </summary>
        public TelematicsKernelTestContext(AccountVariant accountVariant = AccountVariant.Default,
            AccountType accountType = AccountType.Person)
            : base(accountVariant, accountType, DataBaseType.ToneTests)
        {
        }

        public TelematicsKernelTestContext(Permission permission,
            AccountType accountType = AccountType.Person)
            : base(permission, accountType, DataBaseType.ToneTests)
        {
        }
    }
}
