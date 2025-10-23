// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SuedtirolWein
{
    public class GetSuedtirolWeinData
    {
        private static async Task<HttpResponseMessage> RequestCompaniesAsync(
            string serviceurl,
            string lang
        )
        {
            try
            {
                string requesturl = serviceurl + "companies.ashx?lang=" + lang;

                using (var client = new HttpClient())
                {
                    var myresponse = await client.GetAsync(requesturl);

                    return myresponse;
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message),
                };
            }
        }

        private static async Task<HttpResponseMessage> RequestAwardsAsync(
            string serviceurl,
            string lang
        )
        {
            try
            {
                string requesturl = serviceurl + "awards.ashx?lang=" + lang;

                using (var client = new HttpClient())
                {
                    var myresponse = await client.GetAsync(requesturl);

                    return myresponse;
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(ex.Message),
                };
            }
        }

        public static async Task<XDocument> GetSuedtirolWineCompaniesAsync(
            string serviceurl,
            string lang
        )
        {
            //make Request
            HttpResponseMessage response = await RequestCompaniesAsync(serviceurl, lang);
            //Read Content and parse to XDocument
            var responsetask = await response.Content.ReadAsStringAsync();
            XDocument myweatherresponse = XDocument.Parse(responsetask);

            return myweatherresponse;
        }

        public static async Task<XDocument> GetSuedtirolWineAwardsAsync(
            string serviceurl,
            string lang
        )
        {
            //make Request
            HttpResponseMessage response = await RequestAwardsAsync(serviceurl, lang);
            //Read Content and parse to XDocument
            //var responsetask = await response.Content.ReadAsStringAsync();

            //Hack because content-type is malformed and response.Content.ReadAsStringAsync() throws error
            var bytes = await response.Content.ReadAsByteArrayAsync();

            Encoding encoding;
            try
            {
                var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"') ?? "utf-8";
                encoding = Encoding.GetEncoding(charset);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            var xml = encoding.GetString(bytes);

            XDocument myweatherresponse = XDocument.Parse(xml);

            return myweatherresponse;
        }
    }
}
