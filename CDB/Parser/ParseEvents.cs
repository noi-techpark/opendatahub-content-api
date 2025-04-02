//// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
////
//// SPDX-License-Identifier: AGPL-3.0-or-later

//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using DataModel;
//using Helper;

//namespace CDB.Parser
//{
//    public class ParseEvents
//    {
//        public static CultureInfo myculture = new CultureInfo("en");

//        public static EventLinked GetEventData(
//            EventLinked myevent,
//            string eventRid,
//            string publRidList,
//            string ltsuser,
//            string ltspswd,
//            string serviceurl
//        )
//        {
//            var eventresponsede = GetEvents.GetEventDetail(
//                "de",
//                "SMG",
//                eventRid,
//                publRidList,
//                "1",
//                "1",
//                ltsuser,
//                ltspswd,
//                serviceurl
//            );
//            var eventresponseit = GetEvents.GetEventDetail(
//                "it",
//                "SMG",
//                eventRid,
//                publRidList,
//                "1",
//                "1",
//                ltsuser,
//                ltspswd,
//                serviceurl
//            );
//            var eventresponseen = GetEvents.GetEventDetail(
//                "en",
//                "SMG",
//                eventRid,
//                publRidList,
//                "1",
//                "1",
//                ltsuser,
//                ltspswd,
//                serviceurl
//            );

//            if (eventresponsede != null)
//            {
//                if (eventresponsede.HasElements)
//                {
//                    DateTime firstbegindateOLD = DateTime.MinValue;
//                    DateTime lastenddateOLD = DateTime.MaxValue;

//                    List<string> availablelangs = default(List<string>);

//                    if (myevent == null)
//                    {
//                        myevent = new EventLinked();
//                        myevent.SmgActive = false;
//                        myevent.FirstImport = DateTime.Now;

//                        availablelangs = new List<string>() { "de", "it", "en" };
//                    }
//                    else
//                    {
//                        availablelangs = new List<string>() { "de", "it", "en" };

//                        firstbegindateOLD = (DateTime)myevent.DateBegin;
//                        lastenddateOLD = (DateTime)myevent.DateEnd;
//                    }

//                    myevent.LastChange = DateTime.Now;

//                    myevent.Id = eventRid;
//                    //Set Event Languages to de,it,en
//                    myevent.HasLanguage = availablelangs;
//                    //fallback no name
//                    string shortname = "no name";

//                    myevent.Source = "LTS";

//                    //Add LTS Id as Mapping
//                    var ltsriddict = new Dictionary<string, string>()
//                    {
//                        { "rid", eventRid.ToUpper() },
//                    };
//                    myevent.Mapping.TryAddOrUpdate("lts", ltsriddict);

//                    if (eventresponsede.Element("Definition").Element("DefinitionLng") != null)
//                        shortname = eventresponsede
//                            .Element("Definition")
//                            .Element("DefinitionLng")
//                            .Attribute("Title")
//                            .Value;

//                    myevent.Shortname = shortname;

//                    var definitionde =
//                        eventresponsede != null
//                            ? eventresponsede.Elements("Definition").FirstOrDefault()
//                            : null;
//                    var definitionit =
//                        eventresponseit != null
//                            ? eventresponseit.Elements("Definition").FirstOrDefault()
//                            : null;
//                    var definitionen =
//                        eventresponseen != null
//                            ? eventresponseen.Elements("Definition").FirstOrDefault()
//                            : null;

//                    //Active LTS Property TODO active = 3?

//                    //0 = design
//                    //1 = active
//                    //2 = deleted(or inactive)
//                    //3 = out of time

//                    bool active = true;
//                    if (
//                        definitionde.Attribute("Active").Value == "0"
//                        || definitionde.Attribute("Active").Value == "2"
//                    )
//                        active = false;

//                    if (definitionde.Attribute("Active").Value == "3")
//                    {
//                        //for debugging
//                    }

//                    //Classificationrid
//                    if (definitionde.Attribute("ClassificationRID") != null)
//                        myevent.ClassificationRID = definitionde
//                            .Attribute("ClassificationRID")
//                            .Value;

//                    myevent.SmgActive = myevent.SmgActive;
//                    myevent.Active = active;

//                    Detail detailde = new Detail() { Language = "de" };
//                    Detail detailit = new Detail() { Language = "it" };
//                    Detail detailen = new Detail() { Language = "en" };

//                    List<string> hastaglistde = new List<string>();
//                    List<string> hastaglistit = new List<string>();
//                    List<string> hastaglisten = new List<string>();
//                    //Hashtag TODO
//                    //<Hashtag Title="zurückinsLeben" LngID = "DE" />

//                    if (eventresponsede != null)
//                    {
//                        var hashtagelements = eventresponsede.Elements("Hashtag");
//                        foreach (var hashtagelement in hashtagelements)
//                        {
//                            hastaglistde.Add(hashtagelement.Attribute("Title").Value);
//                        }
//                    }

//                    if (eventresponseit != null)
//                    {
//                        var hashtagelementsit = eventresponseit.Elements("Hashtag");
//                        foreach (var hashtagelementit in hashtagelementsit)
//                        {
//                            hastaglistit.Add(hashtagelementit.Attribute("Title").Value);
//                        }
//                    }

//                    if (eventresponseen != null)
//                    {
//                        var hashtagelementsen = eventresponseen.Elements("Hashtag");
//                        foreach (var hashtagelementen in hashtagelementsen)
//                        {
//                            hastaglisten.Add(hashtagelementen.Attribute("Title").Value);
//                        }
//                    }

//                    if (hastaglistde.Count > 0)
//                        myevent.Hashtag.TryAddOrUpdate("de", hastaglistde);
//                    if (hastaglistit.Count > 0)
//                        myevent.Hashtag.TryAddOrUpdate("it", hastaglistit);
//                    if (hastaglisten.Count > 0)
//                        myevent.Hashtag.TryAddOrUpdate("en", hastaglisten);

//                    //CrossSelling
//                    var crosselling = eventresponsede.Elements("CrossSelling");
//                    if (crosselling != null)
//                    {
//                        foreach (XElement crossellingel in crosselling.Elements("Event"))
//                        {
//                            if (crossellingel != null)
//                            {
//                                if (myevent.EventCrossSelling == null)
//                                    myevent.EventCrossSelling = new List<EventCrossSelling>();

//                                EventCrossSelling myeventcrosselling = new EventCrossSelling();
//                                myeventcrosselling.EventRID = crossellingel.Attribute("RID").Value;

//                                myevent.EventCrossSelling.Add(myeventcrosselling);
//                            }
//                        }
//                    }

//                    //Contactinfo
//                    if (definitionde != null)
//                    {
//                        myevent.GpsInfo = new List<GpsInfo>();
//                        GpsInfo eventgpsinfo = new GpsInfo();

//                        eventgpsinfo.Gpstype = "position";
//                        eventgpsinfo.Latitude = Double.Parse(
//                            definitionde.Attribute("GNP").Value,
//                            myculture
//                        );
//                        eventgpsinfo.Longitude = Double.Parse(
//                            definitionde.Attribute("GEP").Value,
//                            myculture
//                        );
//                        eventgpsinfo.AltitudeUnitofMeasure = "m";

//                        myevent.GpsInfo.Add(eventgpsinfo);

//                        myevent.SignOn =
//                            definitionde.Attribute("SignOn") != null
//                                ? definitionde.Attribute("SignOn").Value
//                                : "";

//                        //PayMet types
//                        //0 = free
//                        //1 = on site
//                        //2 = free choice
//                        //3 = at the moment of inscription
//                        myevent.PayMet =
//                            definitionde.Attribute("PayMet") != null
//                                ? definitionde.Attribute("PayMet").Value
//                                : "";

//                        //Deprecated?? REMOVE?
//                        //myevent.Ranc = definitionde.Attribute("Ranc") != null ? Convert.ToInt32(definitionde.Attribute("Ranc").Value) : 0;

//                        //PDF Attribute
//                        myevent.Pdf =
//                            definitionde.Attribute("PDF") != null
//                                ? definitionde.Attribute("PDF").Value
//                                : "";

//                        //Deprecated!
//                        //myevent.Type = definitionde.Attribute("Type") != null ? definitionde.Attribute("Type").Value : "";

//                        //New Fields
//                        myevent.Source = "LTS";

//                        //CHeck if boolean parsing is working
//                        Boolean.TryParse(
//                            definitionde.Attribute("GrpEvent").Value,
//                            out var grpevent
//                        );
//                        myevent.GrpEvent = grpevent;
//                        Boolean.TryParse(
//                            definitionde.Attribute("EventBenefit").Value,
//                            out var eventbenefit
//                        );
//                        myevent.EventBenefit = eventbenefit;

//                        myevent.Ticket = definitionde.Attribute("Ticket").Value;
//                        myevent.OrgRID = definitionde.Attribute("OrgRID").Value;

//                        ContactInfos mycontactinfode = new ContactInfos();
//                        mycontactinfode.Language = "de";

