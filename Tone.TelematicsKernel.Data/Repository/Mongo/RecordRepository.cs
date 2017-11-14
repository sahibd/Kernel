using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core.Data;
using Tone.Core.Enums;
using Tone.Core.Provider;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Record = Tone.Data.Mongo.Model.Record;
using VirtualDevice = Tone.Data.Mongo.Model.VirtualDevice;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class RecordRepository : RepositoryBaseObjectId<IRecord, Record>, IRecordRepository
    {
        public RecordRepository(IConnectionStringProvider provider)
            : base(Config.DeviceRecordCollection, provider.ConnectionString)
        {
        }
        
        public async Task<IRecord[]> GetByDevice(object deviceId, DateTime? from, DateTime? to)
        {
            return await GetByDeviceId(deviceId, from, to);
        }

        public async Task<IRecord[]> GetByDeviceId(object deviceId, DateTime? from, DateTime? to)
        {
            if (!(deviceId is ObjectId)) return null;

            var filter = Builders<Record>.Filter.Eq(r => r.DeviceId, (ObjectId?)deviceId);
            if (from != null)
                filter = Builders<Record>.Filter.And(filter, Builders<Record>.Filter.Gt(r => r.Time, from.Value));
            if (to != null)
                filter = Builders<Record>.Filter.And(filter, Builders<Record>.Filter.Lte(r => r.Time, to.Value));

            var records = await Collection.Find(filter).ToListAsync().ConfigureAwait(false);
            return records.Cast<IRecord>().ToArray();
        }

        public async Task<IRecord[]> GetByDevice(IDevice device, DateTime? from, DateTime? to)
        {
            if (device == null) return null;
            return await GetByDevice((ObjectId?)device.Id, from, to);
        }

        public Task<IRecord[]> GetByDevice(IDevice device, DateTime? from, DateTime? to, RoadEventType[] events)
        {
            throw new NotImplementedException();
        }

        public async Task<IRecord[]> GetByDevice(IVirtualDevice device, DateTime from, DateTime to)
        {
            var virtualDevice = (VirtualDevice)device;

            var deviceIds = virtualDevice.Properties.Select(p => p.DeviceId).Distinct().ToArray();
            var deviceProperties = new Dictionary<object, List<VirtualProperty>>();
            for (var i = 0; i < virtualDevice.Properties.Count; i++)
            {
                List<VirtualProperty> properties;
                var virtualProperty = virtualDevice.Properties[i];
                if (!deviceProperties.TryGetValue(virtualProperty.DeviceId, out properties))
                {
                    properties = new List<VirtualProperty>();
                    deviceProperties.Add(virtualProperty.DeviceId, properties);
                }

                properties.Add(new VirtualProperty
                {
                    DevicePropertyIndex = virtualProperty.PropertyIndex,
                    VirtualPropertyIndex = i
                });
            }

            var deviceRecords = await GetByDeviceIds(deviceIds, from, to);
            var records = deviceRecords.Cast<Record>().OrderBy(r => r.Time).ToArray();

            var result = new List<IRecord>(records.Length);
            for (var i = 0; i < records.Length; i++)
            {
                var virtualRecord = new Record
                {
                    Id = records[i].Id,
                    DeviceId = virtualDevice.Id,
                    Time = records[i].Time
                };

                var j = i;
                while (j < records.Length && records[j].Time == records[i].Time)
                {
                    var record = records[j];
                    var properties = deviceProperties[record.DeviceId];
                    var recordProperties = new List<RecordValue>(properties.Count);
                    foreach (var recordProperty in record.Values)
                    {
                        var virtualProperty = properties.FirstOrDefault(p => p.DevicePropertyIndex == recordProperty.Index);
                        if (virtualProperty == null)
                            continue;

                        recordProperty.Index = virtualProperty.VirtualPropertyIndex;
                        recordProperties.Add(recordProperty);
                    }

                    virtualRecord.Values.AddRange(recordProperties);
                    if (i != j)
                        i++;
                    j++;
                }

                result.Add(virtualRecord);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Получить дорожные события по устройству
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        public async Task<IRecord[]> GetRoadEventsByDevice(object deviceId, object resourceIdFilter, DateTime from, DateTime to, int limit, int skip)
        {
            if (deviceId == null)
                return null;

            if (!(deviceId is ObjectId))
                return null;

            var filter = Builders<Record>.Filter.SizeGte(r => r.TrackEvents, 1);
            filter = Builders<Record>.Filter.And(filter, Builders<Record>.Filter.Gt(r => r.Time, from));
            filter = Builders<Record>.Filter.And(filter, Builders<Record>.Filter.Lte(r => r.Time, to));
            filter = Builders<Record>.Filter.And(filter, Builders<Record>.Filter.Eq(r => r.DeviceId, (ObjectId?)deviceId));

            if (resourceIdFilter != null && resourceIdFilter is ObjectId)
            {
                // Условие на то, что событие будет сгенерировано со связанным ресурсов, который указан в фильтре
                filter = Builders<Record>.Filter.And(filter,
                    Builders<Record>.Filter.ElemMatch(record => record.TrackEvents,
                        trackEvent => trackEvent.RelatedResourceId == resourceIdFilter));
            }
            
            var list = await Collection.Find(filter)
                .Skip(skip)
                .Limit(limit)                
                .Project(x => new Record { Id = x.Id, Time = x.Time, TrackEvents = x.TrackEvents })
                .Sort(Builders<Record>.Sort.Descending("Time"))
                .ToListAsync();

            return list.Cast<IRecord>().ToArray();
        }

        public async Task<bool> IsDeviceContainsRecords(IDevice device)
        {
            return await Collection.CountAsync(r => r.DeviceId == device.Id).ConfigureAwait(false) > 0;
        }

        public async Task DeleteAllByDevice(IDevice device)
        {
            await Collection.DeleteManyAsync(r => r.DeviceId == device.Id).ConfigureAwait(false);
        }

        private async Task<IRecord[]> GetByDeviceIds(object[] deviceIds, DateTime from, DateTime to)
        {
            var records = await Collection.Find(r => deviceIds.Contains(r.DeviceId) && r.Time >= from && r.Time <= to)
                .ToListAsync().ConfigureAwait(false);

            return records.Cast<IRecord>().ToArray();
        }

        public async Task<IRecord[]> GetByParam(IDevice device, int? top, int? skip)
        {
            var records = await Collection.Find(r => r.DeviceId == ToId(device.Id)).Skip(skip).Limit(top)
                .ToListAsync().ConfigureAwait(false);
            return records.Cast<IRecord>().OrderBy(r => r.Time).ToArray();
        }

        public async Task<long> CountByDeviceId(object deviceId)
        {
            return await Collection.CountAsync(r => r.DeviceId == ToId(deviceId)).ConfigureAwait(false);
        }

        public async Task InsertMany(IRecord[] items)
        {
            var records = items as Record[];
            await Collection.InsertManyAsync(records).ConfigureAwait(false);
        }

        // todo: Удалить, метод используется для заглушки
        public async Task<IRecord[]> GetWithEvents()
        {
            var filter = Builders<Record>.Filter.Gte(r => r.TrackEvents.Count, 1);
            var result = await Collection.Find(filter).ToListAsync();
            return result.Cast<IRecord>().ToArray();
        }
    }
}