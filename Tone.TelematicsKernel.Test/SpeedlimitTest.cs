using System;
using System.Linq;
using Tone.TelematicsKernel.Data;
using Tone.TelematicsKernel.Data.Providers;
using Xunit;

namespace Tone.TelematicsKernel.Tests
{
    public class SpeedlimitTest
    {
       
        [Fact]
        public void CheckSpeedlimit()
        {
            var provider = SpeedlimitProvider.Instance;
            Assert.Equal(60, provider.GetEdgeSpeedlimit(51.291533, 37.826244));
            Assert.Equal(110, provider.GetEdgeSpeedlimit(52.279593, 39.053842));
            Assert.Equal(0, provider.GetEdgeSpeedlimit(52.287541, 39.116670));
            Assert.Equal(90, provider.GetEdgeSpeedlimit(54.440219, 36.331649));
            Assert.Equal(90, provider.GetEdgeSpeedlimit(55.590730, 37.283760));
            Assert.Equal(60, provider.GetEdgeSpeedlimit(55.770392, 37.635296));
            Assert.Equal(0, provider.GetEdgeSpeedlimit(55.828429, 37.604816));
        }
    }
}
