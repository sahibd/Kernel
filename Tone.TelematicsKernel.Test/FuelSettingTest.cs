using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Test;
using Tone.TelematicsKernel.Test.Fixtures;
using Xunit;

namespace Tone.TelematicsKernel.Test
{
    public class FuelSettingTest : PlatformTestBase
    {
        private readonly IDeviceRepository _deviceRepository;

        public FuelSettingTest()
        {
            var testContext = new TelematicsKernelTestContext();
            _deviceRepository = testContext.DevicesRepository;
        }

        protected override void TestCleanup()
        {
            _deviceRepository.Drop();
        }

        protected override void InitMapper()
        {            
        }

        protected override void SetMocks()
        {         
        }

        [Fact]
        public void TaringTable_OrderedAfterSet_Succes()
        {
            //Arrange
            var table = new TaringTableCreateModelFixture().Create();

            //Act

            //Assert
            for (var i = 1; i < table.Table.Length; i++)
                Assert.True(table.Table[i].DeviceValue > table.Table[i - 1].DeviceValue);
        }

        //[Fact]
        //public void FuelSetting_OrderedAfterSetInTable_Succes()
        //{
        //    //Arrange
        //    var table = new TaringTableCreateModelFixture().Create();
        //    var x = table.Table[5];
        //    table.Table[5] = table.Table[6];
        //    table.Table[6] = x;
        //    var fuelSetting = new FuelSetting();
        //    //var dic = new Dictionary<string, TaringTable> { ["sensor"] = table };
        //    var dic = new List<TaringTable>()
        //    {
        //        new TaringTable() {SensorName = "sensor", Table = table.Table}
        //    };
            
        //    //Act
        //    fuelSetting.TaringTables = dic;
            
        //    //Assert
        //    for (var i = 1; i < fuelSetting.TaringTables.Count; i++)
        //        Assert.True(fuelSetting.TaringTables[i].Table[i].DeviceValue > fuelSetting.TaringTables[i-1].Table[i-1].DeviceValue);
        //}        

        [Fact]
        public void FuelSetting_GetAdjustedValueIncorrectSencorName_SuccesReturn()
        {
            //Arrange
            var fuelSetting = new FuelSettingCreateModelFixture().Create();
            double d = 345d;

            //Act
            var fuel = fuelSetting.FuelSources[0].GetAdjustedValue(d);

            //Assert
            Assert.Equal(d, fuel);
        }

        [Fact]
        public void FuelSetting_GetAdjustedValueInInterval_Succes()
        {
            //Arrange
            var fuelSetting = new FuelSettingCreateModelFixture().Create();

            //Act
            var fuel = fuelSetting.FuelSources[0].GetAdjustedValue(550);

            //Assert
            Assert.Equal(55d, fuel);
        }

        [Fact]
        public void FuelSetting_GetAdjustedValueInPoint_Succes()
        {
            //Arrange
            var fuelSetting = new FuelSettingCreateModelFixture().Create();

            //Act
            var fuel = fuelSetting.FuelSources[0].GetAdjustedValue(500);

            //Assert
            Assert.Equal(50d, fuel);
        }

        [Fact]
        public void FuelSetting_GetAdjustedValueInZero_Succes()
        {
            //Arrange
            var fuelSetting = new FuelSettingCreateModelFixture().Create();

            //Act
            var fuel = fuelSetting.FuelSources[0].GetAdjustedValue(0);

            //Assert
            Assert.Equal(0, fuel);
        }

        [Fact]
        public void FuelSetting_GetAdjustedValueOutOfIntervalSubZero_Succes()
        {
            //Arrange
            var fuelSetting = new FuelSettingCreateModelFixture().Create();

            //Act
            var fuel = fuelSetting.FuelSources[0].GetAdjustedValue(-1);

            //Assert
            Assert.Equal(0d, fuel);
        }

        [Fact]
        public void FuelSetting_GetAdjustedValueOutOfIntervalSupTankVolume_Succes()
        {
            //Arrange
            var fuelSetting = new FuelSettingCreateModelFixture().Create();

            //Act
            var fuel = fuelSetting.FuelSources[0].GetAdjustedValue(9999999);

            //Assert
            Assert.Equal(fuelSetting.FuelSources[0].FuelTankVolume, fuel);
        }
    }
}
