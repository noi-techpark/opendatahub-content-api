﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WeatherData
{
    public class GetWeatherFromSIAG
    {
        public const string serviceurlsiag = @"https://wetter.ws.siag.it/Weather_V1.svc/web/getLastProvBulletin";
        public const string serviceurlbezirksiag = @"https://wetter.ws.siag.it/Agriculture_V1.svc/web/getLastBulletin";

        public const string serviceurlrealtime = @"http://weather.services.siag.it/api/v2/station";

        public const string serviceurl = @"http://daten.buergernetz.bz.it/services/weather/bulletin";
        public const string serviceurlbezirk = @" http://daten.buergernetz.bz.it/services/weather/district/";

        public static async Task<HttpResponseMessage> RequestAsync(string lang, string siaguser, string siagpswd, string source)
        {
            try
            {
                string requesturl = serviceurl + "?lang=" + lang + "&format=xml";
                if (source == "siag")
                    requesturl = serviceurlsiag + "?lang=" + lang + "&format=xml";

                CredentialCache wrCache = new CredentialCache();
                wrCache.Add(new Uri(requesturl), "Basic", new NetworkCredential(siaguser, siagpswd));

                using (var handler = new HttpClientHandler { Credentials = wrCache })
                {
                    using (var client = new HttpClient(handler))
                    {
                        var myresponse = await client.GetAsync(requesturl);

                        return myresponse;
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(ex.Message) };
            }

        }

        public static async Task<HttpResponseMessage> RequestBezirksWeatherAsync(string lang, string distid, string siaguser, string siagpswd, string source)
        {
            try
            {
                string requesturl = serviceurlbezirk + distid + "/bulletin?lang=" + lang + "&format=xml";

                if (source == "siag")
                    requesturl = serviceurlbezirksiag + "?lang=" + lang + "&dist=" + distid;

                CredentialCache wrCache = new CredentialCache();
                wrCache.Add(new Uri(requesturl), "Basic", new NetworkCredential(siaguser, siagpswd));

                using (var handler = new HttpClientHandler { Credentials = wrCache })
                {
                    using (var client = new HttpClient(handler))
                    {
                        var myresponse = await client.GetAsync(requesturl);

                        return myresponse;
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(ex.Message) };
            }

        }

        public static async Task<HttpResponseMessage> RequestRealtimeWeatherAsync(string lang)
        {
            try
            {
                string requesturl = serviceurlrealtime + "?lang=" + lang;

                using (var handler = new HttpClientHandler { })
                {
                    using (var client = new HttpClient(handler))
                    {
                        var myresponse = await client.GetAsync(requesturl);

                        return myresponse;
                    }
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(ex.Message) };
            }
        }

    }
}
