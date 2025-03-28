// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using Google.Apis.Auth.OAuth2;
using Humanizer;
using Newtonsoft.Json;

namespace PushServer
{
    #region Direct Send FCM to Firebase


    public class FCMPushNotification
    {
        public static async Task<PushResult> SendNotification(
            FCMModels fcmmessage,
            string fcmurl,
            string fcmsenderid,
            string fcmauthkey
        )
        {
            FCMPushNotification result = new FCMPushNotification();
            try
            {
                HttpClient myclient = new HttpClient();

                myclient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Authorization",
                    "key=" + fcmauthkey
                );
                myclient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Sender",
                    string.Format("id={0}", fcmsenderid)
                );

                var myresponse = await myclient.PostAsync(
                    fcmurl,
                    new StringContent(
                        JsonConvert.SerializeObject(fcmmessage),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

                var myresponseparsed = await myresponse.Content.ReadAsStringAsync();

                return new PushResult()
                {
                    Error = null,
                    Response = myresponseparsed,
                    Success = true,
                    Messages = 1,
                };
            }
            catch (Exception ex)
            {
                return new PushResult()
                {
                    Error = ex.Message,
                    Response = "Error",
                    Success = false,
                    Messages = 1,
                };
            }
        }

        public static async Task<PushResult> SendNotificationV2(
            FCMessageV2 fcmmessage,
            string fcmurl,
            string fcmserviceaccountjsonname
        )
        {
            FCMPushNotification result = new FCMPushNotification();
            try
            {
                HttpClient myclient = new HttpClient();

                //Getting GoogleCredentials from File
                var googlecred = await GetGoogleTokenServiceAccount(fcmserviceaccountjsonname);
                //GET THE Bearertoken out of the Google Credential
                var token = await googlecred.UnderlyingCredential.GetAccessTokenForRequestAsync();

                myclient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Authorization",
                    "Bearer " + token
                );

                var myresponse = await myclient.PostAsync(
                    fcmurl,
                    new StringContent(
                        JsonConvert.SerializeObject(fcmmessage),
                        Encoding.UTF8,
                        "application/json"
                    )
                );

                var myresponseparsed = await myresponse.Content.ReadAsStringAsync();

                return new PushResult()
                {
                    Error = null,
                    Response = myresponseparsed,
                    Success = true,
                    Messages = 1,
                };
            }
            catch (Exception ex)
            {
                return new PushResult()
                {
                    Error = ex.Message,
                    Response = "Error",
                    Success = false,
                    Messages = 1,
                };
            }
        }

        public static async Task<GoogleCredential> GetGoogleTokenServiceAccount(
            string fcmserviceaccountjsonname,
            bool fromfile = true
        )
        {
            //FromJson
            GoogleCredential? cred;

            string contents = File.ReadAllText(fcmserviceaccountjsonname);

            if (fromfile)
                cred = GoogleCredential
                    .FromFile(fcmserviceaccountjsonname)
                    .CreateScoped(new[] { "https://www.googleapis.com/auth/firebase.messaging" });
            else
                cred = GoogleCredential
                    .FromJson(fcmserviceaccountjsonname)
                    .CreateScoped(new[] { "https://www.googleapis.com/auth/firebase.messaging" });

            return cred;
        }

        public static async Task<UserCredential> GetGoogleTokenWebAuthorization(
            string fcmserviceaccountjsonname
        )
        {
            UserCredential credential;
            using (
                var stream = new FileStream(
                    fcmserviceaccountjsonname,
                    FileMode.Open,
                    FileAccess.Read
                )
            )
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { "https://www.googleapis.com/auth/firebase.messaging", "noi-community" },
                    "user",
                    CancellationToken.None
                );
            }

            return credential;
        }
    }

    #endregion
}
