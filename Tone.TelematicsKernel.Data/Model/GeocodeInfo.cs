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
    public class GeocodeInfo : GeocodeInfo<ObjectId?>
    {
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        protected override ObjectId? ToId(object id)
        {
            return id.ToNullableObjectId();
        }
    }
}
