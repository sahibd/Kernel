using MongoDB.Bson;
using System;
using Tone.Core;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Web.Base.Helpers;
using Tone.Web.Base.Model.Request;

namespace Tone.TelematicsKernel.Nancy
{
    public class DevicesModuleNancy : NancyTelematicsKernelSubsystemModule
    {
        public DevicesModuleNancy() : base(string.Empty)
        {
            #region GET /Telematics/Devices?skip={skip}&limit={limit}&relationStatus={status}&namefilter={namefilter} - Получить список всех устройств в системе по фильтру

            Get["/Telematics/Devices", true] = async (ctx, cancellation) =>
            {
                var request = GenerateDeviceTelematicsRequest();
                var paginationRequest = GetPlatformPaginationRequest();
                var response = await Modules.Devices.GetDevices(request, paginationRequest);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Telematics/Devices/Details - Получение списка детальной информации всех устройств в системе.

            // [Obsolete("Method Do Not Use")]
            Get["/Telematics/Devices/Details", true] = async (ctx, cancellation) =>
            {
                var response = await Modules.Devices.GetDevicesDetails();
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Telematics/Device/Details/{id} - Получение информации о ТМУ для формы редактирования

            Get["/Telematics/Devices/Details/{id}", true] = async (ctx, cancellation) =>
            {
                string deviceId = ctx.Id.ToString();
                if (string.IsNullOrEmpty(deviceId))
                    return ResponseCode(ResultCode.BadFields, "Device Id is missing");

                var response = await Modules.Devices.GetDevice(deviceId);
                return Response.AsPlatformResponse(response);
            };

            #endregion
            
            #region POST /Telematics/Devices/{id}/Commands/{code} - Выполнить команду

            // [Obsolete("Method Do Not Use")]
            Post["/Telematics/Devices/{id}/Commands/{code}", true] = async (ctx, cancellation) =>
            {
                string deviceId = ctx.id.ToString();
                if (string.IsNullOrEmpty(deviceId))
                    return ResponseCode(ResultCode.BadFields, "Device Id is missing");

                string code = ctx.code.ToString();
                var execution = ReadJsonBody<CommandExecution>();
                var response = await Modules.Devices.ExecuteCommand(deviceId, code, execution.Arguments);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Telematics/Devices/{id}/State - Получить состояние устройства

            // [Obsolete("Method Do Not Use")]
            Get["/Telematics/Devices/{id}/State", true] = async (ctx, cancellation) =>
            {
                string deviceId = ctx.id.ToString();
                if (string.IsNullOrEmpty(deviceId))
                    return ResponseCode(ResultCode.BadFields, "Device Id is missing");

                var response = await Modules.Devices.GetState(deviceId);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Telematics/Records?deviceId={id}&top={top}&skip={skip} - Получить состояние устройства

            // [Obsolete("Method Do Not Use")]
            Get["Telematics/Records", true] = async (ctx, cancellation) =>
            {
                string deviceId = GetQueryParameter("deviceId");
                var skip = GetQueryParameter<int?>("skip");
                var top = GetQueryParameter<int?>("top");

                var response = await Modules.Devices.GetRecordsByParam(deviceId, top, skip);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/Devices/LastPositions - Получить последние значения позиций

            // [Obsolete("Method Do Not Use")]
            Post["/Telematics/Devices/LastPositions", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<GetLastPositionsRequest>();
                var response = await Modules.Devices.LastPositions(request.Ids);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Telematics/Device/{id}/LastPosition - Получить последние значения позиций

            // [Obsolete("Method Do Not Use")]
            Get["/Telematics/Device/{id}/LastPosition", true] = async (ctx, cancellation) =>
            {
                string deviceId = ctx.id.ToString();
                if (string.IsNullOrEmpty(deviceId))
                    return ResponseCode(ResultCode.BadFields, "Device Id is missing");

                var response = await Modules.Devices.LastPositions(new string[] {deviceId});
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/Devices/GetFirstTrackParts - Получить последние значения позиций --- Full records

            Post["/Telematics/Devices/GetFirstTrackParts", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<GetFirstTrackPartsRequest>();
                var response = await Modules.Devices.GetFirstTrackPartsFullRecords(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/Devices/GetNextTrackParts - Получить последние значения позиций --- Full records

            Post["/Telematics/Devices/GetNextTrackParts", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<GetNextTrackPartsRequest>();
                var response = await Modules.Devices.GetNextTrackPartsFullRecords(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/Tracks - Возврат записей по списку устройств --- Full records

            Post["/Telematics/Tracks", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<RecordsByDevicesRequest>();
                var response = await Modules.Devices.GetFullRecordsByDevices(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/RoadEvent - Возврат полной информации по дорожному событию

            Get["/Telematics/RoadEvent", true] = async (ctx, cancellation) =>
            {
                string roadEventId = GetQueryParameter("id");
                if (string.IsNullOrEmpty(roadEventId))
                    return ResponseCode(ResultCode.BadFields, "Road event Id is missing");

                var response = await Modules.Devices.GetSingleRoadEvent(roadEventId);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Telematics/History?id={id}&skip={skip}&limit={limit} - Получение истории объектов, привязанных к ТМУ ранее

            Get["/Telematics/Devices/History", true] = async (ctx, cancellation) =>
            {
                var request = GenerateDeviceHistoryRequest();
                var response = await Modules.Devices.GetDeviceHistory(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/Device/Update -  Обновление настроек ТМУ

            Post["/Telematics/Device/Update", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<DeviceRequestModel>();
                var response = await Modules.Devices.DeviceUpdate(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Telematics/Device/SendCommand - Добавить команду

            Post["/Telematics/Device/SendCommand", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<SendCommandRequest>();
                var response = await Modules.Devices.AddCommand(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Admin/Telematics/Devices?skip={skip}&limit={limit}&relationStatus={status}&customerId={customerId}&namefilter={namefilter} - Получить список всех устройств в системе по фильтру дя Customers

            Get["/Admin/Telematics/Devices", true] = async (ctx, cancellation) =>
            {
                var request = GenerateDeviceCustomerRequest();
                var paginationRequest = GetPlatformPaginationRequest();
                var response = await Modules.Devices.AdminGetDevices(request, paginationRequest);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Admin/Telematics/Device/ManageCustomer?customerId={customerId}&deviceId={deviceId} -  Привязка/отвязка ТМУ к кастомеру

            Post["/Admin/Telematics/Device/ManageCustomer", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<DeviceCustomerBindingRequest>();
                var response = await Modules.Devices.UpdateDeviceCustomerBinding(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Admin/Telematics/Device/SetCustomerRelation -  Привязка/отвязка массива ТМУ к кастомеру

            Post["/Admin/Telematics/Device/SetCustomerRelation", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<MultipleDeviceCustomerBindingRequest>();
                var response = await Modules.Devices.UpdateMultipleDeviceCustomerBinding(request);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Admin/Telematics/Device/Update - Изменение ТМУ - Admin

            Post["/Admin/Telematics/Device/Update", true] = async (ctx, cancellation) =>
            {
                var request = ReadJsonBody<DeviceRequestModel>();
                var response = await Modules.Devices.DeviceUpdate(request, true);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region POST /Admin/Telematics/Device/{id} - Удаление ТМУ

            Post["/Admin/Telematics/Device/{id}", true] = async (ctx, cancellation) =>
            {
                string id = ctx.id.ToString();
                if (string.IsNullOrEmpty(id))
                    return ResponseCode(ResultCode.BadFields, "Device Id is missing");

                var response = await Modules.Devices.AdminDelete(id);
                return Response.AsPlatformResponse(response);
            };

            #endregion

            #region GET /Admin/Telematics/Device/Details/{id} - Получение информации о ТМУ для формы редактирования в Админке

            Get["/Admin/Telematics/Devices/Details/{id}", true] = async (ctx, cancellation) =>
            {
                string deviceId = ctx.Id.ToString();
                if (string.IsNullOrEmpty(deviceId))
                    return ResponseCode(ResultCode.BadFields, "Device Id is missing");

                var response = await Modules.Devices.GetDevice(deviceId, true);
                return Response.AsPlatformResponse(response);
            };

            #endregion
        }

        #region Generate Request Models

        private DeviceFilterRequest GenerateDeviceTelematicsRequest()
        {
            return new DeviceFilterRequest {Status = GetRelationStatus()};
        }

        private DeviceHistoryRequest GenerateDeviceHistoryRequest()
        {
            var param = 0;
            return new DeviceHistoryRequest
            {
                DeviceId = new ObjectId(GetQueryParameter("id")),
                Skip = int.TryParse(GetQueryParameter("skip"), out param) ? param : 0,
                Limit = int.TryParse(GetQueryParameter("limit"), out param) ? param : 200
            };
        }

        private RelationStatus GetRelationStatus()
        {
            var inputStatus = GetQueryParameter("relationStatus");
            if (inputStatus == null)
                return RelationStatus.All;
            RelationStatus relationStatus;
            if (Enum.IsDefined(typeof(RelationStatus), inputStatus) && Enum.TryParse(inputStatus, true, out relationStatus))
                return relationStatus;

            return RelationStatus.All;
        }

        private DeviceCustomerRequest GenerateDeviceCustomerRequest()
        {
            return new DeviceCustomerRequest() {CustomerId = GetQueryParameter("customerId"), Status = GetRelationStatus()};
        }

        #endregion Generate Request Models
    }
}