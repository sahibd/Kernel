using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Tone.Core.Data;
using Tone.Core.Data.Mongo.NewTrackEvents;
using Tone.Core.Data.NewTrackEvent;
using Tone.Core.Provider;
using Tone.Core.Subsystems.AnalyticSystem.Model;
using Tone.Core.Subsystems.TelematicsKernel.Repositories;
using Tone.Data.Mongo.Base;
using Tone.Data.Mongo.Base.Extensions;

namespace Tone.TelematicsKernel.Data.Repository.Mongo
{
    public class TrackEventRepository : RepositoryBaseObjectId<ITrackEvent, TrackEvent>, ITrackEventRepository
    {
        public TrackEventRepository(IConnectionStringProvider provider) : base(Config.TrackEventCollection, provider.ConnectionString)
        {
        }

        public async Task<List<ITrackEvent>> GetByInterval(DateTime from, DateTime to, object deviceId)
        {
            var deviceId_objectId = deviceId.ToNullableObjectId();
            if (deviceId_objectId == null) return null;

            var query = Collection.Find(
                x => x.DeviceId.Equals(deviceId_objectId)
                && from <= x.EventDeviceTime
                && x.EventDeviceTime < to).SortBy(x => x.EventDeviceTime);
            var events = await query.ToListAsync().ConfigureAwait(false);
            return events.Cast<ITrackEvent>().ToList();
        }

        public async Task<List<ITrackEvent>> GetByIntervalExt(DateTime from, DateTime to, object deviceId, object resourceId,
            int? skip, int? limit)
        {
            var deviceId_objectId = deviceId.ToNullableObjectId();
            if (deviceId_objectId == null) return null;

            // VehicleId
            var deviceFilter = Builders<TrackEvent>.Filter.Eq(r => r.DeviceId, deviceId_objectId);
            // DateStart
            var startFilter = Builders<TrackEvent>.Filter.Gte(r => r.EventDeviceTime, from);
            // DateEnd
            var endFilter = Builders<TrackEvent>.Filter.Lt(r => r.EventDeviceTime, to);
            // RoadEventId
            var resourceObjectId = resourceId.ToNullableObjectId();
            var resourceIdFilter = (resourceObjectId != null) ? Builders<TrackEvent>.Filter.Eq(r => r.RelatedResourceId, resourceObjectId) 
                : Builders<TrackEvent>.Filter.Where(r => true);

            var filter = Builders<TrackEvent>.Filter.And(deviceFilter, startFilter, endFilter, resourceIdFilter);
            var result = await Collection.Find(filter).Skip(skip).Limit(limit).ToListAsync().ConfigureAwait(false);
            return result.Cast<ITrackEvent>().ToList();
        }
    }
}