//                        EventAdditionalInfos eventadditionalinfode = new EventAdditionalInfos();
//                        eventadditionalinfode.Language = "de";

//                        //Language
//                        mycontactinfode.Phonenumber = definitionde.Attribute("Phone").Value;
//                        mycontactinfode.ZipCode = definitionde.Attribute("Zip").Value;
//                        //mycontactinfode.nat = definitionde.Attribute("NatID").Value;
//                        mycontactinfode.CountryCode = "IT";

//                        var deflngde = definitionde.Element("DefinitionLng");

//                        if (deflngde != null)
//                        {
//                            detailde.Title = deflngde.Attribute("Title").Value;
//                            detailde.MetaTitle = detailde.Title + " | suedtirol.info";

//                            mycontactinfode.Address = deflngde.Attribute("Street").Value;
//                            mycontactinfode.City = deflngde.Attribute("City").Value;
//                            eventadditionalinfode.Location = deflngde.Attribute("Location").Value;

//                            mycontactinfode.Email = deflngde.Attribute("EMail").Value;
//                            mycontactinfode.Url = deflngde.Attribute("Web").Value;

//                            eventadditionalinfode.Reg = deflngde.Attribute("Reg").Value;
//                            eventadditionalinfode.Mplace = deflngde.Attribute("MPlace").Value;

//                            mycontactinfode.CompanyName = deflngde.Attribute("MPlace").Value;
//                        }

//                        myevent.ContactInfos.TryAddOrUpdate("de", mycontactinfode);
//                        myevent.EventAdditionalInfos.TryAddOrUpdate("de", eventadditionalinfode);
//                    }

//                    if (definitionit != null)
//                    {
//                        ContactInfos mycontactinfoit = new ContactInfos();
//                        mycontactinfoit.Language = "it";

//                        EventAdditionalInfos eventadditionalinfoit = new EventAdditionalInfos();
//                        eventadditionalinfoit.Language = "it";

//                        //Language
//                        mycontactinfoit.Phonenumber = definitionde.Attribute("Phone").Value;
//                        mycontactinfoit.ZipCode = definitionde.Attribute("Zip").Value;
//                        //mycontactinfode.nat = definitionde.Attribute("NatID").Value;
//                        mycontactinfoit.CountryCode = "IT";

//                        var deflngit = definitionit.Element("DefinitionLng");

//                        if (deflngit != null)
//                        {
//                            detailit.Title = deflngit.Attribute("Title").Value;
//                            detailit.MetaTitle = detailit.Title + " | suedtirol.info";

//                            mycontactinfoit.Address = deflngit.Attribute("Street").Value;
//                            mycontactinfoit.City = deflngit.Attribute("City").Value;
//                            eventadditionalinfoit.Location = deflngit.Attribute("Location").Value;

//                            mycontactinfoit.Email = deflngit.Attribute("EMail").Value;
//                            mycontactinfoit.Url = deflngit.Attribute("Web").Value;

//                            eventadditionalinfoit.Reg = deflngit.Attribute("Reg").Value;
//                            eventadditionalinfoit.Mplace = deflngit.Attribute("MPlace").Value;

//                            mycontactinfoit.CompanyName = deflngit.Attribute("MPlace").Value;
//                        }

//                        myevent.ContactInfos.TryAddOrUpdate("it", mycontactinfoit);
//                        myevent.EventAdditionalInfos.TryAddOrUpdate("it", eventadditionalinfoit);
//                    }

//                    if (definitionen != null)
//                    {
//                        ContactInfos mycontactinfoen = new ContactInfos();
//                        mycontactinfoen.Language = "en";

//                        EventAdditionalInfos eventadditionalinfoen = new EventAdditionalInfos();
//                        eventadditionalinfoen.Language = "en";

//                        //Language
//                        mycontactinfoen.Phonenumber = definitionde.Attribute("Phone").Value;
//                        mycontactinfoen.ZipCode = definitionde.Attribute("Zip").Value;
//                        //mycontactinfode.nat = definitionde.Attribute("NatID").Value;
//                        mycontactinfoen.CountryCode = "IT";

//                        var deflngen = definitionen.Element("DefinitionLng");

//                        if (deflngen != null)
//                        {
//                            detailen.Title = deflngen.Attribute("Title").Value;
//                            detailen.MetaTitle = detailen.Title + " | suedtirol.info";

//                            mycontactinfoen.Address = deflngen.Attribute("Street").Value;
//                            mycontactinfoen.City = deflngen.Attribute("City").Value;
//                            eventadditionalinfoen.Location = deflngen.Attribute("Location").Value;

//                            mycontactinfoen.Email = deflngen.Attribute("EMail").Value;
//                            mycontactinfoen.Url = deflngen.Attribute("Web").Value;

//                            eventadditionalinfoen.Reg = deflngen.Attribute("Reg").Value;
//                            eventadditionalinfoen.Mplace = deflngen.Attribute("MPlace").Value;

//                            mycontactinfoen.CompanyName = deflngen.Attribute("MPlace").Value;
//                        }

//                        myevent.ContactInfos.TryAddOrUpdate("en", mycontactinfoen);
//                        myevent.EventAdditionalInfos.TryAddOrUpdate("en", eventadditionalinfoen);
//                    }

//                    //Description
//                    if (eventresponsede != null)
//                    {
//                        var descriptionsde = eventresponsede.Elements("Desc");

//                        if (descriptionsde != null)
//                        {
//                            foreach (XElement description in descriptionsde)
//                            {
//                                if (description.Attribute("DType").Value == "1")
//                                    detailde.BaseText = description.Attribute("DValue").Value;
//                                if (description.Attribute("DType").Value == "2")
//                                    detailde.GetThereText = description.Attribute("DValue").Value;
//                                //TODO ADD ALSO OTHER DTYPE
//                                //0 = undefined
//                                //1 = normal description
//                                //2 = rout description
//                                //3 = PDF
//                                //4 = Flash
//                                //5 = MP3
//                                //6 = Zip - File
//                                //7 = Website
//                                //8 = Video
//                                //9 = Facebook
//                                //10 = Google +
//                            }
//                        }
//                        myevent.Detail.TryAddOrUpdate("de", detailde);

//                        //New Element Description Additional
//                        var descadditionalde = eventresponsede.Elements("DescAdditional");

//                        if (descadditionalde != null)
//                        {
//                            var descriptionadditionalde = descadditionalde.FirstOrDefault();

//                            if (descriptionadditionalde != null)
//                            {
//                                EventDescAdditional eventdescadditionalde =
//                                    new EventDescAdditional();
//                                eventdescadditionalde.Type = descriptionadditionalde
//                                    .Attribute("DType")
//                                    .Value;
//                                eventdescadditionalde.Language = "de";
//                                eventdescadditionalde.Order = descriptionadditionalde
//                                    .Attribute("Order")
//                                    .Value;
//                                eventdescadditionalde.RQPlain =
//                                    descriptionadditionalde.Attribute("RQPlain") != null
//                                        ? descriptionadditionalde.Attribute("RQPlain").Value
//                                        : descriptionadditionalde.Attribute("RQPlan").Value;
//                                eventdescadditionalde.RQHtml = descriptionadditionalde
//                                    .Attribute("RQHtml")
//                                    .Value;
//                                eventdescadditionalde.RSPlain =
//                                    descriptionadditionalde.Attribute("RSPlain") != null
//                                        ? descriptionadditionalde.Attribute("RSPlain").Value
//                                        : descriptionadditionalde.Attribute("RSPlan").Value;
//                                eventdescadditionalde.RSHtml = descriptionadditionalde
//                                    .Attribute("RSHtml")
//                                    .Value;

//                                myevent.EventDescAdditional.TryAddOrUpdate(
//                                    "de",
//                                    eventdescadditionalde
//                                );
//                            }
//                        }
//                    }

//                    if (eventresponseit != null)
//                    {
//                        var descriptionsit = eventresponseit.Elements("Desc");

//                        if (descriptionsit != null)
//                        {
//                            foreach (XElement description in descriptionsit)
//                            {
//                                if (description.Attribute("DType").Value == "1")
//                                    detailit.BaseText = description.Attribute("DValue").Value;
//                                if (description.Attribute("DType").Value == "2")
//                                    detailit.GetThereText = description.Attribute("DValue").Value;
//                            }
//                        }

//                        myevent.Detail.TryAddOrUpdate("it", detailit);

//                        //New Element Description Additional
//                        var descadditionalit = eventresponseit.Elements("DescAdditional");

//                        if (descadditionalit != null)
//                        {
//                            var descriptionadditionalit = descadditionalit.FirstOrDefault();

