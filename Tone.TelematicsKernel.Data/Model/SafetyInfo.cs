using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Data.Mongo.Base.Extensions;

namespace Tone.TelematicsKernel.Data.Model
{
    public class SafetyInfo : SafetyInfo<ObjectId?>
    {
        protected override ObjectId? ToId(object id)
        {
            return id.ToNullableObjectId();
        }
    }
}
