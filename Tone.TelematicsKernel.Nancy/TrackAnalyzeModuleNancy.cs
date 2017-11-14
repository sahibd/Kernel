using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.WebContext;
using Tone.Web.Base;
using Tone.Web.Base.Helpers;

namespace Tone.TelematicsKernel.Nancy
{
    public class TrackAnalyzeModuleNancy : NancyTelematicsKernelSubsystemModule
    {
        public const string ErrIncorrectDate = "Incorrect date";

        public TrackAnalyzeModuleNancy() : base(string.Empty, NancyAuthorization.Off)
        {
            #region GET /TrackAnalyze?device={id}&start={dateTimeStart}&end={dateTimeEnd} - Метод выдачи данных DBA в Json-виде
            // [Obsolete("Method Do Not Use")]
            Get["/TrackAnalyze", true] = async (ctx, cancellation) =>
            {

                string id = Request.Query.Device.ToString();
                var date1 = ReadDateTimeFromQuery("start");
                var date2 = ReadDateTimeFromQuery("end");

                if (!date1.HasValue && !date2.HasValue)
                    return ResponseCode(ResultCode.BadFields, ErrIncorrectDate);

                var result = await Modules.TrackAnalyzes.GetTrackAnalyze(id, date1.Value, date2.Value);
                
                return Response.AsPlatformResponse(result);
            };

            #endregion
        }
    }
}
