

using System.Text.Json;

namespace ZOHO
{
    public class GetDataFromZoho
    {
        public static async Task<IEnumerable<ZohoRootobject>> RequestDataFromZoho(string url, string clientid, string clientsecret, string authurl, string scope)
        {
            var authresponse = await GetAccessTokenAsync(authurl, clientid, clientsecret, scope);

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "Zoho-oauthtoken " + authresponse.access_token);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<ZohoResponse>(jsonResponse);

                return data.data;
            }
        }

        private static async Task<ZohoAuthToken> GetAccessTokenAsync(
            string authurl,
            string clientId,
            string clientSecret,
            string scope)
        {
            var requestBody = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", scope)
            });

            HttpClient _httpClient = new HttpClient();
            var response = await _httpClient.PostAsync(authurl, requestBody);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<ZohoAuthToken>(jsonResponse);

            return token;
        }
    }

}
