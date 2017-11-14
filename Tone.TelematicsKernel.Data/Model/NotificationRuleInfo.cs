using MongoDB.Bson;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Data.Mongo.Base.Extensions;

namespace Tone.TelematicsKernel.Data.Model
{
    public class NotificationRuleInfo : NotificationRuleInfo<ObjectId?>
    {
        protected override ObjectId? ToId(object id)
        {
            return id.ToNullableObjectId();
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}
