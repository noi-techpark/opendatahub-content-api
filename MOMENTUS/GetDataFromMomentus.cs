// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using MOMENTUS.Model;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

        public static async Task<IEnumerable<MomentusEvent>> RequestMomentusEvents(string url, string? clientid, string? clientsecret, string? authurl, EventSearchRequest eventsearchrequest, MomentusTokenResponse? authtoken)
        {      
            using (var client = new HttpClient())
            {
                //Reuse the token if passed (CURRENTLY no Validity Check)
                if(authtoken == null)
                {
                    if (String.IsNullOrEmpty(authurl) || String.IsNullOrEmpty(clientid) || String.IsNullOrEmpty(clientsecret))
                        throw new Exception("missing auth config");

                    authtoken = await GetAccessTokenAsync(authurl, clientid, clientsecret);
                }
                
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authtoken.AccessToken);

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

        public static async Task<MomentusEvent> RequestMomentusEventSingle(string url, string? clientid, string? clientsecret, string? authurl, string eventid, MomentusTokenResponse? authtoken)
        {
            using (var client = new HttpClient())
            {

                //Reuse the token if passed (CURRENTLY no Validity Check)
                if (authtoken == null)
                {
                    if (String.IsNullOrEmpty(authurl) || String.IsNullOrEmpty(clientid) || String.IsNullOrEmpty(clientsecret))
                        throw new Exception("missing auth config");

                    authtoken = await GetAccessTokenAsync(authurl, clientid, clientsecret);
                }

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authtoken.AccessToken);

                var response = await client.GetAsync(url + "events/" + eventid);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<MomentusEvent>(jsonResponse, options);

                return data;
            }
        }

        public static async Task<IEnumerable<MomentusRoom>> RequestMomentusRooms(string url, string? clientid, string? clientsecret, string? authurl, MomentusTokenResponse? authtoken)
        {            
            using (var client = new HttpClient())
            {
                //Reuse the token if passed (CURRENTLY no Validity Check)
                if (authtoken == null)
                {
                    if(String.IsNullOrEmpty(authurl) || String.IsNullOrEmpty(clientid) || String.IsNullOrEmpty(clientsecret))
                            throw new Exception("missing auth config");

                    authtoken = await GetAccessTokenAsync(authurl, clientid, clientsecret);
                }

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authtoken.AccessToken);

                var response = await client.GetAsync(url + "general-setup/rooms");
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<IEnumerable<MomentusRoom>>(jsonResponse, options);

                return data;
            }
        }

        public static async Task<IEnumerable<MomentusFunction>> RequestMomentusFunction(string url, string? clientid, string? clientsecret, string? authurl, string eventid, MomentusTokenResponse? authtoken)
        {            
            using (var client = new HttpClient())
            {
                //Reuse the token if passed (CURRENTLY no Validity Check)
                if (authtoken == null)
                {
                    if (String.IsNullOrEmpty(authurl) || String.IsNullOrEmpty(clientid) || String.IsNullOrEmpty(clientsecret))
                        throw new Exception("missing auth config");

                    authtoken = await GetAccessTokenAsync(authurl, clientid, clientsecret);
                }

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authtoken.AccessToken);

                var response = await client.GetAsync(url + "functions/event/" + eventid);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<IEnumerable<MomentusFunction>>(jsonResponse, options);

                return data;
            }
        }

        public static async Task<MomentusTokenResponse> GetAccessTokenAsync(
            string authurl,
            string clientId,
            string clientSecret)
        {
            var jsonBody = JsonSerializer.Serialize(new { clientId, clientSecret });
            var requestBody = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpClient _httpClient = new HttpClient();
            var response = await _httpClient.PostAsync(authurl, requestBody);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var token = JsonSerializer.Deserialize<MomentusTokenResponse>(jsonResponse, options);

            return token;
        }
    }

}
