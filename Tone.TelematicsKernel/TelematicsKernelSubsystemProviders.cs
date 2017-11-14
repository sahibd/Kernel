using Tone.Core.Subsystems;
using Tone.Core.Subsystems.AnalyticSystem.Repository;
using Tone.Core.Subsystems.BusinessObjects.Providers;
using Tone.Core.Subsystems.BusinessObjects.Repositories;
using Tone.Core.Subsystems.Customers.Repositories;
using Tone.Core.Subsystems.Imitation.Repositories;
using Tone.Core.Subsystems.Security.Repositories;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;

namespace Tone.TelematicsKernel
{
    public class TelematicsKernelSubsystemProviders : ITelematicsKernelSubsystemProviders
    {
        /// <summary>
        /// Geocoding Provider
        /// </summary>
        public IGeocodingProvider GeocodingProvider { get; }

        public TelematicsKernelSubsystemProviders(IGeocodingProvider geocodingProvider)
        {
            GeocodingProvider = geocodingProvider;
        }
    }
}