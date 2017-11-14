using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Enums;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Core.WebContext;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel
{
    public class GeozoneModuleCore : TelematicsKernelSubsystemModule, IGeozoneModuleCore
    {
        #region GetAll

        public async Task<PlatformResponse<GeozoneViewModel[]>> GetAll(string accessType, string name)
        {
            DemandAuthorization();

            GeozoneAccessType access;
            var geozones = (!string.IsNullOrEmpty(accessType) && IsAccessTypeCorrect(accessType, out access))
                ? await Repositories.Geozones.GetAllByAccessType(access, name)
                : await Repositories.Geozones.GetAllByAccessType(null, name);

            var result = await BuildGeozoneViewModelCollection(geozones);
            return new PlatformResponse<GeozoneViewModel[]>(result);
        }

        #endregion GetAll

        #region GetGeozone

        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<IGeozone>> GetGeozone(object geozoneId)
        {
            DemandAuthorization();

            geozoneId = Repositories.Geozones.ToId(geozoneId);
            if (geozoneId == null)
                return new PlatformResponse<IGeozone>(ResultCode.BadFormat, "Не корректный параметр geozoneId");

            var geozone = await Repositories.Geozones.GetById(geozoneId);
            if (geozone == null)
                return new PlatformResponse<IGeozone>(ResultCode.NotFound);

            return new PlatformResponse<IGeozone>(geozone);
        }

        #endregion GetGeozone

        #region Ctreate Geozone

        public async Task<PlatformResponse<GeozoneViewModel>> Create(GeozoneCreateModel createModel)
        {
            DemandAuthorization();
            DemandValidation(createModel, true);

            if (createModel != null && (createModel.Access == null || createModel.Access == GeozoneAccessType.All))
                createModel.Access = GeozoneAccessType.PrivateToUser;

            var geozone = Mapper.Map<Geozone>(createModel) as IGeozone;

            geozone.Created = DateTime.UtcNow;
            if (createModel?.Points != null && createModel.Points.Any())
                geozone.Points = createModel.Points.Select(p => new Point {Latitude = p.Latitude, Longitude = p.Longitude}).ToArray();

            geozone = await Repositories.Geozones.SaveGeozone(geozone);
            var result = await BuildGeozoneViewModel(geozone);
            return new PlatformResponse<GeozoneViewModel>(result);
        }

        #endregion Ctreate Geozone

        #region Update Geozone

        public async Task<PlatformResponse<GeozoneViewModel>> Update(GeozoneUpdateModel updateModel)
        {
            DemandAuthorization();
            DemandValidation(updateModel, true);
            
            var id = Repositories.Geozones.ToId(updateModel.Id);
            var currentGeoZone = await Repositories.Geozones.GetById(id);
            if (currentGeoZone == null)
                return new PlatformResponse<GeozoneViewModel>(ResultCode.NotFound, "Geozone Not Found");

            if (updateModel != null && (updateModel.Access == null || updateModel.Access == GeozoneAccessType.All))
                updateModel.Access = GeozoneAccessType.PrivateToUser;

            var geozone = Mapper.Map(updateModel, currentGeoZone);

            // Points
            if (updateModel.Points != null && updateModel.Points.Any())
                geozone.Points = updateModel.Points.Select(p => new Point { Latitude = p.Latitude, Longitude = p.Longitude }).ToArray();

            // Radius
            if (geozone.Type != GeozoneType.Round)
                geozone.Radius = null;

            geozone = await Repositories.Geozones.SaveGeozone(geozone);
            var result = await BuildGeozoneViewModel(geozone);
            return new PlatformResponse<GeozoneViewModel>(result);
        }

        #endregion Update Geozone

        #region Delete Geozone

        public async Task<PlatformResponse<object>> Delete(object geozoneId)
        {
            DemandAuthorization();

            var id = Repositories.Geozones.ToId(geozoneId);
            if (id == null)
                return new PlatformResponse<object>(ResultCode.BadFormat, "Не корректный параметр geozoneId");

            var geozone = await Repositories.Geozones.GetById(id);
            if (geozone == null)
                return new PlatformResponse<object>(ResultCode.NotFound, "Goezone Not Found");

            // Clear Employee Geozone
            await Repositories.Employees.ClearEmployeeGeozone(id);

            // Clear Vehicle Geozone
            await Repositories.Vehicles.ClearVehicleGeozone(id);

            // Delete Geozone
            await Repositories.Geozones.DeleteOne(id);

            return new PlatformResponse<object>(ResultCode.Ok);
        }

        #endregion Delete Geozone

        public IGeozone CreateGeozoneObject() => new Geozone();

        internal bool IsAccessTypeCorrect(string accessType, out GeozoneAccessType geozoneAccessType)
        {
            geozoneAccessType=GeozoneAccessType.PrivateToUser;
            var result = Enum.IsDefined(typeof(GeozoneAccessType), accessType) && Enum.TryParse(accessType, true, out geozoneAccessType);
            return result;
        }

        [Obsolete("Method Do Not Use")]
        public async Task<PlatformResponse<bool>> IntersectPoint(object geozoneId, Point point)
        {
            var id = Repositories.Geozones.ToId(geozoneId);
            var geozone = await Repositories.Geozones.GetById(id);
            if (geozone == null && geozone.Points == null || !geozone.Points.Any())
                return new PlatformResponse<bool>(false);

            var result = geozone.Points.FirstOrDefault(g => g.Latitude == point.Latitude && g.Longitude == point.Longitude);
            return new PlatformResponse<bool>(result != null);
        }

        public async Task<PlatformResponse<GeozoneViewModel[]>> GetGeozonesByCustomer(object customerId)
        {
            DemandAuthorization();

            var id = Repositories.Customers.ToId(customerId);
            var customer = await Repositories.Customers.GetById(id);

            if (customer == null)
                return new PlatformResponse<GeozoneViewModel[]>(ResultCode.NotFound, "Customer not Found");

            var geozones = await Repositories.Geozones.GetGeozonesByCustomer(customerId);
            var result = await BuildGeozoneViewModelCollection(geozones);
            return new PlatformResponse<GeozoneViewModel[]>(result);
        }


        #region BuildGeozoneViewModel
        private async Task<GeozoneViewModel> BuildGeozoneViewModel(IGeozone geozone)
        {
        
                var geozoneModel = Mapper.Map<GeozoneViewModel>(geozone);

                var vehicles = await Repositories.Vehicles.GetVehicleIdsByGeozoneId(geozone.Id);
                if (vehicles != null && vehicles.Any())
                {
                    geozoneModel.VehicleObjects = vehicles;
                    geozoneModel.VehiclesCount = vehicles.Length;
                }

                var employees = await Repositories.Employees.GetEmloyeeIdsByGeozoneId(geozone.Id);
                if (employees != null && employees.Any())
                {
                    geozoneModel.EmployeeObjects = employees;
                    geozoneModel.EmployeesCount = employees.Length;
                }


            return geozoneModel;
        }

        private async Task<GeozoneViewModel[]> BuildGeozoneViewModelCollection(IGeozone[] geozones)
        {
            var geozoneViewModelList = new List<GeozoneViewModel>();
            foreach (var geozone in geozones)
                geozoneViewModelList.Add(await BuildGeozoneViewModel(geozone));
                
            return geozoneViewModelList.ToArray();
        }

	    public async Task<PlatformResponse<IGeozone[]>> FindGeozonesByPoint(double longitude, double latitude)
	    {
			DemandAuthorization();

		    var geozoneIds = await Repositories.GeozoneInfoRepository.GetGeozoneInfo(longitude, latitude);
		    var geozones = await Repositories.Geozones.GetByIds(geozoneIds);
		    return new PlatformResponse<IGeozone[]>(geozones);
	    }

	    #endregion
    }
}