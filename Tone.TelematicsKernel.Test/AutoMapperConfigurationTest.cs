using AutoMapper;
using Tone.TelematicsKernel.MappingProfiles;
using Xunit;


namespace Tone.TelematicsKernel.Test
{
    public class AutoMapperConfigurationTest
    {

        [Fact]
        public void Test_AutoMapperConfiguration()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<TelematicDeviceMappingProfile>();
                cfg.AddProfile<GeozoneMappingProfile>();

            });

            Mapper.AssertConfigurationIsValid();
        }
    }
}