//                            if (descriptionadditionalit != null)
//                            {
//                                EventDescAdditional eventdescadditionalit =
//                                    new EventDescAdditional();
//                                eventdescadditionalit.Type = descriptionadditionalit
//                                    .Attribute("DType")
//                                    .Value;
//                                eventdescadditionalit.Language = "it";
//                                eventdescadditionalit.Order = descriptionadditionalit
//                                    .Attribute("Order")
//                                    .Value;
//                                eventdescadditionalit.RQPlain =
//                                    descriptionadditionalit.Attribute("RQPlain") != null
//                                        ? descriptionadditionalit.Attribute("RQPlain").Value
//                                        : descriptionadditionalit.Attribute("RQPlan").Value;
//                                eventdescadditionalit.RQHtml = descriptionadditionalit
//                                    .Attribute("RQHtml")
//                                    .Value;
//                                eventdescadditionalit.RSPlain =
//                                    descriptionadditionalit.Attribute("RSPlain") != null
//                                        ? descriptionadditionalit.Attribute("RSPlain").Value
//                                        : descriptionadditionalit.Attribute("RSPlan").Value;
//                                eventdescadditionalit.RSHtml = descriptionadditionalit
//                                    .Attribute("RSHtml")
//                                    .Value;

//                                myevent.EventDescAdditional.TryAddOrUpdate(
//                                    "it",
//                                    eventdescadditionalit
//                                );
//                            }
//                        }
//                    }

//                    if (eventresponseen != null)
//                    {
//                        var descriptionsen = eventresponseen.Elements("Desc");

//                        if (descriptionsen != null)
//                        {
//                            foreach (XElement description in descriptionsen)
//                            {
//                                if (description.Attribute("DType").Value == "1")
//                                    detailen.BaseText = description.Attribute("DValue").Value;
//                                if (description.Attribute("DType").Value == "2")
//                                    detailen.GetThereText = description.Attribute("DValue").Value;
//                            }
//                        }
//                        myevent.Detail.TryAddOrUpdate("en", detailen);

//                        //New Element Description Additional
//                        var descadditionalen = eventresponseen.Elements("DescAdditional");

//                        if (descadditionalen != null)
//                        {
//                            var descriptionadditionalen = descadditionalen.FirstOrDefault();

//                            if (descriptionadditionalen != null)
//                            {
//                                EventDescAdditional eventdescadditionalen =
//                                    new EventDescAdditional();
//                                eventdescadditionalen.Type = descriptionadditionalen
//                                    .Attribute("DType")
//                                    .Value;
//                                eventdescadditionalen.Language = "en";
//                                eventdescadditionalen.Order = descriptionadditionalen
//                                    .Attribute("Order")
//                                    .Value;
//                                eventdescadditionalen.RQPlain =
//                                    descriptionadditionalen.Attribute("RQPlain") != null
//                                        ? descriptionadditionalen.Attribute("RQPlain").Value
//                                        : descriptionadditionalen.Attribute("RQPlan").Value;
//                                eventdescadditionalen.RQHtml = descriptionadditionalen
//                                    .Attribute("RQHtml")
//                                    .Value;
//                                eventdescadditionalen.RSPlain =
//                                    descriptionadditionalen.Attribute("RSPlain") != null
//                                        ? descriptionadditionalen.Attribute("RSPlain").Value
//                                        : descriptionadditionalen.Attribute("RSPlan").Value;
//                                eventdescadditionalen.RSHtml = descriptionadditionalen
//                                    .Attribute("RSHtml")
//                                    .Value;

//                                myevent.EventDescAdditional.TryAddOrUpdate(
//                                    "en",
//                                    eventdescadditionalen
//                                );
//                            }
//                        }
//                    }

//                    //ImageGallery
//                    List<ImageGallery> myimagegalleryList = new List<ImageGallery>();

//                    if (eventresponsede != null)
//                    {
//                        var fotos = eventresponsede
//                            .Elements("Foto")
//                            .OrderBy(x => Convert.ToInt32(x.Attribute("Order").Value));
//                        if (fotos != null)
//                        {
//                            int i = 0;
//                            foreach (XElement myfoto in fotos)
//                            {
//                                ImageGallery myimggallery = new ImageGallery();

//                                myimggallery.ImageUrl = myfoto.Attribute("FPath").Value;
//                                myimggallery.ImageDesc.TryAddOrUpdate(
//                                    "de",
//                                    myfoto.Attribute("CRight").Value
//                                );
//                                myimggallery.ListPosition =
//                                    Convert.ToInt32(myfoto.Attribute("Order").Value) - 1;
//                                myimggallery.ImageSource = "LTS";
//                                myimggallery.IsInGallery = true;
//                                myimggallery.CopyRight =
//                                    myfoto.Attribute("CRight") != null
//                                        ? myfoto.Attribute("CRight").Value
//                                        : "";
//                                myimggallery.License =
//                                    myfoto.Attribute("License") != null
//                                        ? myfoto.Attribute("License").Value
//                                        : "";

//                                myimagegalleryList.Add(myimggallery);

//                                i++;
//                            }
//                        }
//                    }

//                    if (eventresponseit != null)
//                    {
//                        var fotosit = eventresponseit
//                            .Elements("Foto")
//                            .OrderBy(x => Convert.ToInt32(x.Attribute("Order").Value));
//                        ;
//                        if (fotosit != null)
//                        {
//                            foreach (XElement myfoto in fotosit)
//                            {
//                                myimagegalleryList
//                                    .Where(x =>
//                                        x.ListPosition
//                                        == (Convert.ToInt32(myfoto.Attribute("Order").Value) - 1)
//                                    )
//                                    .FirstOrDefault()
//                                    .ImageDesc.TryAddOrUpdate(
//                                        "it",
//                                        myfoto.Attribute("CRight").Value
//                                    );
//                            }
//                        }
//                    }

//                    if (eventresponseen != null)
//                    {
//                        var fotosen = eventresponseen
//                            .Elements("Foto")
//                            .OrderBy(x => Convert.ToInt32(x.Attribute("Order").Value));
//                        ;
//                        if (fotosen != null)
//                        {
//                            foreach (XElement myfoto in fotosen)
//                            {
//                                myimagegalleryList
//                                    .Where(x =>
//                                        x.ListPosition
//                                        == (Convert.ToInt32(myfoto.Attribute("Order").Value) - 1)
//                                    )
//                                    .FirstOrDefault()
//                                    .ImageDesc.TryAddOrUpdate(
//                                        "en",
//                                        myfoto.Attribute("CRight").Value
//                                    );
//                            }
//                        }
//                    }

//                    myevent.ImageGallery = myimagegalleryList.ToList();

//                    if (myevent.ImageGallery.Count() > 0)
//                    {
//                        myevent.ImageGallery = myevent
//                            .ImageGallery.OrderBy(x => x.ListPosition)
//                            .ToList();
//                    }

//                    //Topic
//                    var topics = eventresponsede.Elements("Topic");
//                    if (topics != null)
//                    {
//                        List<string> topicridlist = new List<string>();

//                        foreach (XElement mytopic in topics)
//                        {
//                            topicridlist.Add(mytopic.Attribute("TopRID").Value);
//                        }
//                        myevent.TopicRIDs = topicridlist.ToList();
//                    }

//                    //Topic TODO Refactor
//                    var topicsnew = eventresponsede.Elements("Topic");
//                    if (topicsnew != null)
//                    {
//                        List<TopicLinked> topicridlist = new List<TopicLinked>();

//                        foreach (XElement mytopic in topicsnew)
//                        {
//                            TopicLinked thetopic = new TopicLinked();
//                            thetopic.TopicRID = mytopic.Attribute("TopRID").Value;

//                            switch (thetopic.TopicRID)
//                            {
//                                case "0D25868CC23242D6AC97AEB2973CB3D6":
//                                    thetopic.TopicInfo = "Tagungen Vorträge";
//                                    break;
//                                case "162C0067811B477DA725D2F5F2D98398":
//                                    thetopic.TopicInfo = "Sport";
//                                    break;
//                                case "252200A028C8449D9A6205369A6D0D36":
//                                    thetopic.TopicInfo = "Gastronomie/Typische Produkte";
//                                    break;
//                                case "33BDC54BD39946F4852B3394B00610AE":
//                                    thetopic.TopicInfo = "Handwerk/Brauchtum";
//                                    break;
//                                case "4C4961D9FC5B48EEB73067BEB9D4402A":
//                                    thetopic.TopicInfo = "Messen/Märkte";
//                                    break;
//                                case "6884FE362C88434B9F49725E3328112B":
//                                    thetopic.TopicInfo = "Theater/Vorführungen";
//                                    break;
//                                case "767F6F43FC394CE9A3C8A9725C6FF134":
//                                    thetopic.TopicInfo = "Kurse/Bildung";
//                                    break;
//                                case "7E048074BA004EC58E29E330A9AA476B":
//                                    thetopic.TopicInfo = "Musik/Tanz";
//                                    break;
//                                case "9C3449EE278C4D94AA5A7C286729DEA0":
//                                    thetopic.TopicInfo = "Volksfeste/Festivals";
//                                    break;
//                                case "ACE8B613F2074A7BB59C0B1DD40A43CD":
//                                    thetopic.TopicInfo = "Wanderungen/Ausflüge";
//                                    break;
//                                case "B5467FEFE5C74FA5AD32B83793A76165":
//                                    thetopic.TopicInfo = "Führungen/Besichtigungen";
//                                    break;
//                                case "C72CE969B98947FABC99CBC7B033F28E":
//                                    thetopic.TopicInfo = "Ausstellungen/Kunst";
//                                    break;
//                                case "D98B49DF24C342D09A8161836435CF86":
//                                    thetopic.TopicInfo = "Familie";
//                                    break;
//                            }

