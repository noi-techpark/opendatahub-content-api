// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using Helper;
using Helper.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PANOCLOUD
{
    public class ParsePanocloudToODH
    {
        //Panocloud returns a single object instead of an array when only one element is present (XML to JSON conversion)
        private static IEnumerable<dynamic> ToList(dynamic token)
        {
            if (token == null)
                return new List<dynamic>();

            return token is JArray ? (IEnumerable<dynamic>)token : new List<dynamic>() { token };
        }

        public static WebcamInfoLinked ParseWebcamToWebcamInfo(
            WebcamInfoLinked webcam,
            dynamic webcamtoparse,
            string odhid
        )
        {
            if (webcam == null)
                webcam = new WebcamInfoLinked();

            webcam.Source = "panocloud";
            webcam.Id = odhid; //no id in panocloud

            if ((string)webcamtoparse["@attributes"]["cameraStatus"] == "active")
                webcam.Active = true;
            else
                webcam.Active = false;

            webcam.SmgActive = webcam.Active;

            if ((string)webcamtoparse["@attributes"]["full360"] == "yes")
                webcam.WebCamProperties.ViewAngleDegree = "360";
            else
                webcam.WebCamProperties.ViewAngleDegree = "";

            if ((string)webcamtoparse["@attributes"]["hasVR"] == "yes")
                webcam.WebCamProperties.HasVR = true;
            else
                webcam.WebCamProperties.HasVR = false;

            webcam.WebCamProperties.ViewerType = (string)webcamtoparse["@attributes"]["viewerType"];
            webcam.WebCamProperties.WebcamUrl = (
                (string)webcamtoparse["@attributes"]["url"]
            ).AddHttpsPrefixIfNotPresent();

            webcam.LastChange = Convert.ToDateTime(webcamtoparse["@attributes"]["lastModified"]);
            webcam.Shortname = (string)webcamtoparse["@attributes"]["name"];

            var defaultlanguage = (string)webcamtoparse["@attributes"]["defaultLang"];
            if (String.IsNullOrEmpty(defaultlanguage))
                defaultlanguage = "de";

            //Detail
            Detail detail = new Detail();
            detail.Title = (string)webcamtoparse["@attributes"]["name"];
            detail.IntroText = (string)webcamtoparse["@attributes"]["description"];

            detail.BaseText = !String.IsNullOrEmpty(
                (string)webcamtoparse["@attributes"]["longdescription"]
            )
                ? (string)webcamtoparse["@attributes"]["longdescription"]
                : (string)webcamtoparse["@attributes"]["description"];
            detail.Language = defaultlanguage;

            //TODO add urlconfig in Detail Object
            detail.AdditionalText =
                webcamtoparse["urlConfig"] != null
                    ? (string)webcamtoparse["urlConfig"]["example"]
                    : null;
            detail.AuthorTip =
                webcamtoparse["urlConfig"] != null
                    ? (string)webcamtoparse["urlConfig"]["@attributes"]["description"]
                    : null;

            webcam.Detail.TryAddOrUpdate(detail.Language, detail);

            //ContactInfos
            ContactInfos contactinfo = new ContactInfos();
            contactinfo.CompanyName = !String.IsNullOrEmpty(
                (string)webcamtoparse["@attributes"]["pageTitle"]
            )
                ? (string)webcamtoparse["@attributes"]["pageTitle"]
                : (string)webcamtoparse["@attributes"]["name"];
            contactinfo.CountryCode =
                (string)webcamtoparse["@attributes"]["addressIso"] != null
                    ? ((string)webcamtoparse["@attributes"]["addressIso"]).ToUpper()
                    : "";
            contactinfo.CountryName = (string)contactinfo.CountryCode == "IT" ? "Italien" : "";
            contactinfo.ZipCode = (string)webcamtoparse["@attributes"]["addressZip"];
            contactinfo.Address = (string)webcamtoparse["@attributes"]["addressStreet"];
            contactinfo.City = (string)webcamtoparse["@attributes"]["geoPlacename"];
            contactinfo.Region = (string)webcamtoparse["@attributes"]["geoRegion"];
            contactinfo.Language = defaultlanguage;

            if (webcamtoparse["Logos"] != null)
            {
                if (webcamtoparse["Logos"]["0"] != null)
                {
                    contactinfo.LogoUrl = (
                        (string)webcamtoparse["Logos"]["0"]["@attributes"]["logoUrl"]
                    ).AddHttpsPrefixIfNotPresent();
                }
                else if (webcamtoparse["Logos"]["Logo"] != null)
                {
                    //Take the first logo
                    foreach (var logo in ToList(webcamtoparse.Logos.Logo))
                    {
                        contactinfo.LogoUrl = (
                            (string)logo["@attributes"]["logoUrl"]
                        ).AddHttpsPrefixIfNotPresent();
                        break;
                    }
                }
            }

            webcam.ContactInfos.TryAddOrUpdate(contactinfo.Language, contactinfo);

            //GPSInfo
            GpsInfo gpsinfo = new GpsInfo();
            gpsinfo.Gpstype = "position";
            gpsinfo.Latitude =
                webcamtoparse["@attributes"]["geoLat"] != null
                    ? Convert.ToDouble(webcamtoparse["@attributes"]["geoLat"])
                    : 0;
            gpsinfo.Longitude =
                webcamtoparse["@attributes"]["geoLong"] != null
                    ? Convert.ToDouble(webcamtoparse["@attributes"]["geoLong"])
                    : 0;
            gpsinfo.Altitude =
                webcamtoparse["@attributes"]["geoAlt"] != null
                    ? Convert.ToDouble(webcamtoparse["@attributes"]["geoAlt"])
                    : 0;
            gpsinfo.AltitudeUnitofMeasure = "m";
            webcam.GpsInfo = new List<GpsInfo>() { gpsinfo };

            //Images
            webcam.ImageGallery = new List<ImageGallery>();
            if (webcamtoparse.Images != null)
            {
                foreach (var imagetoparse in ToList(webcamtoparse.Images.image))
                {
                    ImageGallery image = new ImageGallery();

                    image.Width = (int)imagetoparse["@attributes"]["imgWidth"];
                    image.Height = (int)imagetoparse["@attributes"]["imgHeight"];
                    image.ImageName = (string)imagetoparse["@attributes"]["fileName"];
                    image.ImageUrl = (
                        (string)imagetoparse["@attributes"]["fileUrl"]
                    ).AddHttpsPrefixIfNotPresent();
                    image.ImageSource = "panocloud";
                    image.IsInGallery = true;

                    if (image.ImageTags == null)
                        image.ImageTags = new List<string>();

                    image.ImageTags.Add((string)imagetoparse["@attributes"]["fileType"]);
                    image.ImageTags.Add((string)imagetoparse["@attributes"]["mimeType"]);

                    if ((string)imagetoparse["@attributes"]["fileType"] == "thumbnail")
                        image.ListPosition = 0;

                    //"panorama": "yes",
                    if ((string)imagetoparse["@attributes"]["panorama"] == "yes")
                    {
                        image.ImageTags.Add("panorama");
                    }

                    //"fileType": "big",
                    if (!String.IsNullOrEmpty((string)imagetoparse["@attributes"]["fileType"]))
                    {
                        if (image.ImageTags == null)
                            image.ImageTags = new List<string>();

                        if (
                            !image.ImageTags.Contains(
                                (string)imagetoparse["@attributes"]["fileType"]
                            )
                        )
                            image.ImageTags.Add((string)imagetoparse["@attributes"]["fileType"]);
                    }

                    //"fileTimeUnix": "1691568228",
                    //"fileTime": "2023-08-09T08:03:48+00:00"
                    //"mimeType": "image/jpeg",

                    webcam.ImageGallery.Add(image);
                }

                webcam.ImageGallery = webcam
                    .ImageGallery.OrderByDescending(x => x.ListPosition)
                    .ToList();
            }

            //Videos
            //"duration": "120",
            //"frameRate": "25.05",
            //"bitRate": "1678000",
            //"videoBitRate": "1677000",
            //"videoCodec": "",
            //"definition": "HD",
            //"resolution": "720",
            //"fileTimeUnix": "1691564828",
            //"fileTime": "2023-08-09T07:07:08+00:00",
            //"videoPlayerUrl": "alpenrose-haidersee.panocloud.webcam/wmsclipplayer.php"

            webcam.VideoItems = new Dictionary<string, ICollection<VideoItems>>();

            //Only one videos assigned
            if (webcamtoparse["Videos"] != null)
            {
                if (webcamtoparse["Videos"]["0"] != null)
                {
                    VideoItems videoitem = new VideoItems();
                    videoitem.Url = (
                        (string)webcamtoparse["Videos"]["0"]["@attributes"]["videoClipUrl"]
                    ).AddHttpsPrefixIfNotPresent();

                    //TODO videoPlayerUrl ex. "videoPlayerUrl": "abcdefg-wms.com/wmsclipplayer.php"

                    videoitem.StreamingSource = "panocloud";
                    videoitem.Active = true;

                    videoitem.Resolution = (int)
                        webcamtoparse["Videos"]["0"]["@attributes"]["resolution"];
                    videoitem.Definition = (string)
                        webcamtoparse["Videos"]["0"]["@attributes"]["definition"];
                    videoitem.Bitrate = (int)
                        webcamtoparse["Videos"]["0"]["@attributes"]["videoBitRate"];
                    videoitem.Duration = (double)
                        webcamtoparse["Videos"]["0"]["@attributes"]["duration"];

                    //"mimeType": "video/mp4",
                    videoitem.VideoType = (string)
                        webcamtoparse["Videos"]["0"]["@attributes"]["mimeType"];

                    webcam.VideoItems.TryAddOrUpdate(
                        defaultlanguage,
                        new List<VideoItems>() { videoitem }
                    );
                }
                else
                {
                    var videoitemslist = new List<VideoItems>();

                    foreach (var videotoparse in ToList(webcamtoparse.Videos.video))
                    {
                        VideoItems videoitem = new VideoItems();
                        //"videoClipUrl": "alpenrose-haidersee.panocloud.webcam/clip_current_720p.mp4",
                        videoitem.Url = (string)videotoparse["@attributes"]["videoClipUrl"];
                        videoitem.StreamingSource = "panocloud";
                        videoitem.Active = true;

                        videoitem.Resolution = (int)videotoparse["@attributes"]["resolution"];
                        videoitem.Definition = (string)videotoparse["@attributes"]["definition"];
                        videoitem.Bitrate = (int)videotoparse["@attributes"]["videoBitRate"];
                        videoitem.Duration = (double)videotoparse["@attributes"]["duration"];

                        //"mimeType": "video/mp4",
                        videoitem.VideoType = (string)videotoparse["@attributes"]["mimeType"];

                        videoitemslist.Add(videoitem);
                    }
                    webcam.VideoItems.TryAddOrUpdate(defaultlanguage, videoitemslist);
                }
            }

            //TODO digitalSignage

            //TODO WeatherData


            //Mapping
            webcam.Mapping.TryAddOrUpdate(
                "panocloud",
                new Dictionary<string, string>()
                {
                    { "locationId", (string)webcamtoparse["@attributes"]["locationId"] },
                }
            );

            //HasLanguage
            webcam.HasLanguage = new List<string>() { defaultlanguage };

            return webcam;
        }
    }
}
