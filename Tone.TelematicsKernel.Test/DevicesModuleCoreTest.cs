using System;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Data.Constants;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Core.Test;
using Xunit;

namespace Tone.TelematicsKernel.Test
{
    public class DevicesModuleCoreTest
    {
        private readonly IDevicesModuleCore _module;
        private readonly IRecordRepository _recordRepository;
        private readonly IGeozoneModuleCore _module1;

        public DevicesModuleCoreTest()
        {
            var testContext = new TelematicsKernelTestContext(AccountVariant.SysAdmin);
            testContext.Rc.RequestInfo.ClientAddress = "127.0.0.1";

            _module = testContext.DeviceModule;
            _module1 = testContext.GeozoneModule;
            _recordRepository = testContext.RecordRepository;
        }

        //[Fact]
        //public async Task GetRecordsByDevice_Success()
        //{
        //    var records = await _recordRepository.GetAll();
        //    string deviceId = records.ToList().FirstOrDefault().DeviceId.ToString();   //"58199319fba0f76f2c7b8fdf"
        //    var date1 = DateTime.UtcNow.AddMonths(-12);
        //    var date2 = DateTime.UtcNow;
        //    var request = new RecordsByDevicesRequest
        //    {
        //        DeviceIds = new []{ deviceId },
        //        DateStart = date1,
        //        DateEnd = date2,
        //    };

        //    var result = await _module.GetRecordsByDevices(request);
        //    Assert.Equal(result.Code, ResultCode.Ok);
        //    Assert.NotNull(result.Data);
        //}

        //[Fact]
        //public async Task GetRecordsByDevice_DeviceNotFound()
        //{   
        //    var deviceId = MongoObjectId.EmptyObjectId.ToString();
        //    var date1 = DateTime.UtcNow.AddMonths(-12);
        //    var date2 = DateTime.UtcNow;
        //    var request = new RecordsByDevicesRequest
        //    {
        //        DeviceIds = new[] { deviceId },
        //        DateStart = date1,
        //        DateEnd = date2,
        //    };

        //    var result = await _module.GetRecordsByDevices(request);
        //    Assert.Equal(result.Code, ResultCode.DeviceNotFound);
        //    Assert.NotNull(result.Data);
        //}

        //[Fact]
        //public async Task GetRecordsByDeviceIds_Success()
        //{
        //    RecordsByDevicesRequest request = new RecordsByDevicesRequest();
        //    request.DateStart = DateTime.UtcNow.AddMonths(-12);
        //    request.DateEnd = DateTime.UtcNow;     request.DeviceIds = new string[] {"5822d31cfba0f7613ca45eee", "583c44364330da1fec674c08", "58199319fba0f76f2c7b8fdf", "5819948afba0f7b260adac6e"};
       

        //    var result = await _module.GetRecordsByDevices(request);
        //    Assert.Equal(result.Code, ResultCode.Ok);
        //    Assert.NotNull(result.Data);
        //}

        //[Fact]
        //public async Task GetRecordsByDeviceIds_WithNotFound()
        //{
        //    RecordsByDevicesRequest request = new RecordsByDevicesRequest();
        //    request.DateStart = DateTime.UtcNow.AddMonths(-12);
        //    request.DateEnd = DateTime.UtcNow;
        //    request.DeviceIds = new string[] { MongoObjectId.EmptyObjectId.ToString(), "5822d31cfba0f7613ca45eee", "583c44364330da1fec674c08", "58199319fba0f76f2c7b8fdf", "5819948afba0f7b260adac6e" };

        //    var result = await _module.GetRecordsByDevices(request);
        //    Assert.Equal(result.Code, ResultCode.Ok);
        //    Assert.NotNull(result.Data);
        //}

        [Fact]
        public async Task GetRecordsByParam_Success()
        {
            var records = await _recordRepository.GetAll();
            string deviceId = records.ToList().FirstOrDefault().DeviceId.ToString();

            var result = await _module.GetRecordsByParam(deviceId, null, null);
            Assert.Equal(result.Code, ResultCode.Ok);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task GetRecordsByParam_DeviceNotFound()
        {
            var deviceId = MongoObjectId.EmptyObjectId;
            var result = await _module.GetRecordsByParam(deviceId, null, null);
            Assert.Equal(result.Code, ResultCode.DeviceNotFound);
        }

        [Fact]
        public async Task GetRecordsByParam_ParametersMissing()
        {
            var records = await _recordRepository.GetAll();
            string deviceId = records.ToList().FirstOrDefault().DeviceId.ToString();

            var result = await _module.GetRecordsByParam(deviceId, null, null);
            Assert.Equal(result.Code, ResultCode.BadFields);
            
        }
    }
}
