using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;

namespace Tone.TelematicsKernel.Data.Providers.GeoProviders
{
    public class SafetyObjectsProvider
    {
        private readonly ISafetyInfoRepository _safetyInfoRepository;
        public SafetyObjectsProvider(ISafetyInfoRepository safetyInfoRepository)
        {
            _safetyInfoRepository = safetyInfoRepository;
        }

        public async Task<List<ISafetyInfo>> GetSafetyObjects(double lon, double lat, int geocoderDistance = 50)
        {
            var result = await _safetyInfoRepository.GetSafetyInfo(lon, lat, geocoderDistance).ConfigureAwait(false);
            return result;
        }        
    }
}