//                            topicridlist.Add(thetopic);
//                        }
//                        myevent.Topics = topicridlist.ToList();
//                    }

//                    //Publisher

//                    var publisher = eventresponsede.Elements("Publish");
//                    myevent.EventPublisher = new List<EventPublisher>();

//                    if (publisher != null)
//                    {
//                        foreach (XElement mypublish in publisher)
//                        {
//                            EventPublisher myeventpublisher = new EventPublisher();
//                            myeventpublisher.Publish =
//                                mypublish.Attribute("Publish") != null
//                                    ? Convert.ToInt32(mypublish.Attribute("Publish").Value)
//                                    : 0;
//                            myeventpublisher.PublisherRID =
//                                mypublish.Attribute("PublRID") != null
//                                    ? mypublish.Attribute("PublRID").Value
//                                    : "";
//                            myeventpublisher.Ranc =
//                                mypublish.Attribute("Ranc") != null
//                                    ? Convert.ToInt32(mypublish.Attribute("Ranc").Value)
//                                    : 0;

//                            myevent.EventPublisher.Add(myeventpublisher);
//                        }
//                    }

//                    //Event Price OBSOLETE!!

//                    var eventprice = eventresponsede.Elements("Price");
//                    if (eventprice != null)
//                    {
//                        foreach (XElement myprice in eventprice)
//                        {
//                            EventPrice myeventprice = new EventPrice();
//                            myeventprice.Price =
//                                myprice.Attribute("PValue") != null
//                                    ? Convert.ToDouble(myprice.Attribute("PValue").Value, myculture)
//                                    : 0;
//                            myeventprice.Pstd =
//                                myprice.Attribute("PStd") != null
//                                    ? myprice.Attribute("PStd").Value
//                                    : "";
//                            myeventprice.Type =
//                                myprice.Attribute("PType") != null
//                                    ? myprice.Attribute("PType").Value
//                                    : "";

//                            myeventprice.Language = "de";

//                            var mypricelng = myprice.Element("PriceLng");
//                            if (mypricelng != null)
//                            {
//                                myeventprice.ShortDesc =
//                                    mypricelng.Attribute("PShort") != null
//                                        ? mypricelng.Attribute("PShort").Value
//                                        : "";
//                                myeventprice.Description =
//                                    mypricelng.Attribute("PDesc") != null
//                                        ? mypricelng.Attribute("PDesc").Value
//                                        : "";
//                            }

//                            myevent.EventPrice.TryAddOrUpdate("de", myeventprice);
//                        }
//                    }

//                    if (eventresponseit != null)
//                    {
//                        //Event Price it
//                        var eventpriceit = eventresponseit.Elements("Price");
//                        if (eventpriceit != null)
//                        {
//                            foreach (XElement myprice in eventpriceit)
//                            {
//                                EventPrice myeventprice = new EventPrice();
//                                myeventprice.Price =
//                                    myprice.Attribute("PValue") != null
//                                        ? Convert.ToDouble(
//                                            myprice.Attribute("PValue").Value,
//                                            myculture
//                                        )
//                                        : 0;
//                                myeventprice.Pstd =
//                                    myprice.Attribute("PStd") != null
//                                        ? myprice.Attribute("PStd").Value
//                                        : "";
//                                myeventprice.Type =
//                                    myprice.Attribute("PType") != null
//                                        ? myprice.Attribute("PType").Value
//                                        : "";

//                                myeventprice.Language = "it";

//                                var mypricelng = myprice.Element("PriceLng");
//                                if (mypricelng != null)
//                                {
//                                    myeventprice.ShortDesc =
//                                        mypricelng.Attribute("PShort") != null
//                                            ? mypricelng.Attribute("PShort").Value
//                                            : "";
//                                    myeventprice.Description =
//                                        mypricelng.Attribute("PDesc") != null
//                                            ? mypricelng.Attribute("PDesc").Value
//                                            : "";
//                                }

//                                myevent.EventPrice.TryAddOrUpdate("it", myeventprice);
//                            }
//                        }
//                    }

//                    if (eventresponseen != null)
//                    {
//                        //Event Price en
//                        var eventpriceen = eventresponseen.Elements("Price");
//                        if (eventpriceen != null)
//                        {
//                            foreach (XElement mypriceen in eventpriceen)
//                            {
//                                EventPrice myeventprice = new EventPrice();
//                                myeventprice.Price =
//                                    mypriceen.Attribute("PValue") != null
//                                        ? Convert.ToDouble(
//                                            mypriceen.Attribute("PValue").Value,
//                                            myculture
//                                        )
//                                        : 0;
//                                myeventprice.Pstd =
//                                    mypriceen.Attribute("PStd") != null
//                                        ? mypriceen.Attribute("PStd").Value
//                                        : "";
//                                myeventprice.Type =
//                                    mypriceen.Attribute("PType") != null
//                                        ? mypriceen.Attribute("PType").Value
//                                        : "";

//                                myeventprice.Language = "en";

//                                var mypricelng = mypriceen.Element("PriceLng");
//                                if (mypricelng != null)
//                                {
//                                    myeventprice.ShortDesc =
//                                        mypricelng.Attribute("PShort") != null
//                                            ? mypricelng.Attribute("PShort").Value
//                                            : "";
//                                    myeventprice.Description =
//                                        mypricelng.Attribute("PDesc") != null
//                                            ? mypricelng.Attribute("PDesc").Value
//                                            : "";
//                                }

//                                myevent.EventPrice.TryAddOrUpdate("en", myeventprice);
//                            }
//                        }
//                    }

//                    //Event Day

//                    DateTime firstbegindate = DateTime.MaxValue;
//                    DateTime lastenddate = DateTime.MinValue;

//                    var days = eventresponsede.Elements("Day");
//                    if (days.Count() > 0)
//                    {
//                        List<EventDate> myeventdatelist = new List<EventDate>();

//                        foreach (XElement myday in days)
//                        {
//                            EventDate myeventdate = new EventDate();

//                            myeventdate.From = Convert.ToDateTime(myday.Attribute("Date").Value);
//                            myeventdate.To = Convert.ToDateTime(myday.Attribute("DateTo").Value);

//                            myeventdate.SingleDays =
//                                myday.Attribute("SingleDays").Value == "0" ? false : true;
//                            myeventdate.MinPersons = Convert.ToInt32(myday.Attribute("MinP").Value);
//                            myeventdate.MaxPersons = Convert.ToInt32(myday.Attribute("MaxP").Value);
//                            myeventdate.Ticket =
//                                myday.Attribute("Ticket").Value == "0" ? false : true;
//                            myeventdate.Begin = TimeSpan.Parse(myday.Attribute("Begin").Value);
//                            myeventdate.End = TimeSpan.Parse(myday.Attribute("End").Value);
//                            myeventdate.Entrance = TimeSpan.Parse(
//                                myday.Attribute("Entrance").Value
//                            );
//                            myeventdate.GpsEast =
//                                myday.Attribute("GEP") != null
//                                    ? Convert.ToDouble(myday.Attribute("GEP").Value, myculture)
//                                    : 0;
//                            myeventdate.GpsNorth =
//                                myday.Attribute("GNP") != null
//                                    ? Convert.ToDouble(myday.Attribute("GNP").Value, myculture)
//                                    : 0;

//                            if (firstbegindate > myeventdate.From)
//                                firstbegindate = myeventdate.From;

//                            if (lastenddate < myeventdate.To)
//                                lastenddate = myeventdate.To;

//                            //New Fields
//                            bool.TryParse(myday.Attribute("Active").Value, out var dayactive);
//                            myeventdate.Active = dayactive;
//                            myeventdate.DayRID = myday.Attribute("I6RID").Value;

//                            myeventdate.PriceFrom =
//                                myday.Attribute("PriceFrom") != null
//                                    ? myday.Attribute("PriceFrom").Value
//                                    : null;
//                            myeventdate.Cancelled =
//                                myday.Attribute("Cancelled") != null
//                                    ? myday.Attribute("Cancelled").Value
//                                    : null;

//                            //InscriptionTill
//                            if (!String.IsNullOrEmpty(myday.Attribute("InscriptionTill").Value))
//                                myeventdate.InscriptionTill =
//                                    myday.Attribute("InscriptionTill") != null
//                                        ? Convert.ToDouble(
//                                            myday.Attribute("InscriptionTill").Value,
//                                            myculture
//                                        )
//                                        : 0;

