using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace FOODGOBACKEND.Helpers
{
    /// <summary>
    /// Helper class for geolocation calculations.
    /// </summary>
    public static class GeoLocationHelper
    {
        private const double EarthRadiusKm = 6371.0; // Earth's radius in kilometers
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Constants for delivery time estimation
        private const int PREP_TIME_MINUTES = 10; // Time for restaurant to prepare food
        private const int DEFAULT_SHIPPER_TO_RESTAURANT_TIME = 15; // Default time if no shipper assigned
        private const int AVG_SPEED_KM_PER_HOUR = 30; // Average speed for delivery (30 km/h)

        /// <summary>
        /// Calculates the distance between two geographic coordinates using the Haversine formula.
        /// </summary>
        /// <param name="lat1">Latitude of the first point.</param>
        /// <param name="lon1">Longitude of the first point.</param>
        /// <param name="lat2">Latitude of the second point.</param>
        /// <param name="lon2">Longitude of the second point.</param>
        /// <returns>Distance in kilometers, rounded to 1 decimal place.</returns>
        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Convert degrees to radians
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);

            // Haversine formula
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = EarthRadiusKm * c;

            return Math.Round(distance, 1);
        }

        /// <summary>
        /// Calculates distance with null checks.
        /// Returns 0 if any coordinate is null.
        /// </summary>
        /// <param name="userLat">User's latitude (optional).</param>
        /// <param name="userLng">User's longitude (optional).</param>
        /// <param name="targetLat">Target latitude (optional).</param>
        /// <param name="targetLng">Target longitude (optional).</param>
        /// <returns>Distance in kilometers, or 0 if coordinates are missing.</returns>
        public static double CalculateDistanceSafe(
            double? userLat, 
            double? userLng, 
            double? targetLat, 
            double? targetLng)
        {
            if (!userLat.HasValue || !userLng.HasValue || 
                !targetLat.HasValue || !targetLng.HasValue)
            {
                return 0;
            }

            return CalculateDistance(
                userLat.Value, 
                userLng.Value, 
                targetLat.Value, 
                targetLng.Value);
        }

        /// <summary>
        /// Calculates the shortest distance between two addresses.
        /// This method combines geocoding and distance calculation.
        /// </summary>
        /// <param name="fromAddress">Starting address (string format).</param>
        /// <param name="toAddress">Destination address (string format).</param>
        /// <returns>
        /// A tuple containing:
        /// - Success: Whether the calculation was successful
        /// - DistanceKm: Distance in kilometers (0 if failed)
        /// - FromCoordinates: Coordinates of the starting address (null if failed)
        /// - ToCoordinates: Coordinates of the destination address (null if failed)
        /// - ErrorMessage: Error message if failed (null if successful)
        /// </returns>
        public static async Task<(
            bool Success, 
            double DistanceKm, 
            (double Lat, double Lng)? FromCoordinates, 
            (double Lat, double Lng)? ToCoordinates,
            string? ErrorMessage)> CalculateDistanceBetweenAddresses(
                string fromAddress, 
                string toAddress)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(fromAddress))
                {
                    return (false, 0, null, null, "Starting address is required.");
                }

                if (string.IsNullOrWhiteSpace(toAddress))
                {
                    return (false, 0, null, null, "Destination address is required.");
                }

                // Geocode the starting address
                var fromCoords = await GetCoordinatesFromAddress(fromAddress);
                if (!fromCoords.HasValue)
                {
                    return (false, 0, null, null, $"Could not find coordinates for starting address: {fromAddress}");
                }

                // Geocode the destination address
                var toCoords = await GetCoordinatesFromAddress(toAddress);
                if (!toCoords.HasValue)
                {
                    return (false, 0, fromCoords, null, $"Could not find coordinates for destination address: {toAddress}");
                }

                // Calculate distance using Haversine formula
                var distance = CalculateDistance(
                    fromCoords.Value.Latitude,
                    fromCoords.Value.Longitude,
                    toCoords.Value.Latitude,
                    toCoords.Value.Longitude
                );

                return (
                    true, 
                    distance, 
                    (fromCoords.Value.Latitude, fromCoords.Value.Longitude),
                    (toCoords.Value.Latitude, toCoords.Value.Longitude),
                    null
                );
            }
            catch (Exception ex)
            {
                return (false, 0, null, null, $"Error calculating distance: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the shortest distance between two addresses (simplified version).
        /// Returns only the distance in kilometers, or null if calculation fails.
        /// </summary>
        /// <param name="fromAddress">Starting address (string format).</param>
        /// <param name="toAddress">Destination address (string format).</param>
        /// <returns>Distance in kilometers, or null if geocoding fails.</returns>
        public static async Task<double?> CalculateDistanceBetweenAddressesSimple(
            string fromAddress, 
            string toAddress)
        {
            if (string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(toAddress))
            {
                return null;
            }

            try
            {
                var fromCoords = await GetCoordinatesFromAddress(fromAddress);
                if (!fromCoords.HasValue) return null;

                var toCoords = await GetCoordinatesFromAddress(toAddress);
                if (!toCoords.HasValue) return null;

                return CalculateDistance(
                    fromCoords.Value.Latitude,
                    fromCoords.Value.Longitude,
                    toCoords.Value.Latitude,
                    toCoords.Value.Longitude
                );
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Calculate estimated delivery time based on order status and distances.
        /// Formula: Prep time (10 min) + Shipper pickup + Delivery
        /// </summary>
        /// <param name="orderStatus">Current order status.</param>
        /// <param name="restaurantAddress">Restaurant address.</param>
        /// <param name="deliveryAddress">Customer delivery address.</param>
        /// <param name="shipperLat">Shipper's current latitude (optional).</param>
        /// <param name="shipperLng">Shipper's current longitude (optional).</param>
        /// <param name="confirmedAt">Order confirmed timestamp (optional).</param>
        /// <param name="preparedAt">Order prepared timestamp (optional).</param>
        /// <param name="deliveringAt">Order delivering timestamp (optional).</param>
        /// <returns>Estimated time in minutes.</returns>
        public static async Task<int> CalculateEstimatedDeliveryTime(
            string orderStatus,
            string restaurantAddress,
            string deliveryAddress,
            double? shipperLat = null,
            double? shipperLng = null,
            DateTime? confirmedAt = null,
            DateTime? preparedAt = null,
            DateTime? deliveringAt = null)
        {
            var totalMinutes = 0;

            switch (orderStatus.ToUpper())
            {
                case "PENDING":
                case "CONFIRMED":
                    // Order is waiting: Prep time + Shipper pickup + Delivery
                    totalMinutes += PREP_TIME_MINUTES;
                    
                    // Time for shipper to reach restaurant
                    if (shipperLat.HasValue && shipperLng.HasValue)
                    {
                        // Calculate actual distance from shipper's current location to restaurant
                        var shipperToRestaurantResult = await CalculateDistanceBetweenAddresses(
                            $"{shipperLat},{shipperLng}",
                            restaurantAddress
                        );
                        
                        if (shipperToRestaurantResult.Success && shipperToRestaurantResult.DistanceKm > 0)
                        {
                            var shipperPickupTime = (int)Math.Ceiling((shipperToRestaurantResult.DistanceKm / AVG_SPEED_KM_PER_HOUR) * 60);
                            totalMinutes += shipperPickupTime;
                        }
                        else
                        {
                            totalMinutes += DEFAULT_SHIPPER_TO_RESTAURANT_TIME;
                        }
                    }
                    else
                    {
                        // No shipper assigned yet, use default time
                        totalMinutes += DEFAULT_SHIPPER_TO_RESTAURANT_TIME;
                    }
                    
                    // Time from restaurant to customer
                    var deliveryResult = await CalculateDistanceBetweenAddresses(
                        restaurantAddress,
                        deliveryAddress
                    );
                    
                    if (deliveryResult.Success && deliveryResult.DistanceKm > 0)
                    {
                        var deliveryTime = (int)Math.Ceiling((deliveryResult.DistanceKm / AVG_SPEED_KM_PER_HOUR) * 60);
                        totalMinutes += deliveryTime;
                    }
                    else
                    {
                        totalMinutes += 20; // Fallback: 20 minutes
                    }
                    break;

                case "PREPARING":
                    // Food is being prepared: remaining prep time + delivery time
                    if (confirmedAt.HasValue)
                    {
                        var prepElapsed = (int)(DateTime.UtcNow - confirmedAt.Value).TotalMinutes;
                        var remainingPrepTime = Math.Max(0, PREP_TIME_MINUTES - prepElapsed);
                        totalMinutes += remainingPrepTime;
                    }
                    else
                    {
                        totalMinutes += PREP_TIME_MINUTES;
                    }
                    
                    // Time for shipper to pick up (if not already picked up)
                    if (!preparedAt.HasValue)
                    {
                        if (shipperLat.HasValue && shipperLng.HasValue)
                        {
                            var shipperToRestaurantResult = await CalculateDistanceBetweenAddresses(
                                $"{shipperLat},{shipperLng}",
                                restaurantAddress
                            );
                            
                            if (shipperToRestaurantResult.Success)
                            {
                                var shipperPickupTime = (int)Math.Ceiling((shipperToRestaurantResult.DistanceKm / AVG_SPEED_KM_PER_HOUR) * 60);
                                totalMinutes += shipperPickupTime;
                            }
                        }
                        else
                        {
                            totalMinutes += DEFAULT_SHIPPER_TO_RESTAURANT_TIME;
                        }
                    }
                    
                    // Time from restaurant to customer
                    var prepDeliveryResult = await CalculateDistanceBetweenAddresses(
                        restaurantAddress,
                        deliveryAddress
                    );
                    
                    if (prepDeliveryResult.Success && prepDeliveryResult.DistanceKm > 0)
                    {
                        var deliveryTime = (int)Math.Ceiling((prepDeliveryResult.DistanceKm / AVG_SPEED_KM_PER_HOUR) * 60);
                        totalMinutes += deliveryTime;
                    }
                    else
                    {
                        totalMinutes += 20;
                    }
                    break;

                case "DELIVERING":
                    // Food is on the way: only remaining delivery time
                    var deliveringResult = await CalculateDistanceBetweenAddresses(
                        restaurantAddress,
                        deliveryAddress
                    );
                    
                    if (deliveringResult.Success && deliveringResult.DistanceKm > 0)
                    {
                        var deliveryTime = (int)Math.Ceiling((deliveringResult.DistanceKm / AVG_SPEED_KM_PER_HOUR) * 60);
                        
                        // If we have DeliveringAt timestamp, subtract elapsed time
                        if (deliveringAt.HasValue)
                        {
                            var deliveryElapsed = (int)(DateTime.UtcNow - deliveringAt.Value).TotalMinutes;
                            totalMinutes = Math.Max(5, deliveryTime - deliveryElapsed); // At least 5 minutes
                        }
                        else
                        {
                            totalMinutes = deliveryTime;
                        }
                    }
                    else
                    {
                        totalMinutes = 15; // Default 15 minutes
                    }
                    break;

                default:
                    totalMinutes = 0;
                    break;
            }

            return totalMinutes;
        }

        /// <summary>
        /// Converts a string address to latitude and longitude coordinates using Nominatim OpenStreetMap API.
        /// Improved version with multiple fallback strategies for Vietnamese addresses.
        /// </summary>
        /// <param name="address">The address string to geocode.</param>
        /// <returns>A tuple containing (latitude, longitude), or null if geocoding fails.</returns>
        public static async Task<(double Latitude, double Longitude)?> GetCoordinatesFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            try
            {
                // Strategy 1: Try original address with Vietnam country code
                var result = await TryGeocodeWithCountry(address, "vn");
                if (result.HasValue) return result;

                // Strategy 2: Try simplified address (remove "Số", "phường", "quận", etc.)
                var simplifiedAddress = SimplifyVietnameseAddress(address);
                result = await TryGeocodeWithCountry(simplifiedAddress, "vn");
                if (result.HasValue) return result;

                // Strategy 3: Try without diacritics (remove Vietnamese accents)
                var normalizedAddress = RemoveVietnameseDiacritics(address);
                result = await TryGeocodeWithCountry(normalizedAddress, "vn");
                if (result.HasValue) return result;

                // Strategy 4: Try just city/district for approximate location
                var cityOnlyAddress = ExtractCityAndDistrict(address);
                if (!string.IsNullOrEmpty(cityOnlyAddress))
                {
                    result = await TryGeocodeWithCountry(cityOnlyAddress, "vn");
                    if (result.HasValue) return result;
                }

                // Strategy 5: Try without country code (global search)
                result = await TryGeocode(address);
                if (result.HasValue) return result;

                return null;
            }
            catch (Exception ex)
            {
                // Log exception in production
                Console.WriteLine($"Geocoding error for address '{address}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Try to geocode an address with a specific country code.
        /// </summary>
        private static async Task<(double Latitude, double Longitude)?> TryGeocodeWithCountry(string address, string countryCode)
        {
            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&countrycodes={countryCode}&limit=5&addressdetails=1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "FoodGoApp/1.0 (Contact: admin@foodgo.com)");
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "vi,en");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;

                if (root.GetArrayLength() == 0)
                {
                    return null;
                }

                // Get the best result (first one is usually most relevant)
                var firstResult = root[0];
                
                if (firstResult.TryGetProperty("lat", out var latProp) && 
                    firstResult.TryGetProperty("lon", out var lonProp))
                {
                    var lat = latProp.GetString();
                    var lon = lonProp.GetString();

                    if (double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) && 
                        double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
                    {
                        Console.WriteLine($"✓ Found coordinates for: {address} -> ({latitude}, {longitude})");
                        return (latitude, longitude);
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Try to geocode an address without country restriction.
        /// </summary>
        private static async Task<(double Latitude, double Longitude)?> TryGeocode(string address)
        {
            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://nominatim.openstreetmap.org/search?q={encodedAddress}&format=json&limit=3";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "FoodGoApp/1.0");

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                
                using var document = JsonDocument.Parse(jsonString);
                var root = document.RootElement;

                if (root.GetArrayLength() == 0)
                {
                    return null;
                }

                var firstResult = root[0];
                var lat = firstResult.GetProperty("lat").GetString();
                var lon = firstResult.GetProperty("lon").GetString();

                if (double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) && 
                    double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
                {
                    return (latitude, longitude);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Simplifies Vietnamese address by removing common prefixes.
        /// Example: "Số 13-15 Lê Thánh Tông, phường Cửa Nam" -> "13-15 Lê Thánh Tông, Cửa Nam"
        /// </summary>
        private static string SimplifyVietnameseAddress(string address)
        {
            var simplified = address
                .Replace("Số ", "")
                .Replace("số ", "")
                .Replace("phường ", "")
                .Replace("Phường ", "")
                .Replace("quận ", "")
                .Replace("Quận ", "")
                .Replace("Thành phố ", "")
                .Replace("thành phố ", "")
                .Replace("Tỉnh ", "")
                .Replace("tỉnh ", "")
                .Replace("Huyện ", "")
                .Replace("huyện ", "")
                .Replace("Xã ", "")
                .Replace("xã ", "")
                .Replace("Thị trấn ", "")
                .Replace("thị trấn ", "");

            return simplified.Trim();
        }

        /// <summary>
        /// Removes Vietnamese diacritics (accents) from text.
        /// Example: "Hà Nội" -> "Ha Noi"
        /// </summary>
        private static string RemoveVietnameseDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace("đ", "d")
                .Replace("Đ", "D");
        }

        /// <summary>
        /// Extracts city and district from a full Vietnamese address.
        /// Example: "Số 13 Lê Thánh Tông, phường Cửa Nam, quận Hoàn Kiếm, Thành phố Hà Nội" -> "Hoàn Kiếm, Hà Nội"
        /// </summary>
        private static string ExtractCityAndDistrict(string address)
        {
            try
            {
                var parts = address.Split(',');
                
                // Try to find city (usually last or second-to-last part)
                var city = parts.LastOrDefault(p => 
                    p.Contains("Hà Nội") || p.Contains("Hồ Chí Minh") || 
                    p.Contains("Đà Nẵng") || p.Contains("Cần Thơ") ||
                    p.Contains("Thành phố") || p.Contains("Tỉnh"))?.Trim();

                // Try to find district
                var district = parts.FirstOrDefault(p => 
                    p.Contains("quận") || p.Contains("Quận") || 
                    p.Contains("huyện") || p.Contains("Huyện"))?.Trim();

                if (!string.IsNullOrEmpty(city))
                {
                    return string.IsNullOrEmpty(district) 
                        ? city 
                        : $"{district}, {city}";
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Batch geocode multiple addresses.
        /// Note: Please respect Nominatim's usage policy (max 1 request per second).
        /// </summary>
        /// <param name="addresses">List of addresses to geocode.</param>
        /// <returns>Dictionary mapping address to coordinates.</returns>
        public static async Task<Dictionary<string, (double Latitude, double Longitude)?>> GetCoordinatesFromAddresses(List<string> addresses)
        {
            var results = new Dictionary<string, (double Latitude, double Longitude)?>();

            foreach (var address in addresses)
            {
                var coords = await GetCoordinatesFromAddress(address);
                results[address] = coords;

                // Respect Nominatim usage policy: 1 request per second
                await Task.Delay(1000);
            }

            return results;
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="degrees">Angle in degrees.</param>
        /// <returns>Angle in radians.</returns>
        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="radians">Angle in radians.</param>
        /// <returns>Angle in degrees.</returns>
        public static double ToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        /// <summary>
        /// Validates if coordinates are within valid ranges.
        /// Latitude: -90 to 90, Longitude: -180 to 180
        /// </summary>
        /// <param name="latitude">Latitude to validate.</param>
        /// <param name="longitude">Longitude to validate.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValidCoordinates(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 &&
                   longitude >= -180 && longitude <= 180;
        }
    }
}