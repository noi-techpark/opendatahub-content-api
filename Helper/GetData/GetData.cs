// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;

namespace Helper.GetData
{
    public enum GetDataAuthenticationOptions
    {
        None,
        Basic,
        Bearer,
    }

    public class GetData
    {
        private string serviceurl;
        private string user;
        private string pass;
        private string bearertoken;
        private GetDataAuthenticationOptions authtype;

        public GetData(
            string _serviceurl,
            string _user,
            string _pass,
            string _bearertoken,
            GetDataAuthenticationOptions _authtype
        )
        {
            serviceurl = _serviceurl;
            user = _user;
            pass = _pass;
            bearertoken = _bearertoken;
            authtype = _authtype;
        }

        private async Task<HttpResponseMessage> GetDataFromService()
        {
            if (authtype == GetDataAuthenticationOptions.Basic)
            {
                CredentialCache wrCache = new CredentialCache();
                wrCache.Add(new Uri(serviceurl), "Basic", new NetworkCredential(user, pass));

                using (var handler = new HttpClientHandler { Credentials = wrCache, AutomaticDecompression = DecompressionMethods.All })
                {
                    using (var client = new HttpClient(handler))
                    {
                        return await client.GetAsync(serviceurl);
                    }
                }
            }
            if (authtype == GetDataAuthenticationOptions.Bearer)
            {
                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        bearertoken
                    );

                    return await client.GetAsync(serviceurl);
                }
            }
            else
            {
                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }))
                {
                    return await client.GetAsync(serviceurl);
                }
            }
        }

        public async Task<dynamic?> GetDataAsJsonAsync()
        {
            //Request
            HttpResponseMessage response = await GetDataFromService();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error on getting data " + response.StatusCode.ToString());

            //Parse JSON Response to
            var responsecontent = await response.Content.ReadAsStringAsync();
            dynamic? responseobject = JsonConvert.DeserializeObject(responsecontent);

            return responseobject;
        }

        public async Task<XDocument> GetDataAsXmlAsync()
        {
            //Request
            HttpResponseMessage response = await GetDataFromService();

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Error on getting data " + response.StatusCode.ToString());

            //Parse JSON Response to
            var responsecontent = await response.Content.ReadAsStringAsync();
            XDocument responseobject = XDocument.Parse(responsecontent);

            return responseobject;
        }
    }
}
