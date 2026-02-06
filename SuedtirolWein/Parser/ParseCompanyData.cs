// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using DataModel;
using Helper;

namespace SuedtirolWein.Parser
{
    public class ParseCompanyData
    {
        private static void ParseContactInfo(
            string language,
            XElement companydata,
            ODHActivityPoiLinked mywinecompany
        )
        {
            ContactInfos contactinfo = new ContactInfos();

            contactinfo.CompanyName = companydata.Element("title").Value;
            contactinfo.Address =
                companydata.Element("address") != null
                    ? companydata.Element("address").Value
                    : null;
            contactinfo.ZipCode =
                companydata.Element("zipcode") != null
                    ? companydata.Element("zipcode").Value
                    : null;
            contactinfo.City =
                companydata.Element("place") != null ? companydata.Element("place").Value : null;

            string countryname = "Italy";
            if (language == "de")
                countryname = "Italien";
            if (language == "it")
                countryname = "Italia";

            contactinfo.CountryName = countryname;
            contactinfo.CountryCode = "IT";
            contactinfo.Phonenumber =
                companydata.Element("phone") != null ? companydata.Element("phone").Value : null;

            string webadresseIT = "";
            //Webadress gschicht
            if (companydata.Element("homepage") != null)
            {
                webadresseIT = companydata.Element("homepage").Value.Contains("http")
                    ? companydata.Element("homepage").Value
                    : "http://" + companydata.Element("homepage").Value;
            }

            contactinfo.Url = webadresseIT;
            contactinfo.Email =
                companydata.Element("email") != null ? companydata.Element("email").Value : null;
            contactinfo.LogoUrl =
                companydata.Element("logo") != null ? companydata.Element("logo").Value : null;
            contactinfo.Language = language;

            mywinecompany.ContactInfos.TryAddOrUpdate(language, contactinfo);
        }

        private static void ParseDetailInfo(
            string language,
            XElement companydata,
            ODHActivityPoiLinked mywinecompany
        )
        {
            //Detail IT
            Detail mydetail = new Detail();

            if (mywinecompany.Detail != null)
                if (mywinecompany.Detail.ContainsKey(language))
                    mydetail = mywinecompany.Detail[language];

            mydetail.Title = companydata.Element("title").Value;

            mydetail.BaseText =
                companydata.Element("companydescription") != null
                    ? companydata.Element("companydescription").Value
                    : null;

            mydetail.Header =
                companydata.Element("slogan") != null ? companydata.Element("slogan").Value : null;
            mydetail.SubHeader =
                companydata.Element("subtitle") != null
                    ? companydata.Element("subtitle").Value
                    : null;
            mydetail.IntroText =
                companydata.Element("quote") != null ? companydata.Element("quote").Value : null;

            mydetail.Language = language;
            mywinecompany.Detail.TryAddOrUpdate(language, mydetail);
        }