//                            if (myday.Element("DayLng") != null)
//                            {
//                                var maydayde = myday.Element("DayLng");

//                                EventDateAdditionalInfo eventdateadditionalinfode =
//                                    new EventDateAdditionalInfo();
//                                eventdateadditionalinfode.Description = maydayde
//                                    .Attribute("DDesc")
//                                    .Value;
//                                eventdateadditionalinfode.Guide = maydayde
//                                    .Attribute("DGuide")
//                                    .Value;
//                                eventdateadditionalinfode.InscriptionLanguage = maydayde
//                                    .Attribute("DLInsc")
//                                    .Value;
//                                eventdateadditionalinfode.Language = maydayde
//                                    .Attribute("LngID")
//                                    .Value;
//                                eventdateadditionalinfode.Cancelled = maydayde
//                                    .Attribute("DCancelled")
//                                    .Value;

//                                myeventdate.EventDateAdditionalInfo.TryAddOrUpdate(
//                                    eventdateadditionalinfode.Language,
//                                    eventdateadditionalinfode
//                                );

//                                //Do for the other languages
//                                if (eventresponseit != null)
//                                {
//                                    var eventresponseitday = eventresponseit
//                                        .Elements("Day")
//                                        .Where(x =>
//                                            x.Attribute("I6RID").Value == myeventdate.DayRID
//                                        )
//                                        .FirstOrDefault();

//                                    if (eventresponseitday != null)
//                                    {
//                                        if (eventresponseitday.Element("DayLng") != null)
//                                        {
//                                            var maydayit = eventresponseitday.Element("DayLng");

//                                            EventDateAdditionalInfo eventdateadditionalinfoit =
//                                                new EventDateAdditionalInfo();
//                                            eventdateadditionalinfoit.Description = maydayit
//                                                .Attribute("DDesc")
//                                                .Value;
//                                            eventdateadditionalinfoit.Guide = maydayit
//                                                .Attribute("DGuide")
//                                                .Value;
//                                            eventdateadditionalinfoit.InscriptionLanguage = maydayit
//                                                .Attribute("DLInsc")
//                                                .Value;
//                                            eventdateadditionalinfoit.Language = maydayit
//                                                .Attribute("LngID")
//                                                .Value;

//                                            myeventdate.EventDateAdditionalInfo.TryAddOrUpdate(
//                                                eventdateadditionalinfoit.Language,
//                                                eventdateadditionalinfoit
//                                            );
//                                        }
//                                    }
//                                }
//                                if (eventresponseen != null)
//                                {
//                                    var eventresponseenday = eventresponseen
//                                        .Elements("Day")
//                                        .Where(x =>
//                                            x.Attribute("I6RID").Value == myeventdate.DayRID
//                                        )
//                                        .FirstOrDefault();

//                                    if (eventresponseenday != null)
//                                    {
//                                        if (eventresponseenday.Element("DayLng") != null)
//                                        {
//                                            var maydayen = eventresponseenday.Element("DayLng");

//                                            EventDateAdditionalInfo eventdateadditionalinfoen =
//                                                new EventDateAdditionalInfo();
//                                            eventdateadditionalinfoen.Description = maydayen
//                                                .Attribute("DDesc")
//                                                .Value;
//                                            eventdateadditionalinfoen.Guide = maydayen
//                                                .Attribute("DGuide")
//                                                .Value;
//                                            eventdateadditionalinfoen.InscriptionLanguage = maydayen
//                                                .Attribute("DLInsc")
//                                                .Value;
//                                            eventdateadditionalinfoen.Language = maydayen
//                                                .Attribute("LngID")
//                                                .Value;

//                                            myeventdate.EventDateAdditionalInfo.TryAddOrUpdate(
//                                                eventdateadditionalinfoen.Language,
//                                                eventdateadditionalinfoen
//                                            );
//                                        }
//                                    }
//                                }
//                            }

//                            //<OTime Days = '…' Entr1='…' Begin1='…' End1='…' Entr2='…' Begin2='…' End2='…' />
//                            if (myday.Elements("OTime").Count() > 0)
//                            {
//                                var myotimes = myday.Elements("OTime");

//                                foreach (var myotime in myotimes)
//                                {
//                                    EventDateAdditionalTime eventadditionaltime =
//                                        new EventDateAdditionalTime();
//                                    eventadditionaltime.Days = myotime.Attribute("Days").Value; //Convert.ToDateTime(myotime.Attribute("Days").Value); //TODO FIND OUT DATATYPE
//                                    eventadditionaltime.Begin1 =
//                                        myotime.Attribute("Begin1") != null
//                                            ? TimeSpan.Parse(myotime.Attribute("Begin1").Value)
//                                            : TimeSpan.Zero;
//                                    eventadditionaltime.Begin2 =
//                                        myotime.Attribute("Begin2") != null
//                                            ? TimeSpan.Parse(myotime.Attribute("Begin2").Value)
//                                            : TimeSpan.Zero;
//                                    eventadditionaltime.End1 =
//                                        myotime.Attribute("End1") != null
//                                            ? TimeSpan.Parse(myotime.Attribute("End1").Value)
//                                            : TimeSpan.Zero;
//                                    eventadditionaltime.End2 =
//                                        myotime.Attribute("End2") != null
//                                            ? TimeSpan.Parse(myotime.Attribute("End2").Value)
//                                            : TimeSpan.Zero;
//                                    eventadditionaltime.Entrance1 =
//                                        myotime.Attribute("Entrance1") != null
//                                            ? TimeSpan.Parse(myotime.Attribute("Entrance1").Value)
//                                            : TimeSpan.Zero;
//                                    eventadditionaltime.Entrance2 =
//                                        myotime.Attribute("Entrance2") != null
//                                            ? TimeSpan.Parse(myotime.Attribute("Entrance2").Value)
//                                            : TimeSpan.Zero;

//                                    if (myeventdate.EventDateAdditionalTime == null)
//                                        myeventdate.EventDateAdditionalTime =
//                                            new List<EventDateAdditionalTime>();

//                                    myeventdate.EventDateAdditionalTime.Add(eventadditionaltime);
//                                }
//                            }
//                            else if (myeventdate.EventDateAdditionalTime != null)
//                                myeventdate.EventDateAdditionalTime.Clear();

//                            //            <CDay RID = '…' Day='…' Begin='…' TicketsAv='…' MaxSellableTickets="…" Availability="…" >
//                            //                <Variant VarRID = "…" Price="…" IsStandardVariant="…" TotalSellable="…" />
//                            //            </CDay>

//                            if (myday.Element("CDay") != null)
//                            {
//                                var mcday = myday.Element("CDay");

//                                //CDAY Parsing
//                                EventDateCalculatedDay eventcalculated =
//                                    new EventDateCalculatedDay();
//                                eventcalculated.CDayRID = mcday.Attribute("RID").Value;
//                                eventcalculated.Day = Convert.ToDateTime(
//                                    mcday.Attribute("Day").Value
//                                );
//                                eventcalculated.Begin = TimeSpan.Parse(
//                                    mcday.Attribute("Begin").Value
//                                );
//                                //eventcalculated.TicketsAvailable = mcday.Attribute("TicketsAv") != null ? Convert.ToInt32(mcday.Attribute("TicketsAv").Value) : -1;
//                                //eventcalculated.MaxSellableTickets = mcday.Attribute("MaxSellableTickets") != null ? Convert.ToInt32(mcday.Attribute("MaxSellableTickets").Value) : -1;
//                                //eventcalculated.AvailabilityCalculatedValue = mcday.Attribute("TicketsAv") != null ? Convert.ToInt32(mcday.Attribute("TicketsAv").Value) : -1;
//                                //eventcalculated.AvailabilityLow = mcday.Attribute("AvailabilityLow") != null ? Convert.ToInt32(mcday.Attribute("AvailabilityLow").Value) : -1;
//                                //eventcalculated.PriceFrom = mcday.Attribute("PriceFrom") != null ? Convert.ToDouble(mcday.Attribute("PriceFrom").Value) : (double?)null;

//                                //VARIANT EventCalculated
//                                //if (mcday.Elements("Variant") != null)
//                                //{
//                                //    foreach(var cdayvariant in mcday.Elements("Variant"))
//                                //    {
//                                //        EventDateCalculatedDayVariant eventdatecalculateddayvariant = new EventDateCalculatedDayVariant();
//                                //        eventdatecalculateddayvariant.VarRID = cdayvariant.Attribute("VarRID").Value;
//                                //        eventdatecalculateddayvariant.Price = cdayvariant.Attribute("Price") != null ? Convert.ToDouble(cdayvariant.Attribute("Price").Value) : 0;
//                                //        eventdatecalculateddayvariant.IsStandardVariant = cdayvariant.Attribute("IsStandardVariant") != null ? Convert.ToBoolean(cdayvariant.Attribute("IsStandardVariant").Value) : (bool?)null;
//                                //        eventdatecalculateddayvariant.TotalSellable = cdayvariant.Attribute("TotalSellable") != null ? Convert.ToInt32(cdayvariant.Attribute("TotalSellable").Value) : (int?)null;

