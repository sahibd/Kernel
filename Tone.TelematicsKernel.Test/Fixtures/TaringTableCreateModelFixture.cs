using System;
using System.Collections.Generic;
using Tone.Core.Test.Fixtures;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.AnalyticSystem.Model.Base;
using Tone.Core.Data;
using Tone.Core.Subsystems.AnalyticSystem.Model.Base;
using System.Linq;

namespace Tone.TelematicsKernel.Test.Fixtures
{
    public class TaringTableCreateModelFixture : IFixture<TaringTable>
    {
        private Random _rnd;

        private double _multiplier;
        
        public TaringTableCreateModelFixture()
        {
            _rnd = new Random((int)DateTime.UtcNow.Ticks);
        }
        
        public TaringTable Create()
        {
            _multiplier = _rnd.NextDouble() * 10 + 1;

            var table = new List<TaringPair> { new TaringPair { DeviceValue = 0, FuelAmount = 0 } };

            for (var i = 0; i < 50; i ++)
            {
                var r = _rnd.Next(1, 10000);
                table.Add(new TaringPair { DeviceValue = r, FuelAmount = r * _multiplier });
            }
            
            table = table.GroupBy(p => p.DeviceValue).Select(g => g.First()).ToList(); // удаляем дубли

            return new TaringTable() { Table = table.ToArray() };
        }
    }
}
