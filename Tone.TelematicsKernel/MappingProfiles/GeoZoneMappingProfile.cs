using Tone.Core;
using Tone.Core.Extensions;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.MappingProfiles
{
    public class GeozoneMappingProfile : PlatformAutomapperProfile
    {
        public GeozoneMappingProfile()
        {
            CreateMap<GeozoneCreateModel, Geozone>()
                .Ignore(d => d.Id)
                .Ignore(d => d.CustomerId)
                .Ignore(d => d.Points)
                .Ignore(d => d.GeoInfo)
                .Ignore(d => d.Created);

            CreateMap<GeozoneUpdateModel, Geozone>()
                .Ignore(d => d.Id)
                .Ignore(d => d.CustomerId)
                .Ignore(d => d.Points)
                .Ignore(d => d.GeoInfo)
                .Ignore(d => d.Created);


            CreateMap<Geozone, GeozoneViewModel>()
                .Ignore(d => d.EmployeeObjects)
                .Ignore(d => d.EmployeesCount)
                .Ignore(d => d.VehicleObjects)
                .Ignore(d => d.VehiclesCount);
        }
    }
}
