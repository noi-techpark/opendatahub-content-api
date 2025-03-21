// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;

namespace PushServer
{
    public class FCMMessageConstructor
    {
        public static FCMModels? ConstructMyMessage(
            string identifier,
            string language,
            IIdentifiable myobject
        )
        {
            var message = default(FCMModels);

            if (
                (identifier == "noicommunity" || identifier == "noi-communityapp")
                && myobject is ArticlesLinked
            )
            {
                message = new FCMModels();

                message.to = "/topics/newsfeednoi_" + language.ToLower();

                string deeplink = "noi-community://it.bz.noi.community/newsDetails/" + myobject.Id;

                message.data = new { deep_link = deeplink };
                FCMNotification notification = new FCMNotification();

                //notification.icon = "ic_notification";
                //notification.link = deeplink;

                notification.title =
                    ((ArticlesLinked)myobject).Detail.ContainsKey(language)
                    && !String.IsNullOrEmpty(((ArticlesLinked)myobject).Detail[language].Title)
                        ? ((ArticlesLinked)myobject).Detail[language].Title
                        : "Noi Community App News";
                notification.body =
                    ((ArticlesLinked)myobject).Detail.ContainsKey(language)
                    && !String.IsNullOrEmpty(
                        ((ArticlesLinked)myobject).Detail[language].AdditionalText
                    )
                        ? ((ArticlesLinked)myobject).Detail[language].AdditionalText
                        : "Check out the latest News on the NOI Community App";

                //notification.sound = "default";

                message.notification = notification;
            }
            else if (
                (identifier == "noicommunityapp" || identifier == "noi-communityapp")
                && myobject is EventShortLinked
            )
            {
                message = new FCMModels();

                message.to = "/topics/events_" + language.ToLower();

                string deeplink = "noi-community://it.bz.noi.community/eventDetails/" + myobject.Id;

                message.data = new { deep_link = deeplink };
                FCMNotification notification = new FCMNotification();

                //notification.icon = "ic_notification";
                //notification.link = deeplink;

                notification.title =
                    ((EventShortLinked)myobject).EventTitle.ContainsKey(language)
                    && !String.IsNullOrEmpty(((EventShortLinked)myobject).EventTitle[language])
                        ? ((EventShortLinked)myobject).EventTitle[language]
                        : "Noi Community App Event";
                notification.body =
                    ((EventShortLinked)myobject).EventText.ContainsKey(language)
                    && !String.IsNullOrEmpty(((EventShortLinked)myobject).EventText[language])
                        ? ((EventShortLinked)myobject).EventText[language]
                        : "Check out the latest Events on the NOI Community App";

                //notification.sound = "default";

                message.notification = notification;
            }

            return message;
        }

        public static FCMessageV2? ConstructMyMessageV2(
            string identifier,
            string language,
            IIdentifiable myobject
        )
        {
            var message = default(FCMessageV2);

            if (
                (identifier == "noicommunity" || identifier == "noi-communityapp")
                && myobject is ArticlesLinked
            )
            {
                message = new FCMessageV2();
                var messagebody = new FCMessageBodyV2();

                messagebody.topic = "newsfeednoi_" + language.ToLower();

                string deeplink = "noi-community://it.bz.noi.community/newsDetails/" + myobject.Id;

                //messagebody.data = new { deep_link = deeplink };
                messagebody.data = new Dictionary<string, string>()
                {
                    { "deep_link", deeplink },
                    { "gcm.n.link_android", deeplink },
                };

                FCMNotification notification = new FCMNotification();

                //notification.icon = "ic_notification";
                //notification.link = deeplink;

                notification.title =
                    ((ArticlesLinked)myobject).Detail.ContainsKey(language)
                    && !String.IsNullOrEmpty(((ArticlesLinked)myobject).Detail[language].Title)
                        ? ((ArticlesLinked)myobject).Detail[language].Title
                        : "Noi Community App News";
                notification.body =
                    ((ArticlesLinked)myobject).Detail.ContainsKey(language)
                    && !String.IsNullOrEmpty(
                        ((ArticlesLinked)myobject).Detail[language].AdditionalText
                    )
                        ? ((ArticlesLinked)myobject).Detail[language].AdditionalText
                        : "Check out the latest News on the NOI Community App";

                //notification.sound = "default";

                messagebody.notification = notification;

                message.message = messagebody;
            }
            else if (
                (identifier == "noicommunity" || identifier == "noi-communityapp")
                && myobject is EventShortLinked
            )
            {
                message = new FCMessageV2();
                var messagebody = new FCMessageBodyV2();

                messagebody.topic = "events_" + language.ToLower();

                string deeplink = "noi-community://it.bz.noi.community/eventDetails/" + myobject.Id;

                messagebody.data = new { deep_link = deeplink };
                FCMNotification notification = new FCMNotification();

                string titleprefix = "New Event: ";
                if (language.ToLower() == "de")
                    titleprefix = "Neue Veranstaltung: ";
                if (language.ToLower() == "it")
                    titleprefix = "nuovo evento: ";

                //notification.icon = "ic_notification";
                //notification.link = deeplink;

                notification.title =
                    ((EventShortLinked)myobject).EventTitle.ContainsKey(language)
                    && !String.IsNullOrEmpty(((EventShortLinked)myobject).EventTitle[language])
                        ? titleprefix + ((EventShortLinked)myobject).EventTitle[language]
                        : "Noi Community App Event";
                notification.body =
                    ((EventShortLinked)myobject).EventText.ContainsKey(language)
                    && !String.IsNullOrEmpty(((EventShortLinked)myobject).EventText[language])
                        ? ((EventShortLinked)myobject).EventText[language]
                        : "Check out the latest Events on the NOI Community App";

                //notification.sound = "default";

                messagebody.notification = notification;

                message.message = messagebody;
            }

            return message;
        }
    }
}
