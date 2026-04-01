using MOMENTUS.Model;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MOMENTUS
{
    public class GetDataFromMomentus
    {
        public static EventSearchRequest GetEventSearchRequest(DateOnly datefrom, DateOnly dateto, List<string> venueids, List<string> roomids, bool includebookedspaces)
        {
            return new EventSearchRequest
            {
                Start = datefrom,
                End = dateto,
                VenueIds = venueids.ToArray(),
                RoomIds = roomids.ToArray(),
                IncludeBookedSpaces = true
            };
        }

        public static async Task<IEnumerable<MomentusEvent>> RequestMomentusEvents(string url, string clientid, string clientsecret, string authurl, EventSearchRequest eventsearchrequest)
        {
            var authresponse = await GetAccessTokenAsync(authurl, clientid, clientsecret);

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authresponse.AccessToken);

                //var request = new EventSearchRequest
                //{
                //    Start = new DateOnly(2026, 2, 24),
                //    End = new DateOnly(2027, 2, 28),
                //    VenueIds = ["venue-1-A"],
                //    RoomIds = ["room-44-A"],
                //    IncludeBookedSpaces = true
                //};


                //var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var response = await client.PostAsJsonAsync(url + "events/query-by-date-range", eventsearchrequest);

                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<IEnumerable<MomentusEvent>>(jsonResponse, options);                

                return data;
            }
        }

        public static async Task<MomentusEvent> RequestMomentusEventSingle(string url, string clientid, string clientsecret, string authurl, string eventid)
        {
            var authresponse = await GetAccessTokenAsync(authurl, clientid, clientsecret);

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authresponse.AccessToken);

                var response = await client.GetAsync(url + "events/" + eventid);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<MomentusEvent>(jsonResponse, options);

                return data;
            }
        }

        public static async Task<MomentusEvent> RequestMomentusRoomResponse(string url, string clientid, string clientsecret, string authurl)
        {
            var authresponse = await GetAccessTokenAsync(authurl, clientid, clientsecret);

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authresponse.AccessToken);

                var response = await client.GetAsync(url + "general-setup/rooms");
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<MomentusEvent>(jsonResponse, options);

                return data;
            }
        }

        public static async Task<IEnumerable<MomentusFunction>> RequestMomentusRoomResponse(string url, string clientid, string clientsecret, string authurl, string eventid)
        {
            var authresponse = await GetAccessTokenAsync(authurl, clientid, clientsecret);

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authresponse.AccessToken);

                var response = await client.GetAsync(url + "functions/event/" + eventid);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<IEnumerable<MomentusFunction>>(jsonResponse, options);

                return data;
            }
        }

        private static async Task<MomentusTokenResponse> GetAccessTokenAsync(
            string authurl,
            string clientId,
            string clientSecret)
        {
            var requestBody = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)                
            });

            HttpClient _httpClient = new HttpClient();
            var response = await _httpClient.PostAsync(authurl, requestBody);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<MomentusTokenResponse>(jsonResponse);

            return token;
        }
    }

}