//                                //        if (eventcalculated.EventDateCalculatedDayVariant == null)
//                                //            eventcalculated.EventDateCalculatedDayVariant = new List<EventDateCalculatedDayVariant>();

//                                //        eventcalculated.EventDateCalculatedDayVariant.Add(eventdatecalculateddayvariant);
//                                //    }
//                                //}


//                                myeventdate.EventCalculatedDay = eventcalculated;
//                            }

//                            myeventdatelist.Add(myeventdate);
//                        }

//                        myevent.EventDate = myeventdatelist.ToList();
//                    }

//                    //Final Check about Date

//                    if (firstbegindate == DateTime.MaxValue && lastenddate == DateTime.MinValue)
//                    {
//                        myevent.DateBegin = firstbegindateOLD;
//                        myevent.DateEnd = lastenddateOLD;
//                    }
//                    else
//                    {
//                        myevent.DateBegin = firstbegindate;
//                        myevent.DateEnd = lastenddate;
//                    }

//                    //District

//                    var districts = eventresponsede.Elements("District");
//                    if (districts != null)
//                    {
//                        List<string> districtlist = new List<string>();

//                        //if (myevent.DistrictIds != null)
//                        //    districtlist = myevent.DistrictIds.ToList();

//                        foreach (XElement district in districts)
//                        {
//                            var frarid = district.Attribute("FraRID").Value;

//                            if (!districtlist.Contains(frarid))
//                                districtlist.Add(frarid);
//                        }

//                        myevent.DistrictIds = districtlist.ToList();

//                        if (eventresponsede.Elements("District").FirstOrDefault() != null)
//                            myevent.DistrictId = districtlist.FirstOrDefault();
//                    }

//                    //Organizer

//                    var myorganizer = eventresponsede.Element("Organizer").Element("Head");
//                    if (myorganizer != null)
//                    {
//                        myevent.OrgRID = myorganizer.Attribute("OrgRID").Value;

//                        var orgadress = myorganizer.Element("Address");

//                        if (orgadress != null)
//                        {
//                            ContactInfos organizerinfos = new ContactInfos();

//                            organizerinfos.Vat = orgadress.Attribute("Vat").Value;
//                            organizerinfos.Tax = orgadress.Attribute("Tax").Value;
//                            organizerinfos.ZipCode = orgadress.Attribute("Zip").Value;
//                            organizerinfos.Phonenumber = orgadress.Attribute("Phone").Value;
//                            organizerinfos.Faxnumber = orgadress.Attribute("Fax").Value;
//                            organizerinfos.Url = orgadress.Attribute("Web").Value;
//                            organizerinfos.Email = orgadress.Attribute("EMail").Value;
//                            //organizerinfos.OrganizerNatID = orgadress.Attribute("NatID").Value;
//                            organizerinfos.Givenname = orgadress.Attribute("FName").Value;
//                            organizerinfos.Surname = orgadress.Attribute("LName").Value;
//                            organizerinfos.Language = "de";

//                            var orgadresslng = orgadress.Element("AddressLng");

//                            if (orgadresslng != null)
//                            {
//                                organizerinfos.Address = orgadresslng.Attribute("Street").Value;
//                                organizerinfos.City = orgadresslng.Attribute("City").Value;
//                                organizerinfos.CompanyName = orgadresslng.Attribute("OName").Value;
//                            }

//                            //OrganizerInfosList.Add(organizerinfos);

//                            myevent.OrganizerInfos.TryAddOrUpdate("de", organizerinfos);
//                        }
//                    }

//                    if (eventresponseit != null)
//                    {
//                        var myorganizerit = eventresponseit.Element("Organizer").Element("Head");
//                        if (myorganizerit != null)
//                        {
//                            var orgadressit = myorganizerit.Element("Address");

//                            if (orgadressit != null)
//                            {
//                                ContactInfos organizerinfos = new ContactInfos();

//                                organizerinfos.Vat = orgadressit.Attribute("Vat").Value;
//                                organizerinfos.Tax = orgadressit.Attribute("Tax").Value;
//                                organizerinfos.ZipCode = orgadressit.Attribute("Zip").Value;
//                                organizerinfos.Phonenumber = orgadressit.Attribute("Phone").Value;
//                                organizerinfos.Faxnumber = orgadressit.Attribute("Fax").Value;
//                                organizerinfos.Url = orgadressit.Attribute("Web").Value;
//                                organizerinfos.Email = orgadressit.Attribute("EMail").Value;
//                                //organizerinfos.OrganizerNatID = orgadress.Attribute("NatID").Value;
//                                organizerinfos.Givenname = orgadressit.Attribute("FName").Value;
//                                organizerinfos.Surname = orgadressit.Attribute("LName").Value;
//                                organizerinfos.Language = "it";

//                                var orgadresslng = orgadressit.Element("AddressLng");

//                                if (orgadresslng != null)
//                                {
//                                    organizerinfos.Address = orgadresslng.Attribute("Street").Value;
//                                    organizerinfos.City = orgadresslng.Attribute("City").Value;
//                                    organizerinfos.CompanyName = orgadresslng
//                                        .Attribute("OName")
//                                        .Value;
//                                }

//                                //OrganizerInfosList.Add(organizerinfos);

//                                myevent.OrganizerInfos.TryAddOrUpdate("it", organizerinfos);
//                            }
//                        }
//                    }

//                    if (eventresponseen != null)
//                    {
//                        //Organizer en
//                        var myorganizeren = eventresponseen.Element("Organizer").Element("Head");
//                        if (myorganizeren != null)
//                        {
//                            var orgadressen = myorganizeren.Element("Address");

//                            if (orgadressen != null)
//                            {
//                                ContactInfos organizerinfos = new ContactInfos();

//                                organizerinfos.Vat = orgadressen.Attribute("Vat").Value;
//                                organizerinfos.Tax = orgadressen.Attribute("Tax").Value;
//                                organizerinfos.ZipCode = orgadressen.Attribute("Zip").Value;
//                                organizerinfos.Phonenumber = orgadressen.Attribute("Phone").Value;
//                                organizerinfos.Faxnumber = orgadressen.Attribute("Fax").Value;
//                                organizerinfos.Url = orgadressen.Attribute("Web").Value;
//                                organizerinfos.Email = orgadressen.Attribute("EMail").Value;
//                                //organizerinfos.OrganizerNatID = orgadress.Attribute("NatID").Value;
//                                organizerinfos.Givenname = orgadressen.Attribute("FName").Value;
//                                organizerinfos.Surname = orgadressen.Attribute("LName").Value;
//                                organizerinfos.Language = "en";

//                                var orgadresslng = orgadressen.Element("AddressLng");

//                                if (orgadresslng != null)
//                                {
//                                    organizerinfos.Address = orgadresslng.Attribute("Street").Value;
//                                    organizerinfos.City = orgadresslng.Attribute("City").Value;
//                                    organizerinfos.CompanyName = orgadresslng
//                                        .Attribute("OName")
//                                        .Value;
//                                }

//                                //OrganizerInfosList.Add(organizerinfos);

//                                myevent.OrganizerInfos.TryAddOrUpdate("en", organizerinfos);
//                            }
//                        }
//                    }

//                    //Operationschedule OV

//                    var operationscheduleoverview = eventresponsede.Element("OperationScheduleOv");

//                    if (operationscheduleoverview != null)
//                    {
//                        EventOperationScheduleOverview eventoperationscheduleoverview =
//                            new EventOperationScheduleOverview();
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Mon").Value,
//                            out var ovmonday
//                        );
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Tue").Value,
//                            out var ovtuesday
//                        );
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Weds").Value,
//                            out var ovwednesday
//                        );
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Thu").Value,
//                            out var ovthursday
//                        );
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Fri").Value,
//                            out var ovfriday
//                        );
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Sat").Value,
//                            out var ovsaturday
//                        );
//                        bool.TryParse(
//                            operationscheduleoverview.Attribute("Sun").Value,
//                            out var ovsunday
//                        );

//                        eventoperationscheduleoverview.Monday = ovmonday;
//                        eventoperationscheduleoverview.Tuesday = ovtuesday;
//                        eventoperationscheduleoverview.Wednesday = ovwednesday;
//                        eventoperationscheduleoverview.Thursday = ovthursday;
//                        eventoperationscheduleoverview.Friday = ovfriday;
//                        eventoperationscheduleoverview.Saturday = ovsaturday;
//                        eventoperationscheduleoverview.Sunday = ovsunday;

//                        myevent.EventOperationScheduleOverview = eventoperationscheduleoverview;
//                    }
//                    else
//                        myevent.EventOperationScheduleOverview = null;

