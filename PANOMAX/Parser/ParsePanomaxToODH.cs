// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using DataModel;
using Helper;
using Newtonsoft.Json;

namespace PANOMAX
{
    public class ParsePanomaxToODH
    {
        public static WebcamInfoLinked ParseWebcamToWebcamInfo(
            WebcamInfoLinked webcam,
            dynamic webcamtoparse,
            string odhid
        )
        {
            if (webcam == null)
                webcam = new WebcamInfoLinked();

            webcam.Source = "panomax";

            webcam.Id = odhid;
            webcam.Active = true;

            webcam.SmgActive = webcam.Active;

            webcam.WebcamId = webcamtoparse.camId;

            //Detail
            Detail detail = new Detail();
            detail.Title = webcamtoparse.name;
            detail.Language = "en";
            webcam.Detail.TryAddOrUpdate(detail.Language, detail);

            //ContactInfos
            ContactInfos contactinfo = new ContactInfos();
            contactinfo.CompanyName = webcamtoparse.customerName;
            contactinfo.LogoUrl = webcamtoparse.logo;
            contactinfo.CountryCode =
                webcamtoparse.country != null ? ((string)webcamtoparse.country).ToUpper() : "";
            contactinfo.CountryName = webcamtoparse.countryName;
            contactinfo.City = webcamtoparse.city;
            contactinfo.Region = webcamtoparse.state;
            contactinfo.Url = webcamtoparse.customerUrl;
            contactinfo.Area = webcamtoparse.area;
            contactinfo.Language = "en";

            webcam.ContactInfos.TryAddOrUpdate(contactinfo.Language, contactinfo);

            //GPS
            GpsInfo gpsinfo = new GpsInfo();
            gpsinfo.Gpstype = "position";
            gpsinfo.Latitude =
                webcamtoparse.latitude != null ? Convert.ToDouble(webcamtoparse.latitude) : 0;
            gpsinfo.Longitude =
                webcamtoparse.longitude != null ? Convert.ToDouble(webcamtoparse.longitude) : 0;
            gpsinfo.Altitude =
                webcamtoparse.elevation != null ? Convert.ToDouble(webcamtoparse.elevation) : 0;
            gpsinfo.AltitudeUnitofMeasure = "m";
            webcam.GpsInfo = new List<GpsInfo>() { gpsinfo };

            //WebcamProperties
            WebcamProperties webcamproperties = new WebcamProperties();
            webcamproperties.WebcamUrl = webcamtoparse.webcamUrl;
            webcamproperties.ViewAngleDegree = webcamtoparse.viewAngleDegree;
            webcamproperties.ZeroDirection = webcamtoparse.zeroDirection;
            webcamproperties.HtmlEmbed = webcamtoparse.htmlEmbed;
            webcamproperties.TourCam =
                webcamtoparse.tourCam != null ? (bool)webcamtoparse.tourCam : false;

            webcam.WebCamProperties = webcamproperties;

            webcam.ImageGallery = new List<ImageGallery>();
            //ImageGallery
            foreach (var imagetoparse in webcamtoparse.images)
            {
                ImageGallery imagetoadd = new ImageGallery();
                imagetoadd.ImageSource = "panomax";
                imagetoadd.ImageUrl = imagetoparse.url;
                imagetoadd.Width = imagetoparse.width;
                imagetoadd.Height = imagetoparse.height;

                if (imagetoadd.ImageUrl.Contains("thumb"))
                {
                    imagetoadd.ListPosition = 0;

                    imagetoadd.ImageTags = new List<string>() { "thumbnail" };
                }

                if (imagetoadd.ImageUrl.Contains("small"))
                {
                    imagetoadd.ListPosition = 0;
                }

                webcam.ImageGallery.Add(imagetoadd);
            }

            webcam.ImageGallery = webcam
                .ImageGallery.OrderByDescending(x => x.ListPosition)
                .ToList();

            //Mapping
            webcam.Mapping.TryAddOrUpdate(
                "panomax",
                new Dictionary<string, string>()
                {
                    { "id", (string)webcamtoparse.id },
                    { "camId", (string)webcamtoparse.camId },
                    { "customerId", (string)webcamtoparse.customerId },
                }
            );

            //HasLanguage
            webcam.HasLanguage = webcam.Detail.Select(x => x.Key).Distinct().ToList();

            return webcam;
        }

        public static IDictionary<string, ICollection<VideoItems>> ParseVideosToVideoItems(
            IDictionary<string, ICollection<VideoItems>> videoitemsdict,
            dynamic videostoparse
        )
        {
            if (videostoparse != null)
            {
                foreach (var videotoparse in videostoparse.videos)
                {
                    if (videoitemsdict == null)
                        videoitemsdict = new Dictionary<string, ICollection<VideoItems>>();

                    var videoitemlist = new List<VideoItems>();

                    VideoItems videoitem = new VideoItems();
                    videoitem.Url = videotoparse.url;
                    videoitem.VideoTitle = videotoparse.fileName;
                    videoitem.Width = Convert.ToInt32(videotoparse.width);
                    videoitem.Height = Convert.ToInt32(videotoparse.height);
                    videoitem.VideoSource = "panomax";
                    videoitem.Active = true;
                    videoitem.Language = "en";

                    videoitemlist.Add(videoitem);

                    videoitemsdict.TryAddOrUpdate("en", videoitemlist);
                }
            }

            return videoitemsdict;
        }
    }
}
