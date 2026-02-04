using GoogleMapsGeocoding;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace DATNN.Service
{

    public class GeocodingService
    {
        private readonly HttpClient _httpClient;

        public GeocodingService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DATNN_Project/1.0 (contact@datnn.com)");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("vi");  // QUAN TRỌNG
        }

        public async Task<(double? Latitude, double? Longitude)> LayToaDoTuDiaChiAsync(
            string chiTiet, string phuong, string quan, string thanhPho)
        {
            // 1. Địa chỉ đầy đủ nhất
            if (!string.IsNullOrWhiteSpace(chiTiet))
            {
                var addr = $"{chiTiet}, {phuong}, {quan}, {thanhPho}, Việt Nam";
                var toaDo = await TimKiemToaDoAsync(addr);
                if (toaDo != null) return toaDo.Value;
            }

            // 2. Phường – quận – thành phố
            var addr2 = $"{phuong}, {quan}, {thanhPho}, Việt Nam";
            var toaDo2 = await TimKiemToaDoAsync(addr2);
            if (toaDo2 != null) return toaDo2.Value;

            // 3. Quận – thành phố
            var addr3 = $"{quan}, {thanhPho}, Việt Nam";
            var toaDo3 = await TimKiemToaDoAsync(addr3);
            if (toaDo3 != null) return toaDo3.Value;

            return (null, null);
        }


        private async Task<(double? Latitude, double? Longitude)?> TimKiemToaDoAsync(string diaChi)
        {
            if (string.IsNullOrWhiteSpace(diaChi)) return null;

            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(diaChi)}&format=json&limit=1";

            try
            {
                // BẮT BUỘC: delay 1 giây mỗi request để không bị chặn
                await Task.Delay(1000);

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[OSM ERROR] {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

                    // Retry nếu bị 429 rate-limit
                    if ((int)response.StatusCode == 429)
                    {
                        await Task.Delay(1500);
                        response = await _httpClient.GetAsync(url);
                    }

                    if (!response.IsSuccessStatusCode)
                        return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var json = JsonDocument.Parse(jsonString);

                if (json.RootElement.GetArrayLength() == 0)
                    return null;

                var item = json.RootElement[0];

                double lat = double.Parse(item.GetProperty("lat").GetString(), System.Globalization.CultureInfo.InvariantCulture);
                double lon = double.Parse(item.GetProperty("lon").GetString(), System.Globalization.CultureInfo.InvariantCulture);

                return (lat, lon);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION] Geocoding error for '{diaChi}' : {ex.Message}");
                return null;
            }
        }
    }
}