//                    ////Variant Parsing
//                    //List<EventVariant> eventvariantlistde = new List<EventVariant>();
//                    //List<EventVariant> eventvariantlistit = new List<EventVariant>();
//                    //List<EventVariant> eventvariantlisten = new List<EventVariant>();

//                    //var variantinfos = eventresponsede.Elements("Variant");
//                    //if(variantinfos != null)
//                    //{
//                    //    //if(variantinfos.Count() > 1)
//                    //    //{
//                    //    //    //FOR DEBUGGING
//                    //    //}

//                    //    foreach(var variantinfo in variantinfos)
//                    //    {
//                    //        EventVariant eventvariant = new EventVariant();
//                    //        eventvariant.VarRID = variantinfo.Attribute("VarRID").Value;

//                    //        //LanguageRelevant Variant
//                    //        var variantinfode = variantinfo.Element("VariantLng");

//                    //        if(variantinfode != null)
//                    //        {
//                    //            eventvariant.Description = variantinfode.Attribute("VDesc").Value;
//                    //            eventvariant.Language = variantinfode.Attribute("LngID").Value;
//                    //            eventvariant.LongDescription = variantinfode.Attribute("VLong").Value;
//                    //            eventvariant.ShortDescription = variantinfode.Attribute("VShort").Value;
//                    //        }
//                    //        eventvariantlistde.Add(eventvariant);


//                    //        if (eventresponseit != null)
//                    //        {
//                    //            //find variant with this RID //check null handling
//                    //            var variantinfoitgeneral = eventresponseit.Elements("Variant").Where(x => x.Attribute("VarRID").Value == eventvariant.VarRID).FirstOrDefault();

//                    //            EventVariant eventvariantit = new EventVariant();
//                    //            eventvariantit.VarRID = eventvariant.VarRID;

//                    //            var variantinfoit = variantinfoitgeneral.Element("VariantLng");
//                    //            if (variantinfoit != null)
//                    //            {
//                    //                eventvariantit.Description = variantinfode.Attribute("VDesc").Value;
//                    //                eventvariantit.Language = variantinfode.Attribute("LngID").Value;
//                    //                eventvariantit.LongDescription = variantinfode.Attribute("VLong").Value;
//                    //                eventvariantit.ShortDescription = variantinfode.Attribute("VShort").Value;
//                    //            }
//                    //            eventvariantlistit.Add(eventvariantit);
//                    //        }
//                    //        if (eventresponseen != null)
//                    //        {
//                    //            //find variant with this RID //check null handling
//                    //            var variantinfoengeneral = eventresponseen.Elements("Variant").Where(x => x.Attribute("VarRID").Value == eventvariant.VarRID).FirstOrDefault();

//                    //            EventVariant eventvarianten = new EventVariant();
//                    //            eventvarianten.VarRID = eventvariant.VarRID;

//                    //            var variantinfoen = variantinfoengeneral.Element("VariantLng");
//                    //            if (variantinfoen != null)
//                    //            {
//                    //                eventvarianten.Description = variantinfode.Attribute("VDesc").Value;
//                    //                eventvarianten.Language = variantinfode.Attribute("LngID").Value;
//                    //                eventvarianten.LongDescription = variantinfode.Attribute("VLong").Value;
//                    //                eventvarianten.ShortDescription = variantinfode.Attribute("VShort").Value;
//                    //            }
//                    //            eventvariantlisten.Add(eventvarianten);
//                    //        }
//                    //    }
//                    //}

//                    //if(myevent.EventVariants != null)
//                    //    myevent.EventVariants.Clear();

//                    ////Adding to Dictionary
//                    //if (eventvariantlistde.Count > 0)
//                    //    myevent.EventVariants.TryAddOrUpdate("de", eventvariantlistde);
//                    //if (eventvariantlistit.Count > 0)
//                    //    myevent.EventVariants.TryAddOrUpdate("it", eventvariantlistit);
//                    //if (eventvariantlisten.Count > 0)
//                    //    myevent.EventVariants.TryAddOrUpdate("en", eventvariantlisten);

//                    //Pricing Parsing new
//                    List<EventPrice> eventpricelistde = new List<EventPrice>();
//                    List<EventPrice> eventpricelistit = new List<EventPrice>();
//                    List<EventPrice> eventpricelisten = new List<EventPrice>();

//                    var priceinfos = eventresponsede.Elements("Price");
//                    if (priceinfos != null)
//                    {
//                        foreach (var priceinfo in priceinfos)
//                        {
//                            EventPrice eventpricenew = new EventPrice();
//                            eventpricenew.Price =
//                                priceinfo.Attribute("PValue") != null
//                                    ? Convert.ToDouble(
//                                        priceinfo.Attribute("PValue").Value,
//                                        myculture
//                                    )
//                                    : 0;
//                            eventpricenew.Pstd =
//                                priceinfo.Attribute("PStd") != null
//                                    ? priceinfo.Attribute("PStd").Value
//                                    : "";
//                            eventpricenew.Type =
//                                priceinfo.Attribute("PType") != null
//                                    ? priceinfo.Attribute("PType").Value
//                                    : "";

//                            eventpricenew.VarRID = priceinfo.Attribute("VarRID").Value;
//                            eventpricenew.PriceRID = priceinfo.Attribute("I5RID").Value;

//                            //LanguageRelevant Variant
//                            var priceinfode = priceinfo.Element("PriceLng");

//                            if (priceinfode != null)
//                            {
//                                eventpricenew.Description =
//                                    priceinfode.Attribute("PDesc") != null
//                                        ? priceinfode.Attribute("PDesc").Value
//                                        : "";
//                                eventpricenew.Language = priceinfode.Attribute("LngID").Value;
//                                eventpricenew.LongDesc =
//                                    priceinfode.Attribute("PLong") != null
//                                        ? priceinfode.Attribute("PLong").Value
//                                        : "";
//                                eventpricenew.ShortDesc =
//                                    priceinfode.Attribute("PShort") != null
//                                        ? priceinfode.Attribute("PShort").Value
//                                        : "";
//                            }
//                            eventpricelistde.Add(eventpricenew);

//                            if (eventresponseit != null)
//                            {
//                                //find price with this RID //check null handling
//                                var priceinfoitgeneral = eventresponseit
//                                    .Elements("Price")
//                                    .Where(x =>
//                                        x.Attribute("I5RID").Value == eventpricenew.PriceRID
//                                    )
//                                    .FirstOrDefault();

//                                EventPrice eventpriceitnew = new EventPrice();
//                                eventpriceitnew.Price = eventpricenew.Price;
//                                eventpriceitnew.Pstd = eventpricenew.Pstd;
//                                eventpriceitnew.Type = eventpricenew.Type;
//                                eventpriceitnew.VarRID = eventpricenew.VarRID;
//                                eventpriceitnew.PriceRID = eventpricenew.PriceRID;

//                                var priceinfoit = priceinfoitgeneral.Element("PriceLng");
//                                if (priceinfoit != null)
//                                {
//                                    eventpriceitnew.Description =
//                                        priceinfoit.Attribute("PDesc") != null
//                                            ? priceinfoit.Attribute("PDesc").Value
//                                            : "";
//                                    eventpriceitnew.Language = priceinfoit.Attribute("LngID").Value;
//                                    eventpriceitnew.LongDesc =
//                                        priceinfoit.Attribute("PLong") != null
//                                            ? priceinfoit.Attribute("PLong").Value
//                                            : "";
//                                    eventpriceitnew.ShortDesc =
//                                        priceinfoit.Attribute("PShort") != null
//                                            ? priceinfoit.Attribute("PShort").Value
//                                            : "";
//                                }
//                                eventpricelistit.Add(eventpriceitnew);
//                            }
//                            if (eventresponseen != null)
//                            {
//                                //find price with this RID //check null handling
//                                var priceinfoengeneral = eventresponseen
//                                    .Elements("Price")
//                                    .Where(x =>
//                                        x.Attribute("I5RID").Value == eventpricenew.PriceRID
//                                    )
//                                    .FirstOrDefault();

//                                EventPrice eventpriceennew = new EventPrice();
//                                eventpriceennew.Price = eventpricenew.Price;
//                                eventpriceennew.Pstd = eventpricenew.Pstd;
//                                eventpriceennew.Type = eventpricenew.Type;
//                                eventpriceennew.VarRID = eventpricenew.VarRID;
//                                eventpriceennew.PriceRID = eventpricenew.PriceRID;

//                                var priceinfoen = priceinfoengeneral.Element("PriceLng");
//                                if (priceinfoen != null)
//                                {
//                                    eventpriceennew.Description =
//                                        priceinfoen.Attribute("PDesc") != null
//                                            ? priceinfoen.Attribute("PDesc").Value
//                                            : "";
//                                    eventpriceennew.Language = priceinfoen.Attribute("LngID").Value;
//                                    eventpriceennew.LongDesc =
//                                        priceinfoen.Attribute("PLong") != null
//                                            ? priceinfoen.Attribute("PLong").Value
//                                            : "";
//                                    eventpriceennew.ShortDesc =
//                                        priceinfoen.Attribute("PShort") != null
//                                            ? priceinfoen.Attribute("PShort").Value
//                                            : "";
//                                }
//                                eventpricelisten.Add(eventpriceennew);
//                            }
//                        }
//                    }

