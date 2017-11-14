using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.AnalyticSystem.Repository;

namespace Tone.TelematicsKernel.Helpers
{
    class DeviceFullRecordHelper
    {
        private readonly IFullRecordRepository _fullRecordRepository;
        //private List<DeviceParameterViewModel> _deviceParameters = new List<DeviceParameterViewModel>();

        public DeviceFullRecordHelper(IFullRecordRepository fullRecordRepository)
        {
            _fullRecordRepository = fullRecordRepository;
        }

        public async Task<bool> GetOnlineStatus(object deviceId)
        {
            if (deviceId == null)
                return false;

            var fullRecord = await GetLastFullRecordByDeviceId(deviceId);
            if (fullRecord == null)
                return false;

            var currentDateTime = DateTime.UtcNow;
            return fullRecord.DeviceTime.AddMinutes(10) >= currentDateTime;
        }

        public bool GetOnlineStatus(FullRecord<ObjectId?> fullRecord) => (fullRecord == null || fullRecord.DeviceId == null) ? false : fullRecord.DeviceTime.AddMinutes(10) >= DateTime.UtcNow;
        
        public async Task<FullRecord<ObjectId?>> GetLastFullRecordByDeviceId(object deviceId)
        {
            var fullRecord = await _fullRecordRepository.GetLastFullRecordByDeviceId(deviceId, null);
            return fullRecord;
        }

        public async Task<List<FullRecord<ObjectId?>>> GetFullRecordsByDeviceTime(object deviceId, DateTime startTime, DateTime endTime)
        {
            var fullRecords = await _fullRecordRepository.GetFullRecordsByDeviceTime(deviceId, startTime, endTime);
            return fullRecords;
        }
    }
}
