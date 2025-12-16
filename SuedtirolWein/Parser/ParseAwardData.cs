// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DataModel;
using Helper;

namespace SuedtirolWein.Parser
{
    public class ParseAwardData
    {
        public static WineLinked ParsetheAwardData(
            WineLinked mywine,
            XElement myawardde,
            XElement myawardit,
            XElement myawarden,
            List<string> haslanguage
        )
        {
            mywine.LastChange = DateTime.Now;

            mywine.Id = myawardde.Element("id").Value;

            string titlede = myawardde.Element("title").Value;
            string winenamede = myawardde.Element("wine").Value;

            Detail mydetail = new Detail();
            mydetail.Title = titlede;
            mydetail.Language = "de";
            mydetail.Header = winenamede;

            mywine.Detail.TryAddOrUpdate("de", mydetail);

            if (haslanguage.Contains("it"))
            {
                string titleit = myawardit.Element("title").Value;
                string winenameit = myawardit.Element("wine").Value;

                Detail mydetailit = new Detail();
                mydetailit.Title = titleit;
                mydetailit.Language = "it";
                mydetailit.Header = winenameit;

                mywine.Detail.TryAddOrUpdate("it", mydetailit);
            }

            if (haslanguage.Contains("en"))
            {
                string titleen = myawarden.Element("title").Value;
                string winenameen = myawarden.Element("wine").Value;

                Detail mydetailen = new Detail();
                mydetailen.Title = titleen;
                mydetailen.Language = "en";
                mydetailen.Header = winenameen;

                mywine.Detail.TryAddOrUpdate("en", mydetailen);
            }

            mywine.Vintage = !String.IsNullOrEmpty(myawardde.Element("vintage").Value)
                ? Convert.ToInt32(myawardde.Element("vintage").Value)
                : 0;
            mywine.Awardyear = !String.IsNullOrEmpty(myawardde.Element("awardyear").Value)
                ? Convert.ToInt32(myawardde.Element("awardyear").Value)
                : 0;

            mywine.CompanyId = myawardde.Element("companyid").Value;
            mywine.Awards = myawardde.Element("awards").Value.Split(',').ToList();

            mywine.CustomId = myawardde.Element("wineid").Value;

            mywine.Shortname = myawardde.Element("title").Value;

            if (!String.IsNullOrEmpty(myawardde.Element("media").Value))
            {
                List<ImageGallery> myimglist = new List<ImageGallery>();

                if (mywine.ImageGallery != null)
                {
                    if (mywine.ImageGallery.Count > 0)
                    {
                        myimglist.AddRange(mywine.ImageGallery.Where(x => x.ImageSource == "SMG"));
                    }
                }

                ImageGallery myimage = new ImageGallery();
                myimage.ImageName = myawardde.Element("title").Value;
                myimage.IsInGallery = true;
                myimage.ListPosition = 0;
                myimage.ImageSource = "SuedtirolWein";
                myimage.ImageUrl = myawardde.Element("media").Value;

                if (myimage.ImageUrl.StartsWith("https://www.suedtirolwein.secure.consisto.net/"))
                {
                    myimage.ImageUrl.Replace(
                        "https://www.suedtirolwein.secure.consisto.net/",
                        "https://intranet.suedtirolwein.com/"
                    );
                }

                myimage.Height = 0;
                myimage.Width = 0;
                myimage.License = "";
                myimage.LicenseHolder = "https://www.suedtirolwein.com/";
                myimage.CopyRight = "Suedtirol Wein";

                myimglist.Add(myimage);

                mywine.ImageGallery = myimglist.ToList();

                mywine.HasLanguage = haslanguage;
            }

            return mywine;
        }
    }
}
