using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tone.Core.Subsystems.BusinessObjects.Providers;
using Tone.Core.Subsystems.TelematicsKernel.Model.GeoProviders;
using Tone.Core.Subsystems.TelematicsKernel.Repositories.GeoProviders;

namespace Tone.TelematicsKernel.Data.Providers.GeoProviders
{
    public class GeocodingProvider : IGeocodingProvider
    {
        private readonly IGeocodeInfoRepository _geocodeInfoRepository;

        public GeocodingProvider(IGeocodeInfoRepository geocodeInfoRepository)
        {
            _geocodeInfoRepository = geocodeInfoRepository;
        }

        public async Task<IGeocodeInfo> GetGeocodeInfo(double longitude, double latitude, int distance = 15)
        {
            var result = await _geocodeInfoRepository.GetGeocodeInfo(longitude, latitude, distance).ConfigureAwait(false);
            return result;
        }

        public async Task<string> GetAddress(double longitude, double latitude, int distance = 15)
        {
            var geocodeInfo = await GetGeocodeInfo(longitude, latitude, distance);
            if (geocodeInfo == null)
                return string.Empty;

            var address =
            (string.IsNullOrEmpty(geocodeInfo.City?.Trim())
                ? (geocodeInfo.Subject ?? "Неизвестная область")
                : string.Empty);
            try
            {
                address +=
                    (!string.IsNullOrEmpty(geocodeInfo.City?.Trim()) ? (
                    (!string.IsNullOrEmpty(address) ? ", " : string.Empty) + geocodeInfo.City) : string.Empty)
                    + (!string.IsNullOrEmpty(geocodeInfo.Street?.Trim()) ? (", " + geocodeInfo.Street) : string.Empty)
                    + (!string.IsNullOrEmpty(geocodeInfo.Building?.Trim()) ? (", д." + geocodeInfo.Building) : string.Empty)
                    + (!string.IsNullOrEmpty(geocodeInfo.Vladenie?.Trim()) ? (", влад." + geocodeInfo.Vladenie) : string.Empty)
                    +(!string.IsNullOrEmpty(geocodeInfo.Corpus?.Trim()) ? (", корп." + geocodeInfo.Corpus) : string.Empty)
                    +(!string.IsNullOrEmpty(geocodeInfo.Stroenie?.Trim()) ? (", стр." + geocodeInfo.Stroenie) : string.Empty)
                    +(!string.IsNullOrEmpty(geocodeInfo.Soorugenie?.Trim()) ? (", с." + geocodeInfo.Soorugenie) : string.Empty);
            }
            catch (Exception e)
            {
                e.ToString();
            }
            return address;
        }
    }
}
