using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Data;
using Tone.Core.Test.Fixtures;

namespace Tone.TelematicsKernel.Test.Fixtures
{
    public class FuelSettingCreateModelFixture : IFixture<FuelSetting>
    {
        public FuelSetting Create()
        {
            var taringPairs = new List<TaringPair>();            
            for (var i = 0; i <= 10; i++)
                taringPairs.Add(new TaringPair { DeviceValue = 100*i, FuelAmount = 10*i });
            var taringTable = new TaringTable { Table = taringPairs.ToArray() };
			
            return new FuelSetting();
        }
    }
}
