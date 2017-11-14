using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core;
using Tone.Core.Extensions;
using Tone.Core.Subsystems.TelematicsKernel;
using Tone.Core.Subsystems.TelematicsKernel.Model;
using Tone.Data.Mongo.Model;

namespace Tone.TelematicsKernel.MappingProfiles
{
    public class DriveStyleMappingProfile : PlatformAutomapperProfile
    {
        public DriveStyleMappingProfile()
        {
            // From Model To Entity
            CreateMap<DriveStylePenaltyScoresModel, DriveStylePenaltyScores>()
                .Ignore(g => g.Id)
                .Ignore(g => g.CustomerId);
            CreateMap<DriveStyleCoefficientsModel, DriveStyleCoefficients>()
                .Ignore(g => g.Id)
                .Ignore(g => g.CustomerId);
            CreateMap<DriveStyleVehiclePenaltiesModel, DriveStyleVehiclePenalties>()
                .Ignore(g => g.Id)
                .Ignore(g => g.CustomerId);


            // From Entity To Model
            CreateMap<DriveStylePenaltyScores, DriveStylePenaltyScoresModel>()
                .Ignore(g => g.CustomerId);
            CreateMap<DriveStyleCoefficients, DriveStyleCoefficientsModel>()
                .Ignore(g => g.CustomerId);
            CreateMap<DriveStyleVehiclePenalties, DriveStyleVehiclePenaltiesModel>()
                .Ignore(g => g.CustomerId);
        }
    }
}
