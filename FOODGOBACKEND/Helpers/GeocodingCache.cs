using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace FOODGOBACKEND.Helpers
{
    public class GeocodingCache
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromDays(30);

        public GeocodingCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            var cacheKey = $"geocode_{address.ToLower().Trim()}";

            // Try get from cache
            if (_cache.TryGetValue<(double, double)?>(cacheKey, out var cachedCoords))
            {
                return cachedCoords;
            }

            // Not in cache, call API
            var coords = await GeoLocationHelper.GetCoordinatesFromAddress(address);

            // Save to cache
            if (coords.HasValue)
            {
                _cache.Set(cacheKey, coords, CacheExpiration);
            }

            return coords;
        }

        public async Task<Dictionary<string, (double Lat, double Lng)?>> GetCoordinatesBatchAsync(List<string> addresses)
        {
            var results = new Dictionary<string, (double Lat, double Lng)?>();
            var addressesToGeocode = new List<string>();

            // Check cache first
            foreach (var address in addresses)
            {
                var cacheKey = $"geocode_{address.ToLower().Trim()}";
                
                if (_cache.TryGetValue<(double, double)?>(cacheKey, out var cachedCoords))
                {
                    if (cachedCoords.HasValue)
                        results[address] = (cachedCoords.Value.Item1, cachedCoords.Value.Item2);
                }
                else
                {
                    addressesToGeocode.Add(address);
                }
            }

            // Geocode missing addresses
            foreach (var address in addressesToGeocode)
            {
                var coords = await GeoLocationHelper.GetCoordinatesFromAddress(address);
                
                if (coords.HasValue)
                {
                    var cacheKey = $"geocode_{address.ToLower().Trim()}";
                    _cache.Set(cacheKey, coords, CacheExpiration);
                    results[address] = (coords.Value.Latitude, coords.Value.Longitude);
                }

                // Rate limiting
                if (addressesToGeocode.Count > 1)
                    await Task.Delay(1000);
            }

            return results;
        }
    }
}