        private static void ParseImageGalleryData(
            IDictionary<string, XElement> companydata,
            ODHActivityPoiLinked mywinecompany
        )
        {
            //ImageGallery
            List<ImageGallery> myimagegallerylist = new List<ImageGallery>();

            string imageurlmedia = "";

            if (companydata["de"].Element("media") != null)
            {
                ImageGallery myimagegallery = new ImageGallery();
                myimagegallery.ImageUrl = companydata["de"].Element("media").Value;
                myimagegallery.ImageSource = "SuedtirolWein";
                myimagegallery.IsInGallery = true;
                myimagegallery.ListPosition = 0;
                myimagegallery.CopyRight = "Suedtirol Wein";
                myimagegallery.License = "";
                myimagegallery.LicenseHolder = "https://www.suedtirolwein.com/";

                //Image Title
                if (companydata["de"].Element("imagemetatitle") != null)
                    myimagegallery.ImageTitle.TryAddOrUpdate(
                        "de",
                        companydata["de"].Element("imagemetatitle").Value
                    );
                //Image Description
                if (companydata["de"].Element("imagemetadescription") != null)
                    myimagegallery.ImageDesc.TryAddOrUpdate(
                        "de",
                        companydata["de"].Element("imagemetadescription").Value
                    );
                //Image Alttext
                if (companydata["de"].Element("imagemetaalt") != null)
                    myimagegallery.ImageAltText.TryAddOrUpdate(
                        "de",
                        companydata["de"].Element("imagemetaalt").Value
                    );

                imageurlmedia = myimagegallery.ImageUrl;

                myimagegallerylist.Add(myimagegallery);
            }
            if (companydata["it"] != null && companydata["it"].Element("media") != null)
            {
                if (myimagegallerylist.Count == 1)
                {
                    if (companydata["it"].Element("imagemetatitle") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageTitle.TryAddOrUpdate(
                                "it",
                                companydata["it"].Element("imagemetatitle").Value
                            );
                    if (companydata["it"].Element("imagemetadescription") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageDesc.TryAddOrUpdate(
                                "it",
                                companydata["it"].Element("imagemetadescription").Value
                            );
                    if (companydata["it"].Element("imagemetaalt") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageAltText.TryAddOrUpdate(
                                "it",
                                companydata["it"].Element("imagemetaalt").Value
                            );
                }
            }
            if (companydata["en"] != null && companydata["en"].Element("media") != null)
            {
                if (myimagegallerylist.Count == 1)
                {
                    if (companydata["en"].Element("imagemetatitle") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageTitle.TryAddOrUpdate(
                                "en",
                                companydata["en"].Element("imagemetatitle").Value
                            );
                    if (companydata["en"].Element("imagemetadescription") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageDesc.TryAddOrUpdate(
                                "en",
                                companydata["en"].Element("imagemetadescription").Value
                            );
                    if (companydata["en"].Element("imagemetaalt") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageAltText.TryAddOrUpdate(
                                "en",
                                companydata["en"].Element("imagemetaalt").Value
                            );
                }
            }
            if (companydata["ru"] != null && companydata["ru"].Element("media") != null)
            {
                if (myimagegallerylist.Count == 1)
                {
                    if (companydata["ru"].Element("imagemetatitle") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageTitle.TryAddOrUpdate(
                                "ru",
                                companydata["ru"].Element("imagemetatitle").Value
                            );
                    if (companydata["ru"].Element("imagemetadescription") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageDesc.TryAddOrUpdate(
                                "ru",
                                companydata["ru"].Element("imagemetadescription").Value
                            );
                    if (companydata["ru"].Element("imagemetaalt") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageAltText.TryAddOrUpdate(
                                "ru",
                                companydata["ru"].Element("imagemetaalt").Value
                            );
                }
            }
            if (companydata["jp"] != null && companydata["jp"].Element("media") != null)
            {
                if (myimagegallerylist.Count == 1)
                {
                    if (companydata["jp"].Element("imagemetatitle") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageTitle.TryAddOrUpdate(
                                "jp",
                                companydata["jp"].Element("imagemetatitle").Value
                            );
                    if (companydata["jp"].Element("imagemetadescription") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageDesc.TryAddOrUpdate(
                                "jp",
                                companydata["jp"].Element("imagemetadescription").Value
                            );
                    if (companydata["jp"].Element("imagemetaalt") != null)
                        myimagegallerylist
                            .FirstOrDefault()
                            .ImageAltText.TryAddOrUpdate(
                                "jp",
                                companydata["jp"].Element("imagemetaalt").Value
                            );
                }
            }

            //Fix add mediadetail only if url differs from media

            if (companydata["de"].Element("mediadetail") != null)
            {
                ImageGallery myimagegallery2 = new ImageGallery();
                myimagegallery2.ImageUrl = companydata["de"].Element("mediadetail").Value;
                myimagegallery2.ImageSource = "SuedtirolWein";
                myimagegallery2.IsInGallery = true;
                myimagegallery2.ListPosition = 1;
                myimagegallery2.CopyRight = "Suedtirol Wein";
                myimagegallery2.License = "";
                myimagegallery2.LicenseHolder = "https://www.suedtirolwein.com/";

                if (imageurlmedia != myimagegallery2.ImageUrl)
                    myimagegallerylist.Add(myimagegallery2);
            }
            //if (companydata["de"].Element("logo") != null)
            //{
            //    ImageGallery myimagegallery3 = new ImageGallery();
            //    myimagegallery3.ImageUrl = companydata["de"].Element("logo").Value;
            //    myimagegallery3.ImageSource = "SuedtirolWein";
            //    myimagegallery3.IsInGallery = true;
            //    myimagegallery3.ListPosition = 2;
            //    myimagegallery3.CopyRight = "Suedtirol Wein";
            //    myimagegallery3.License = "";
            //    myimagegallery3.LicenseHolder = "https://www.suedtirolwein.com/";

            //    if (imageurlmedia != myimagegallery3.ImageUrl)
            //        myimagegallerylist.Add(myimagegallery3);
            //}

            mywinecompany.ImageGallery = myimagegallerylist;
        }

        private static void ParsePropertyData(
            string language,
            XElement companydata,
            ODHActivityPoiLinked mywinecompany
        )
        {
            //PROPS DE
            List<PoiProperty> mypropertylist = new List<PoiProperty>();
            if (companydata.Element("openingtimeswineshop") != null)
            {
                PoiProperty mypropopeningtimeswineshop = new PoiProperty();
                mypropopeningtimeswineshop.Name = "openingtimeswineshop";
                mypropopeningtimeswineshop.Value = companydata
                    .Element("openingtimeswineshop")
                    .Value;
                mypropertylist.Add(mypropopeningtimeswineshop);
            }
            if (companydata.Element("openingtimesguides") != null)
            {
                PoiProperty mypropopeningtimesguides = new PoiProperty();
                mypropopeningtimesguides.Name = "openingtimesguides";
                mypropopeningtimesguides.Value = companydata.Element("openingtimesguides").Value;
                mypropertylist.Add(mypropopeningtimesguides);
            }
            if (companydata.Element("openingtimesgastronomie") != null)
            {
                PoiProperty mypropopeningtimesgastronomie = new PoiProperty();
                mypropopeningtimesgastronomie.Name = "openingtimesgastronomie";
                mypropopeningtimesgastronomie.Value = companydata
                    .Element("openingtimesgastronomie")
                    .Value;
                mypropertylist.Add(mypropopeningtimesgastronomie);
            }
            if (companydata.Element("companyholiday") != null)
            {
                PoiProperty mypropcompanyholiday = new PoiProperty();
                mypropcompanyholiday.Name = "companyholiday";
                mypropcompanyholiday.Value = companydata.Element("companyholiday").Value;
                mypropertylist.Add(mypropcompanyholiday);
            }

            if (companydata.Element("hasvisits") != null)
            {
                PoiProperty myprophasvisits = new PoiProperty();
                myprophasvisits.Name = "hasvisits";

                myprophasvisits.Value = companydata.Element("hasvisits").Value;
                mypropertylist.Add(myprophasvisits);
            }
            if (companydata.Element("hasovernights") != null)
            {
                PoiProperty myprophasovernights = new PoiProperty();
                myprophasovernights.Name = "hasovernights";

                myprophasovernights.Value = companydata.Element("hasovernights").Value;
                mypropertylist.Add(myprophasovernights);
            }
            if (companydata.Element("hasbiowine") != null)
            {
                PoiProperty myprophasbiowine = new PoiProperty();
                myprophasbiowine.Name = "hasbiowine";

                myprophasbiowine.Value = companydata.Element("hasbiowine").Value;
                mypropertylist.Add(myprophasbiowine);
            }
            if (companydata.Element("wines") != null)
            {
                PoiProperty mypropwines = new PoiProperty();
                mypropwines.Name = "wines";
                mypropwines.Value = companydata.Element("wines").Value;
                mypropertylist.Add(mypropwines);
            }
            if (companydata.Element("hasaccomodation") != null)
            {
                PoiProperty myprophasaccomodation = new PoiProperty();
                myprophasaccomodation.Name = "hasaccomodation";

                myprophasaccomodation.Value = companydata.Element("hasaccomodation").Value;
                mypropertylist.Add(myprophasaccomodation);
            }
            if (companydata.Element("isvinumhotel") != null)
            {
                PoiProperty mypropisvinumhotel = new PoiProperty();
                mypropisvinumhotel.Name = "isvinumhotel";

                mypropisvinumhotel.Value = companydata.Element("isvinumhotel").Value;
                mypropertylist.Add(mypropisvinumhotel);
            }
            if (companydata.Element("isanteprima") != null)
            {
                PoiProperty mypropisanteprima = new PoiProperty();
                mypropisanteprima.Name = "isanteprima";

                mypropisanteprima.Value = companydata.Element("isanteprima").Value;
                mypropertylist.Add(mypropisanteprima);
            }
            if (companydata.Element("iswinestories") != null)
            {
                PoiProperty mypropiswinestories = new PoiProperty();
                mypropiswinestories.Name = "iswinestories";

                mypropiswinestories.Value = companydata.Element("iswinestories").Value;
                mypropertylist.Add(mypropiswinestories);
            }
            if (companydata.Element("iswinesummit") != null)
            {
                PoiProperty mypropiswinesummit = new PoiProperty();
                mypropiswinesummit.Name = "iswinesummit";

                mypropiswinesummit.Value = companydata.Element("iswinesummit").Value;
                mypropertylist.Add(mypropiswinesummit);
            }
            if (companydata.Element("issparklingwineassociation") != null)
            {
                PoiProperty mypropissparklingwineassociation = new PoiProperty();
                mypropissparklingwineassociation.Name = "issparklingwineassociation";

                mypropissparklingwineassociation.Value = companydata
                    .Element("issparklingwineassociation")
                    .Value;
                mypropertylist.Add(mypropissparklingwineassociation);
            }
            if (companydata.Element("iswinery") != null)
            {
                PoiProperty mypropiswinery = new PoiProperty();
                mypropiswinery.Name = "iswinery";

                mypropiswinery.Value = companydata.Element("iswinery").Value;
                mypropertylist.Add(mypropiswinery);
            }
            if (companydata.Element("iswineryassociation") != null)
            {
                PoiProperty mypropiswineryassociation = new PoiProperty();
                mypropiswineryassociation.Name = "iswineryassociation";

                mypropiswineryassociation.Value = companydata.Element("iswineryassociation").Value;
                mypropertylist.Add(mypropiswineryassociation);
            }

            //new hasonlineshop
            if (companydata.Element("hasonlineshop") != null)
            {
                PoiProperty myprophasonlineshop = new PoiProperty();
                myprophasonlineshop.Name = "hasonlineshop";

                myprophasonlineshop.Value = companydata.Element("hasonlineshop").Value;
                mypropertylist.Add(myprophasonlineshop);
            }

            //new hasdeliveryservice
            if (companydata.Element("hasdeliveryservice") != null)
            {
                PoiProperty myprophasdeliveryservice = new PoiProperty();
                myprophasdeliveryservice.Name = "hasdeliveryservice";

                myprophasdeliveryservice.Value = companydata.Element("hasdeliveryservice").Value;
                mypropertylist.Add(myprophasdeliveryservice);
            }

            //new onlineshopurl
            if (companydata.Element("onlineshopurl") != null)
            {
                PoiProperty myproponlineshopurl = new PoiProperty();
                myproponlineshopurl.Name = "onlineshopurl";

                myproponlineshopurl.Value = companydata.Element("onlineshopurl").Value;
                mypropertylist.Add(myproponlineshopurl);
            }
            // new deliveryserviceurl
            if (companydata.Element("deliveryserviceurl") != null)
            {
                PoiProperty mypropdeliveryserviceurl = new PoiProperty();
                mypropdeliveryserviceurl.Name = "deliveryserviceurl";

                mypropdeliveryserviceurl.Value = companydata.Element("deliveryserviceurl").Value;
                mypropertylist.Add(mypropdeliveryserviceurl);
            }
            //new h1
            if (companydata.Element("h1") != null)
            {
                PoiProperty myproph1 = new PoiProperty();
                myproph1.Name = "h1";

                myproph1.Value = companydata.Element("h1").Value;
                mypropertylist.Add(myproph1);
            }
            //new h2
            if (companydata.Element("h2") != null)
            {
                PoiProperty myproph2 = new PoiProperty();
                myproph2.Name = "h2";

                myproph2.Value = companydata.Element("h2").Value;
                mypropertylist.Add(myproph2);
            }

            //new quote
            if (companydata.Element("quote") != null)
            {
                PoiProperty mypropquote = new PoiProperty();
                mypropquote.Name = "quote";

                mypropquote.Value = companydata.Element("quote").Value;
                mypropertylist.Add(mypropquote);
            }

            //new quote author
            if (companydata.Element("quoteauthor") != null)
            {
                PoiProperty mypropquoteauthor = new PoiProperty();
                mypropquoteauthor.Name = "quoteauthor";

                mypropquoteauthor.Value = companydata.Element("quoteauthor").Value;
                mypropertylist.Add(mypropquoteauthor);
            }

            //new descriptionsparklingwineproducer
            if (companydata.Element("descriptionsparklingwineproducer") != null)
            {
                PoiProperty mypropdescriptionsparklingwineproducer = new PoiProperty();
                mypropdescriptionsparklingwineproducer.Name = "descriptionsparklingwineproducer";

                mypropdescriptionsparklingwineproducer.Value = companydata
                    .Element("descriptionsparklingwineproducer")
                    .Value;
                mypropertylist.Add(mypropdescriptionsparklingwineproducer);
            }

            //new h1sparklingwineproducer
            if (companydata.Element("h1sparklingwineproducer") != null)
            {
                PoiProperty myproph1sparklingwineproducer = new PoiProperty();
                myproph1sparklingwineproducer.Name = "h1sparklingwineproducer";

                myproph1sparklingwineproducer.Value = companydata
                    .Element("h1sparklingwineproducer")
                    .Value;
                mypropertylist.Add(myproph1sparklingwineproducer);
            }

            //new h2sparklingwineproducer
            if (companydata.Element("h2sparklingwineproducer") != null)
            {
                PoiProperty myproph2sparklingwineproducer = new PoiProperty();
                myproph2sparklingwineproducer.Name = "h2sparklingwineproducer";

                myproph2sparklingwineproducer.Value = companydata
                    .Element("h2sparklingwineproducer")
                    .Value;
                mypropertylist.Add(myproph2sparklingwineproducer);
            }

            //new <imagesparklingwineproducer>
            if (companydata.Element("imagesparklingwineproducer") != null)
            {
                PoiProperty mypropimagesparklingwineproducer = new PoiProperty();
                mypropimagesparklingwineproducer.Name = "imagesparklingwineproducer";

                mypropimagesparklingwineproducer.Value = companydata
                    .Element("imagesparklingwineproducer")
                    .Value;
                mypropertylist.Add(mypropimagesparklingwineproducer);
            }

            //new <hasdirectsales>
            if (companydata.Element("hasdirectsales") != null)
            {
                PoiProperty myprophasdirectsales = new PoiProperty();
                myprophasdirectsales.Name = "hasdirectsales";

                myprophasdirectsales.Value = companydata.Element("hasdirectsales").Value;
                mypropertylist.Add(myprophasdirectsales);
            }

            //isskyalpspartner
            if (companydata.Element("isskyalpspartner") != null)
            {
                PoiProperty mypropisskyalpspartner = new PoiProperty();
                mypropisskyalpspartner.Name = "isskyalpspartner";

                mypropisskyalpspartner.Value = companydata.Element("isskyalpspartner").Value;
                mypropertylist.Add(mypropisskyalpspartner);
            }

            //socialsinstagram
            if (companydata.Element("socialsinstagram") != null)
            {
                PoiProperty mypropsocialsinstagram = new PoiProperty();
                mypropsocialsinstagram.Name = "socialsinstagram";

                mypropsocialsinstagram.Value = companydata.Element("socialsinstagram").Value;
                mypropertylist.Add(mypropsocialsinstagram);
            }

            //socialsfacebook
            if (companydata.Element("socialsfacebook") != null)
            {
                PoiProperty mypropisskyalpspartner = new PoiProperty();
                mypropisskyalpspartner.Name = "socialsfacebook";

                mypropisskyalpspartner.Value = companydata.Element("socialsfacebook").Value;
                mypropertylist.Add(mypropisskyalpspartner);
            }

            //socialslinkedIn
            if (companydata.Element("socialslinkedIn") != null)
            {
                PoiProperty mypropsocialslinkedIn = new PoiProperty();
                mypropsocialslinkedIn.Name = "socialslinkedIn";

                mypropsocialslinkedIn.Value = companydata.Element("socialslinkedIn").Value;
                mypropertylist.Add(mypropsocialslinkedIn);
            }

            //socialspinterest
            if (companydata.Element("socialspinterest") != null)
            {
                PoiProperty mypropsocialspinterest = new PoiProperty();
                mypropsocialspinterest.Name = "socialspinterest";

                mypropsocialspinterest.Value = companydata.Element("socialspinterest").Value;
                mypropertylist.Add(mypropsocialspinterest);
            }

            //socialstiktok
            if (companydata.Element("socialstiktok") != null)
            {
                PoiProperty mypropsocialstiktok = new PoiProperty();
                mypropsocialstiktok.Name = "socialstiktok";

                mypropsocialstiktok.Value = companydata.Element("socialstiktok").Value;
                mypropertylist.Add(mypropsocialstiktok);
            }

            //socialsyoutube
            if (companydata.Element("socialsyoutube") != null)
            {
                PoiProperty mypropsocialsyoutube = new PoiProperty();
                mypropsocialsyoutube.Name = "socialsyoutube";

                mypropsocialsyoutube.Value = companydata.Element("socialsyoutube").Value;
                mypropertylist.Add(mypropsocialsyoutube);
            }

            //socialstwitter
            if (companydata.Element("socialstwitter") != null)
            {
                PoiProperty mypropsocialstwitter = new PoiProperty();
                mypropsocialstwitter.Name = "socialstwitter";

                mypropsocialstwitter.Value = companydata.Element("socialstwitter").Value;
                mypropertylist.Add(mypropsocialstwitter);
            }

            mywinecompany.PoiProperty.TryAddOrUpdate(language, mypropertylist);
        }

        private static void ParseImporterData(
            string language,
            XElement companydata,
            ODHActivityPoiLinked mywinecompany
        )
        {
            List<AdditionalContact> importercontactInfos = new List<AdditionalContact>();

            if (companydata.Element("importers") != null)
            {
                if (companydata.Element("importers").Elements("importer") != null)
                {
                    foreach (
                        var companyimporterde in companydata
                            .Element("importers")
                            .Elements("importer")
                    )
                    {
                        if (companyimporterde.HasElements)
                        {
                            AdditionalContact myadditionalcontactde = new AdditionalContact();
                            myadditionalcontactde.Type = "wineimporter";

                            ContactInfos myimportercontactinfode = new ContactInfos();

                            if (companyimporterde.Element("importername") != null)
                                myimportercontactinfode.CompanyName = companyimporterde
                                    .Element("importername")
                                    .Value;
                            if (companyimporterde.Element("importeraddress") != null)
                                myimportercontactinfode.Address = companyimporterde
                                    .Element("importeraddress")
                                    .Value;
                            if (companyimporterde.Element("importerzipcode") != null)
                                myimportercontactinfode.ZipCode = companyimporterde
                                    .Element("importerzipcode")
                                    .Value;
                            if (companyimporterde.Element("importerplace") != null)
                                myimportercontactinfode.City = companyimporterde
                                    .Element("importerplace")
                                    .Value;
                            if (companyimporterde.Element("importerphone") != null)
                                myimportercontactinfode.Phonenumber = companyimporterde
                                    .Element("importerphone")
                                    .Value;
                            if (companyimporterde.Element("importeremail") != null)
                                myimportercontactinfode.Email = companyimporterde
                                    .Element("importeremail")
                                    .Value;
                            if (companyimporterde.Element("importerhomepage") != null)
                                myimportercontactinfode.Url = companyimporterde
                                    .Element("importerhomepage")
                                    .Value;
                            if (companyimporterde.Element("importercontactperson") != null)
                                myimportercontactinfode.Givenname = companyimporterde
                                    .Element("importercontactperson")
                                    .Value;
                            if (companyimporterde.Element("importerdescription") != null)
                                myadditionalcontactde.Description = companyimporterde
                                    .Element("importerdescription")
                                    .Value;

                            myimportercontactinfode.Language = language;

                            myadditionalcontactde.ContactInfos = myimportercontactinfode;

                            importercontactInfos.Add(myadditionalcontactde);
                        }
                    }
                }
            }
            if (importercontactInfos.Count > 0)
            {
                if (mywinecompany.AdditionalContact == null)
                    mywinecompany.AdditionalContact =
                        new Dictionary<string, List<AdditionalContact>>();

                mywinecompany.AdditionalContact.TryAddOrUpdate(language, importercontactInfos);
            }
        }

        public static ODHActivityPoiLinked ParsetheCompanyData(
            ODHActivityPoiLinked mywinecompany,
            IDictionary<string, XElement> companydata,
            List<string> haslanguage
        )
        {
            mywinecompany.LastChange = DateTime.Now;

            mywinecompany.Id = companydata["de"].Element("id").Value;
            mywinecompany.CustomId = companydata["de"].Element("id").Value;

            //ADD MAPPING
            var suedtirolweinid = new Dictionary<string, string>() { { "id", mywinecompany.Id } };
            mywinecompany.Mapping.TryAddOrUpdate("suedtirolwein", suedtirolweinid);

            mywinecompany.Source = "suedtirolwein";
            mywinecompany.SyncSourceInterface = "suedtirolweincompany";
            mywinecompany.SyncUpdateMode = "Full";

            //Deprecated Fields to null
            mywinecompany.Type = null;
            mywinecompany.SubType = null;
            mywinecompany.AgeFrom = null;
            mywinecompany.AgeTo = null;
            mywinecompany.SmgActive = false;
            mywinecompany.AreaId = null;
            //mywinecompany.ODHActivityPoiTypes = null;

            mywinecompany.Shortname = companydata["de"].Element("title").Value;

            //Contactinfo for all languages
            foreach (var language in haslanguage)
            {
                ParseContactInfo(language, companydata[language], mywinecompany);
            }

            //Detailinfo for all languages
            foreach (var language in haslanguage)
            {
                ParseDetailInfo(language, companydata[language], mywinecompany);
            }

            //Propertyinfo for all languages
            ParseImageGalleryData(companydata, mywinecompany);

            //Propertyinfo for all languages
            foreach (var language in haslanguage)
            {
                ParsePropertyData(language, companydata[language], mywinecompany);
            }

            //Wineids
            if (companydata["de"].Element("wineids") != null)
            {
                List<string> poiServices = new List<string>();
                var wineids = companydata["de"].Element("wines").Value.Split(',');
                poiServices = wineids.ToList();
                mywinecompany.PoiServices = poiServices;
            }

            if (
                companydata["de"].Element("latidude") != null
                && companydata["de"].Element("longitude") != null
                && companydata["de"].Element("latidude").Value != null
                && companydata["de"].Element("longitude").Value != null
            )
            {
                if (
                    !companydata["de"].Element("latidude").Value.Contains("°")
                    && !companydata["de"].Element("longitude").Value.Contains("°")
                )
                {
                    if (
                        companydata["de"].Element("latidude").Value != "0"
                        && companydata["de"].Element("longitude").Value != "0"
                    )
                    {
                        List<GpsInfo> gpsinfolist = new List<GpsInfo>();
                        GpsInfo mygps = new GpsInfo();
                        mygps.Latitude = Convert.ToDouble(
                            companydata["de"].Element("latidude").Value.Replace(",","."),
                            CultureInfo.InvariantCulture
                        );
                        mygps.Longitude = Convert.ToDouble(
                            companydata["de"].Element("longitude").Value.Replace(",", "."),
                            CultureInfo.InvariantCulture
                        );
                        mygps.Gpstype = "position";
                        gpsinfolist.Add(mygps);
                        mywinecompany.GpsInfo = gpsinfolist;
                    }
                }
            }

            //WineImporters for all languages
            foreach (var language in haslanguage)
            {
                ParseImporterData(language, companydata[language], mywinecompany);
            }

            mywinecompany.HasLanguage = mywinecompany.Detail.Keys.ToList();

            return mywinecompany;
        }
    }
}