//                    if (myevent.EventPrices != null)
//                        myevent.EventPrices.Clear();

//                    //Adding to Dictionary
//                    if (eventpricelistde.Count > 0)
//                        myevent.EventPrices.TryAddOrUpdate("de", eventpricelistde);
//                    if (eventpricelistit.Count > 0)
//                        myevent.EventPrices.TryAddOrUpdate("it", eventpricelistit);
//                    if (eventpricelisten.Count > 0)
//                        myevent.EventPrices.TryAddOrUpdate("en", eventpricelisten);

//                    //        <Tags>
//                    //            <Tag RID = '…' >
//                    //                <TagLng LngID='…' Name='…' />
//                    //            </Tag>
//                    //        </Tags>

//                    //Tags
//                    var taglist = new List<LTSTagsLinked>();

//                    var tagginginfos = eventresponsede.Element("Tags");
//                    if (tagginginfos != null)
//                    {
//                        foreach (var tagginginfo in tagginginfos.Elements("Tag"))
//                        {
//                            LTSTagsLinked mytag = new LTSTagsLinked();
//                            mytag.LTSRID = tagginginfo.Attribute("RID").Value;
//                            mytag.Level = 0;

//                            var taginfode = tagginginfo.Element("TagLng");
//                            if (taginfode != null)
//                                mytag.Id = taginfode.Attribute("Name").Value;

//                            Dictionary<string, string> tagname = new Dictionary<string, string>();

//                            if (taginfode != null)
//                                tagname.TryAddOrUpdate(
//                                    taginfode.Attribute("LngID").Value,
//                                    taginfode.Attribute("Name").Value
//                                );

//                            //Search in other languages
//                            var tagginginfosit = eventresponseit.Element("Tags");
//                            if (tagginginfosit != null)
//                            {
//                                var tagginginfoit = tagginginfosit
//                                    .Elements("Tag")
//                                    .Where(x => x.Attribute("RID").Value == mytag.Id)
//                                    .FirstOrDefault();

//                                if (tagginginfoit != null)
//                                {
//                                    var taginfoit = tagginginfoit.Element("TagLng");
//                                    if (taginfoit != null)
//                                        tagname.TryAddOrUpdate(
//                                            taginfoit.Attribute("LngID").Value,
//                                            taginfoit.Attribute("Name").Value
//                                        );
//                                }
//                            }
//                            var tagginginfosen = eventresponseen.Element("Tags");
//                            if (tagginginfosen != null)
//                            {
//                                var tagginginfoen = tagginginfosen
//                                    .Elements("Tag")
//                                    .Where(x => x.Attribute("RID").Value == mytag.Id)
//                                    .FirstOrDefault();

//                                if (tagginginfoen != null)
//                                {
//                                    var taginfoen = tagginginfoen.Element("TagLng");
//                                    if (taginfoen != null)
//                                        tagname.TryAddOrUpdate(
//                                            taginfoen.Attribute("LngID").Value,
//                                            taginfoen.Attribute("Name").Value
//                                        );
//                                }
//                            }
//                            mytag.TagName = tagname;

//                            taglist.Add(mytag);
//                        }
//                    }

//                    if (myevent.LTSTags != null)
//                        myevent.LTSTags.Clear();
//                    if (taglist.Count > 0)
//                        myevent.LTSTags = taglist;

//                    //BookingInformation
//                    //<BookingData BookableFrom = "2021-01-01T00:00:00" BookableTo = "2021-08-25T23:59:00" AccommodationAssignment = "2" >
//                    //    <BookingUrl LngID = "de" Web = "https://mysuedtirol.info/de/veranstaltungen?eventid=78D925DE2CC343FA8E92184DBE98B639" />
//                    //</BookingData >
//                    EventBooking myeventbookingdata = new EventBooking();
//                    var addbookingdata = false;

//                    var bookinginfo = eventresponsede.Element("BookingData");

//                    if (bookinginfo != null)
//                    {
//                        myeventbookingdata.BookableFrom = Convert.ToDateTime(
//                            bookinginfo.Attribute("BookableFrom").Value
//                        );
//                        myeventbookingdata.BookableTo = Convert.ToDateTime(
//                            bookinginfo.Attribute("BookableTo").Value
//                        );
//                        myeventbookingdata.AccommodationAssignment =
//                            bookinginfo.Attribute("AccommodationAssignment") != null
//                                ? Convert.ToInt32(
//                                    bookinginfo.Attribute("AccommodationAssignment").Value
//                                )
//                                : (int?)null;

//                        if (bookinginfo.Elements("BookingUrl").Count() > 0)
//                        {
//                            foreach (var bookinglanginfo in bookinginfo.Elements("BookingUrl"))
//                            {
//                                EventBookingDetail bookingdetailde = new EventBookingDetail();
//                                bookingdetailde.Url = bookinglanginfo.Attribute("Web").Value;

//                                myeventbookingdata.BookingUrl.TryAddOrUpdate(
//                                    bookinglanginfo.Attribute("LngID").Value,
//                                    bookingdetailde
//                                );
//                            }
//                        }

//                        addbookingdata = true;
//                    }

//                    if (eventresponseit != null)
//                    {
//                        var bookinginfoit = eventresponseit.Element("BookingData");
//                        if (bookinginfoit != null)
//                        {
//                            if (bookinginfoit.Elements("BookingUrl").Count() > 0)
//                            {
//                                foreach (
//                                    var bookinglanginfoit in bookinginfoit.Elements("BookingUrl")
//                                )
//                                {
//                                    EventBookingDetail bookingdetailit = new EventBookingDetail();
//                                    bookingdetailit.Url = bookinglanginfoit.Attribute("Web").Value;

//                                    myeventbookingdata.BookingUrl.TryAddOrUpdate(
//                                        bookinglanginfoit.Attribute("LngID").Value,
//                                        bookingdetailit
//                                    );
//                                }
//                            }
//                        }
//                    }
//                    if (eventresponseen != null)
//                    {
//                        var bookinginfoen = eventresponseen.Element("BookingData");
//                        if (bookinginfoen != null)
//                        {
//                            if (bookinginfoen.Elements("BookingUrl").Count() > 0)
//                            {
//                                foreach (
//                                    var bookinglanginfoen in bookinginfoen.Elements("BookingUrl")
//                                )
//                                {
//                                    EventBookingDetail bookingdetailen = new EventBookingDetail();
//                                    bookingdetailen.Url = bookinglanginfoen.Attribute("Web").Value;

//                                    myeventbookingdata.BookingUrl.TryAddOrUpdate(
//                                        bookinglanginfoen.Attribute("LngID").Value,
//                                        bookingdetailen
//                                    );
//                                }
//                            }
//                        }
//                    }
//                    //Set Booking only if filled out
//                    if (addbookingdata)
//                        myevent.EventBooking = myeventbookingdata;
//                    else
//                        myevent.EventBooking = null;

//                    //Language
//                    if (String.IsNullOrEmpty(myevent.Detail["de"].Title))
//                    {
//                        if (myevent.HasLanguage != null && myevent.HasLanguage.Contains("de"))
//                            myevent.HasLanguage.Remove("de");
//                    }

//                    if (myevent.Detail.ContainsKey("it"))
//                    {
//                        if (String.IsNullOrEmpty(myevent.Detail["it"].Title))
//                        {
//                            if (myevent.HasLanguage != null && myevent.HasLanguage.Contains("it"))
//                                myevent.HasLanguage.Remove("it");
//                        }
//                    }
//                    else
//                    {
//                        if (myevent.HasLanguage != null && myevent.HasLanguage.Contains("it"))
//                            myevent.HasLanguage.Remove("it");
//                    }

//                    if (myevent.Detail.ContainsKey("en"))
//                    {
//                        if (String.IsNullOrEmpty(myevent.Detail["en"].Title))
//                        {
//                            if (myevent.HasLanguage != null && myevent.HasLanguage.Contains("en"))
//                                myevent.HasLanguage.Remove("en");
//                        }
//                    }
//                    else
//                    {
//                        if (myevent.HasLanguage != null && myevent.HasLanguage.Contains("en"))
//                            myevent.HasLanguage.Remove("en");
//                    }

//                    return myevent;
//                }
//                else
//                    return null;
//            }
//            else
//                return null;
//        }

//        //Gets the RID List Event
//        private static List<XElement> GetEventRid(XDocument myresult)
//        {
//            List<XElement> mylist = new List<XElement>();

//            foreach (var z in myresult.Root.Elements("Head"))
//            {
//                XElement myevent = new XElement("Event");
//                myevent.Add(new XAttribute("RID", z.Attribute("EvRID").Value));

//                mylist.Add(myevent);
//            }

//            return mylist;
//        }
//    }
//}
