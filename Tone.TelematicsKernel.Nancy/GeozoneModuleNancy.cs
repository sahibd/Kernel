using System;
using Tone.Core;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Web.Base;
using Tone.Web.Base.Helpers;

namespace Tone.TelematicsKernel.Nancy
{
    public class GeozoneModuleNancy : NancyTelematicsKernelSubsystemModule
    {
        public GeozoneModuleNancy() : base(string.Empty, NancyAuthorization.On)
        {
            #region Get /Geozone/GetAll?accessType={accessType}&name={name}

            Get["/Geozone/GetAll", true] = async (ctx, cancellation) =>
            {
                var type = GetQueryParameter("accessType");
                var name = GetQueryParameter("name");
                var response = await Modules.Geozones.GetAll(type, name);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Get /Geozone/{id}
            // [Obsolete("Method Do Not Use")]
            Get["/Geozone/{id}", true] = async (ctx, cancellation) =>
            {
                string id = ctx.id.ToString();
                if (string.IsNullOrEmpty(id))
                    return ResponseCode(ResultCode.BadFields, "Geozone Id is missing");

                var response = await Modules.Geozones.GetGeozone(id);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Post /Geozone/Add

            Post["/Geozone/Add", true] = async (ctx, cancellation) =>
            {
                var geozone = ReadJsonBody<GeozoneCreateModel>();
                var response = await Modules.Geozones.Create(geozone);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Post /Geozone/Update

            Post["/Geozone/Update", true] = async (ctx, cancellation) =>
            {
                var geozone = ReadJsonBody<GeozoneUpdateModel>();
                var response = await Modules.Geozones.Update(geozone);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Post /Geozone/Delete/{id}

            Post["/Geozone/Delete/{id}", true] = async (ctx, cancellation) =>
            {
                string id = ctx.id.ToString();
                if (string.IsNullOrEmpty(id))
                    return ResponseCode(ResultCode.BadFields, "Geozone Id is missing");

                var response = await Modules.Geozones.Delete(id);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region Get /Geozone/IntersectPoint/id={id}&lat={lat}&lon={lon}

            // [Obsolete("Method Do Not Use")]
            Get["/Geozone/IntersectPoint/", true] = async (ctx, cancellation) =>
            {
                var id = GetQueryParameter("id");
                if (string.IsNullOrEmpty(id))
                    return ResponseCode(ResultCode.BadFields, "Geozone Id is missing");

                var lat = GetQueryParameter("lat");
                var lon = GetQueryParameter("lon");
                var point = new Point() {Latitude = double.Parse(lat.Replace('.', ',')), Longitude = double.Parse(lon.Replace('.', ','))};
                var response = await Modules.Geozones.IntersectPoint(id, point);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Geozone/Customer?customerId={customerId}

            Get["/Geozone/Customer", true] = async (ctx, cancellation) =>
            {
                var id = GetQueryParameter("customerId");
                var response = await Modules.Geozones.GetGeozonesByCustomer(id);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Geozone/FindByPoint?longitude={longitude}&latitude={latitude}

            Get["/Geozone/FindByPoint", true] = async (ctx, cancellation) =>
            {
                var longitude = GetQueryParameter<double>("longitude");
                var latitude = GetQueryParameter<double>("latitude");
                var response = await Modules.Geozones.FindGeozonesByPoint(longitude, latitude);
                return Response.AsPlatformResponse(response);
            };

            #endregion
        }
    }
}
