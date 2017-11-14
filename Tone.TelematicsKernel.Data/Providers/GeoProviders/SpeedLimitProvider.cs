using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;

namespace Tone.TelematicsKernel.Data.Providers.GeoProviders
{
    public class SpeedLimitProvider
    {
        private readonly ISpeedLimitRepository _speedLimitRepository;

        public SpeedLimitProvider(ISpeedLimitRepository speedLimitRepository)
        {
            _speedLimitRepository = speedLimitRepository;
        }

        public async Task<int> GetSpeedLimit(double lon, double lat, int distance = 50)
        {
            var result = await _speedLimitRepository.GetSpeedLimit(lon, lat, distance).ConfigureAwait(false);
            return result;
        }
    }
}
