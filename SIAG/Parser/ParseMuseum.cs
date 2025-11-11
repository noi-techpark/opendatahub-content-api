// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace SIAG.Parser
{
    public class ParseMuseum
    {
        public static ODHActivityPoiLinked ParseMuseumToPG(
            ODHActivityPoiLinked mymuseum,
            XElement mysiagmuseum,
            string plz
        )
        {
            XNamespace ax211 = "http://data.service.kks.siag/xsd";

            string adressDE = mysiagmuseum.Element(ax211 + "adresseD").Value;
            string adressIT = mysiagmuseum.Element(ax211 + "adresseI").Value;
            string adressEN = mysiagmuseum.Element(ax211 + "adresseE").Value;

            string anfahrtDE = mysiagmuseum.Element(ax211 + "anfahrtD").Value;
            string anfahrtIT = mysiagmuseum.Element(ax211 + "anfahrtI").Value;
            string anfahrtEN = mysiagmuseum.Element(ax211 + "anfahrtE").Value;

            string beschreibungDE = mysiagmuseum.Element(ax211 + "beschreibungD").Value;
            string beschreibungIT = mysiagmuseum.Element(ax211 + "beschreibungI").Value;
            string beschreibungEN = mysiagmuseum.Element(ax211 + "beschreibungE").Value;

            string bezeichnungDE = mysiagmuseum.Element(ax211 + "bezeichnungD").Value;
            string bezeichnungIT = mysiagmuseum.Element(ax211 + "bezeichnungI").Value;
            string bezeichnungEN = mysiagmuseum.Element(ax211 + "bezeichnungE").Value;

            string bezirkDE = mysiagmuseum.Element(ax211 + "bezirkD").Value;
            string bezirkIT = mysiagmuseum.Element(ax211 + "bezirkI").Value;
            string bezirkEN = mysiagmuseum.Element(ax211 + "bezirkE").Value;

            string eintrittDE = mysiagmuseum.Element(ax211 + "eintrittD").Value;
            string eintrittIT = mysiagmuseum.Element(ax211 + "eintrittI").Value;
            string eintrittEN = mysiagmuseum.Element(ax211 + "eintrittE").Value;

            string emailDE = mysiagmuseum.Element(ax211 + "emailD").Value;
            string emailIT = mysiagmuseum.Element(ax211 + "emailI").Value;

            string fax = mysiagmuseum.Element(ax211 + "fax1").Value;
            string fax2 = mysiagmuseum.Element(ax211 + "fax2").Value;
            string tel = mysiagmuseum.Element(ax211 + "telefon1").Value;
            string tel2 = mysiagmuseum.Element(ax211 + "telefon2").Value;

            string wwwDE = mysiagmuseum.Element(ax211 + "wwwD").Value;
            string wwwIT = mysiagmuseum.Element(ax211 + "wwwI").Value;

            bool freeentrance = Convert.ToBoolean(
                mysiagmuseum.Element(ax211 + "freierEintritt").Value
            );

            //bool besonderesobjekt = String.IsNullOrEmpty(mysiagmuseum.Element(ax211 + "besonderesObjekt").Value) ? false : Convert.ToBoolean(mysiagmuseum.Element(ax211 + "besonderesObjekt").Value);
            //string besonderesobjektDE = String.IsNullOrEmpty(mysiagmuseum.Element(ax211 + "besonderesObjekt").Value) ? "" : mysiagmuseum.Element(ax211 + "besonderesObjekt").Element(ax211 + "beschreibungD").Value;
            //string besonderesobjektDE = String.IsNullOrEmpty(mysiagmuseum.Element(ax211 + "besonderesObjekt").Value) ? "" : mysiagmuseum.Element(ax211 + "besonderesObjekt").Element(ax211 + "beschreibungI").Value;
            //string besonderesobjektDE = String.IsNullOrEmpty(mysiagmuseum.Element(ax211 + "besonderesObjekt").Value) ? "" : mysiagmuseum.Element(ax211 + "besonderesObjekt").Element(ax211 + "beschreibungE").Value;


            string gemeindeDE = mysiagmuseum.Element(ax211 + "gemeindeD").Value;
            string gemeindeIT = mysiagmuseum.Element(ax211 + "gemeindeI").Value;
            string gemeindeEN = mysiagmuseum.Element(ax211 + "gemeindeE").Value;

            string gemeindeid = mysiagmuseum.Element(ax211 + "gemeindeId").Value;

            string latitude = mysiagmuseum.Element(ax211 + "geocoordinateY").Value;
            string longitude = mysiagmuseum.Element(ax211 + "geocoordinateX").Value;

            string openingDE = mysiagmuseum.Element(ax211 + "oeffnungszeitenD").Value;
            string openingIT = mysiagmuseum.Element(ax211 + "oeffnungszeitenI").Value;
            string openingEN = mysiagmuseum.Element(ax211 + "oeffnungszeitenE").Value;

            List<string> museumtaglistDE = new List<string>();
            List<string> museumtaglistIT = new List<string>();
            List<string> museumtaglistEN = new List<string>();
            foreach (var museumtag in mysiagmuseum.Elements(ax211 + "museumTags"))
            {
                if(!String.IsNullOrEmpty(museumtag.Element(ax211 + "kateBezeichnungD").Value))
                    museumtaglistDE.Add(museumtag.Element(ax211 + "kateBezeichnungD").Value);
                if (!String.IsNullOrEmpty(museumtag.Element(ax211 + "kateBezeichnungI").Value)) 
                    museumtaglistIT.Add(museumtag.Element(ax211 + "kateBezeichnungI").Value);
                if (!String.IsNullOrEmpty(museumtag.Element(ax211 + "kateBezeichnungE").Value)) 
                    museumtaglistEN.Add(museumtag.Element(ax211 + "kateBezeichnungE").Value);
            }

            List<string> museumkatlistDE = new List<string>();
            List<string> museumkatlistIT = new List<string>();
            List<string> museumkatlistEN = new List<string>();
            foreach (var museumkat in mysiagmuseum.Elements(ax211 + "museumsKategorien"))
            {
                if (!String.IsNullOrEmpty(museumkat.Element(ax211 + "kateBezeichnungD").Value))
                    museumkatlistDE.Add(museumkat.Element(ax211 + "kateBezeichnungD").Value);
                if (!String.IsNullOrEmpty(museumkat.Element(ax211 + "kateBezeichnungI").Value))
                    museumkatlistIT.Add(museumkat.Element(ax211 + "kateBezeichnungI").Value);
                if (!String.IsNullOrEmpty(museumkat.Element(ax211 + "kateBezeichnungE").Value))
                    museumkatlistEN.Add(museumkat.Element(ax211 + "kateBezeichnungE").Value);
            }

            string schwerpunktDE = mysiagmuseum.Element(ax211 + "schwerpunkteD").Value;
            string schwerpunktIT = mysiagmuseum.Element(ax211 + "schwerpunkteI").Value;
            string schwerpunktEN = mysiagmuseum.Element(ax211 + "schwerpunkteE").Value;

            bool addfamilytag = false;
            bool addbarrierefreitag = false;

            List<string> museumservicelistDE = new List<string>();
            List<string> museumservicelistIT = new List<string>();
            List<string> museumservicelistEN = new List<string>();
            foreach (var museumservice in mysiagmuseum.Elements(ax211 + "services"))
            {
                if (!String.IsNullOrEmpty(museumservice.Element(ax211 + "bezeichnungD").Value))
                    museumservicelistDE.Add(museumservice.Element(ax211 + "bezeichnungD").Value);
                if (!String.IsNullOrEmpty(museumservice.Element(ax211 + "bezeichnungI").Value))
                    museumservicelistIT.Add(museumservice.Element(ax211 + "bezeichnungI").Value);
                if (!String.IsNullOrEmpty(museumservice.Element(ax211 + "bezeichnungE").Value))
                    museumservicelistEN.Add(museumservice.Element(ax211 + "bezeichnungE").Value);

                if (
                    museumservice.Element(ax211 + "bezeichnungD").Value
                    == "familienfreundliches museum"
                )
                    addfamilytag = true;

                if (museumservice.Element(ax211 + "bezeichnungD").Value == "behindertengerecht")
                    addbarrierefreitag = true;
            }

            List<string> museumtraegerlistDE = new List<string>();
            List<string> museumtraegerlistIT = new List<string>();
            List<string> museumtraegerlistEN = new List<string>();
            foreach (var museumtraeger in mysiagmuseum.Elements(ax211 + "traeger"))
            {
                if (!String.IsNullOrEmpty(museumtraeger.Element(ax211 + "kateBezeichnungD").Value))
                    museumtraegerlistDE.Add(museumtraeger.Element(ax211 + "kateBezeichnungD").Value);
                if (!String.IsNullOrEmpty(museumtraeger.Element(ax211 + "kateBezeichnungI").Value))
                    museumtraegerlistIT.Add(museumtraeger.Element(ax211 + "kateBezeichnungI").Value);
                if (!String.IsNullOrEmpty((museumtraeger.Element(ax211 + "kateBezeichnungE").Value)))
                    museumtraegerlistEN.Add(museumtraeger.Element(ax211 + "kateBezeichnungE").Value);
            }

            //Add to TagIds
            if (mymuseum.TagIds == null)
                mymuseum.TagIds = new List<string>();

            foreach(var tag in museumtaglistEN)
            {
                if(!String.IsNullOrEmpty(tag))
                    mymuseum.TagIds.Add(NormalizeTagId(tag));
            }
            foreach (var tag in museumkatlistEN)
            {
                if (!String.IsNullOrEmpty(tag))
                    mymuseum.TagIds.Add(NormalizeTagId(tag));
            }
            foreach (var tag in museumservicelistEN)
            {
                if (!String.IsNullOrEmpty(tag))
                    mymuseum.TagIds.Add(NormalizeTagId(tag));
            }
            foreach (var tag in museumtraegerlistEN)
            {
                if (!String.IsNullOrEmpty(tag))
                    mymuseum.TagIds.Add(NormalizeTagId(tag));
            }


            //Add to TagIds
            if (mymuseum.SmgTags == null)
                mymuseum.SmgTags = new List<string>();
            //Add Additional Tags
            if (mymuseum.TagIds.Contains("siagmuseum.culture"))
            {
                if(!mymuseum.SmgTags.Contains("museen kultur"))
                    mymuseum.SmgTags.Add("museen kultur");
                if(!mymuseum.TagIds.Contains("museums culture"))
                    mymuseum.TagIds.Add("museums culture");
            }
            if (mymuseum.TagIds.Contains("siagmuseum.nature"))
            {
                if (!mymuseum.SmgTags.Contains("museen natur"))
                    mymuseum.SmgTags.Add("museen natur");
                if (!mymuseum.TagIds.Contains("museums nature"))
                    mymuseum.TagIds.Add("museums nature");
            }
            if (mymuseum.TagIds.Contains("siagmuseum.technology"))
            {
                if (!mymuseum.SmgTags.Contains("museen technik"))
                    mymuseum.SmgTags.Add("museen technik");
                if (!mymuseum.TagIds.Contains("museums technology"))
                    mymuseum.TagIds.Add("museums technology");
            }
            if (mymuseum.TagIds.Contains("siagmuseum.art"))
            {
                if (!mymuseum.SmgTags.Contains("museen kunst"))
                    mymuseum.SmgTags.Add("museen kunst");
                if (!mymuseum.TagIds.Contains("museums art"))
                    mymuseum.TagIds.Add("museums art");
            }
            if (mymuseum.TagIds.Contains("siagmuseum.mine"))
            {
                if (!mymuseum.SmgTags.Contains("bergwerke"))
                    mymuseum.SmgTags.Add("bergwerke");
                if (!mymuseum.TagIds.Contains("mines"))
                    mymuseum.TagIds.Add("mines");
            }
            if (mymuseum.TagIds.Contains("siagmuseum.natureparks"))
            {
                if (!mymuseum.SmgTags.Contains("naturparkhäuser"))
                    mymuseum.SmgTags.Add("naturparkhäuser");
                if (!mymuseum.TagIds.Contains("nature park visitors centres"))
                    mymuseum.TagIds.Add("nature park visitors centres");
            }

            List<ImageGallery> imagegallerylist = new List<ImageGallery>();

            foreach (var photogallery in mysiagmuseum.Elements(ax211 + "photoGallery"))
            {
                string imagedescDE = photogallery.Element(ax211 + "beschreibungD").Value;
                string imagedescIT = photogallery.Element(ax211 + "beschreibungI").Value;
                string imagedescEN = photogallery.Element(ax211 + "beschreibungE").Value;
                string filename = photogallery.Element(ax211 + "filename").Value;
                string sortierung = photogallery.Element(ax211 + "sortierung").Value;
                string titleDE = photogallery.Element(ax211 + "titelD").Value;
                string titleIT = photogallery.Element(ax211 + "titelI").Value;
                string titleEN = photogallery.Element(ax211 + "titelE").Value;
                string resourceid = photogallery.Element(ax211 + "resoId").Value;

                ImageGallery myimage = new ImageGallery();
                myimage.ImageSource = "siag";
                myimage.ImageUrl =
                    "https://musport.prov.bz.it/musport/servlet/resource?id=" + resourceid;
                myimage.ImageTitle["de"] = titleDE;
                myimage.ImageTitle["it"] = titleIT;
                myimage.ImageTitle["en"] = titleEN;
                myimage.ImageDesc["de"] = imagedescDE;
                myimage.ImageDesc["it"] = imagedescIT;
                myimage.ImageDesc["en"] = imagedescEN;
                myimage.ListPosition = Convert.ToInt32(sortierung);
                myimage.ImageName = filename;
                myimage.IsInGallery = true;
                myimage.ValidFrom = new DateTime(2000, 1, 1);
                myimage.ValidTo = new DateTime(2000, 12, 31);

                imagegallerylist.Add(myimage);
            }

            

            //Contactinfos
            ContactInfos contactinfode = new ContactInfos();
            contactinfode.Address = adressDE;
            contactinfode.City = gemeindeDE;
            contactinfode.CountryCode = "IT";
            contactinfode.CountryName = "Italien";
            contactinfode.Email = emailDE;
            contactinfode.Faxnumber = fax;
            contactinfode.Phonenumber = tel;

            string webadresseDE = "";
            //Webadress gschicht
            if (!String.IsNullOrEmpty(wwwDE))
            {
                webadresseDE = wwwDE.Contains("http") ? wwwDE : "http://" + wwwDE;
            }

            contactinfode.Url = webadresseDE;
            contactinfode.CompanyName = bezeichnungDE;
            contactinfode.ZipCode = plz;
            contactinfode.Language = "de";

            mymuseum.ContactInfos.TryAddOrUpdate("de", contactinfode);

            ContactInfos contactinfoit = new ContactInfos();
            contactinfoit.Address = adressIT;
            contactinfoit.City = gemeindeIT;
            contactinfoit.CountryCode = "IT";
            contactinfoit.CountryName = "Italia";
            contactinfoit.Email = emailIT;
            contactinfoit.Faxnumber = fax;
            contactinfoit.Phonenumber = tel;

            string webadresseIT = "";
            //Webadress gschicht
            if (!String.IsNullOrEmpty(wwwIT))
            {
                webadresseIT = wwwIT.Contains("http") ? wwwIT : "http://" + wwwIT;
            }
            else
            {
                webadresseIT = webadresseDE;
            }

            contactinfoit.Url = webadresseIT;
            contactinfoit.CompanyName = bezeichnungIT;
            contactinfoit.ZipCode = plz;
            contactinfoit.Language = "it";

            mymuseum.ContactInfos.TryAddOrUpdate("it", contactinfoit);

            ContactInfos contactinfoen = new ContactInfos();
            contactinfoen.Address = adressEN;
            contactinfoen.City = gemeindeEN;
            contactinfoen.CountryCode = "IT";
            contactinfoen.CountryName = "Italy";
            contactinfoen.Email = emailIT;
            contactinfoen.Faxnumber = fax;
            contactinfoen.Phonenumber = tel;
            contactinfoen.Url = webadresseIT;
            contactinfoen.CompanyName = bezeichnungEN;
            contactinfoen.ZipCode = plz;
            contactinfoen.Language = "en";

            mymuseum.ContactInfos.TryAddOrUpdate("en", contactinfoen);

            //Detail
            Detail detailde = new Detail();
            if (mymuseum.Detail != null)
                if (mymuseum.Detail.ContainsKey("de"))
                    detailde = mymuseum.Detail["de"];

            detailde.BaseText = String.IsNullOrEmpty(beschreibungDE) ? null : beschreibungDE;
            detailde.GetThereText = String.IsNullOrEmpty(anfahrtDE) ? null : anfahrtDE;
            detailde.Title = String.IsNullOrEmpty(bezeichnungDE) ? null : bezeichnungDE;
            detailde.Language = "de";

            mymuseum.Detail.TryAddOrUpdate("de", detailde);

            Detail detailit = new Detail();
            if (mymuseum.Detail != null)
                if (mymuseum.Detail.ContainsKey("it"))
                    detailit = mymuseum.Detail["it"];

            detailit.BaseText = String.IsNullOrEmpty(beschreibungDE) ? null : beschreibungIT;
            detailit.GetThereText = String.IsNullOrEmpty(beschreibungDE) ? null : anfahrtIT;
            detailit.Title = String.IsNullOrEmpty(beschreibungDE) ? null : bezeichnungIT;
            detailit.Language = "it";

            mymuseum.Detail.TryAddOrUpdate("it", detailit);

            Detail detailen = new Detail();
            if (mymuseum.Detail != null)
                if (mymuseum.Detail.ContainsKey("en"))
                    detailen = mymuseum.Detail["en"];

            detailen.BaseText = String.IsNullOrEmpty(beschreibungEN) ? null : beschreibungEN;
            detailen.GetThereText = String.IsNullOrEmpty(anfahrtEN) ? null : anfahrtEN;
            detailen.Title = String.IsNullOrEmpty(bezeichnungEN) ? null : bezeichnungEN;
            detailen.Language = "en";

            //HAck no english name
            if (String.IsNullOrEmpty(detailen.Title))
                detailen.Title = detailde.Title;

            mymuseum.Detail.TryAddOrUpdate("en", detailen);

            if (!String.IsNullOrEmpty(latitude) && !String.IsNullOrEmpty(longitude))
            {
                List<GpsInfo> mygpsinfos = new List<GpsInfo>();

                //GPS
                GpsInfo gps = new GpsInfo();
                gps.Gpstype = "position";
                gps.Latitude = Convert.ToDouble(latitude, CultureInfo.InvariantCulture);
                gps.Longitude = Convert.ToDouble(longitude, CultureInfo.InvariantCulture);
                gps.Altitude = 0;
                gps.AltitudeUnitofMeasure = "m";

                //if(mymuseum.GpsInfo != null)
                //    mymuseum.GpsInfo.Clear();

                mygpsinfos.Add(gps);
                mymuseum.GpsInfo = mygpsinfos.ToList();
            }
            //Eigenschaften
            mymuseum.HasFreeEntrance = freeentrance;
            mymuseum.Highlight = null;

            List<PoiProperty> poipropertylistde = new List<PoiProperty>();
            List<PoiProperty> poipropertylistit = new List<PoiProperty>();
            List<PoiProperty> poipropertylisten = new List<PoiProperty>();

            //Öffnungszeiten
            PoiProperty mypropertyopeningde = new PoiProperty();
            //mypropertyopeningde.Name = "Öffnungszeiten";
            mypropertyopeningde.Name = "openingtimes";
            mypropertyopeningde.Value = openingDE;
            poipropertylistde.Add(mypropertyopeningde);

            PoiProperty mypropertyopeningit = new PoiProperty();
            //mypropertyopeningit.Name = "Orari d’apertura";
            mypropertyopeningit.Name = "openingtimes";
            mypropertyopeningit.Value = openingIT;
            poipropertylistit.Add(mypropertyopeningit);

            PoiProperty mypropertyopeningen = new PoiProperty();
            //mypropertyopeningen.Name = "Opening times";
            mypropertyopeningen.Name = "openingtimes";
            mypropertyopeningen.Value = openingEN;
            poipropertylisten.Add(mypropertyopeningen);

            //Eintritt
            PoiProperty mypropertyeintrittde = new PoiProperty();
            //mypropertyeintrittde.Name = "Eintritt";
            mypropertyeintrittde.Name = "entry";
            mypropertyeintrittde.Value = eintrittDE;
            poipropertylistde.Add(mypropertyeintrittde);

            PoiProperty mypropertyeintrittit = new PoiProperty();
            //mypropertyeintrittit.Name = "Biglietto d’ingresso";
            mypropertyeintrittit.Name = "entry";
            mypropertyeintrittit.Value = eintrittIT;
            poipropertylistit.Add(mypropertyeintrittit);

            PoiProperty mypropertyeintritten = new PoiProperty();
            //mypropertyeintritten.Name = "Admission ticket";
            mypropertyeintritten.Name = "entry";
            mypropertyeintritten.Value = eintrittEN;
            poipropertylisten.Add(mypropertyeintritten);

            //Tags
            PoiProperty mypropertytagsde = new PoiProperty();
            mypropertytagsde.Name = "tags";
            mypropertytagsde.Value = String.Join(", ", museumtaglistDE.ToArray());
            poipropertylistde.Add(mypropertytagsde);

            PoiProperty mypropertytagsit = new PoiProperty();
            mypropertytagsit.Name = "tags";
            mypropertytagsit.Value = String.Join(", ", museumtaglistIT.ToArray());
            poipropertylistit.Add(mypropertytagsit);

            PoiProperty mypropertytagsen = new PoiProperty();
            mypropertytagsen.Name = "tags";
            mypropertytagsen.Value = String.Join(", ", museumtaglistEN.ToArray());
            poipropertylisten.Add(mypropertytagsen);

            //Kategorien
            PoiProperty mypropertykategoriesde = new PoiProperty();
            //mypropertykategoriesde.Name = "Kategorien";
            mypropertykategoriesde.Name = "categories";
            mypropertykategoriesde.Value = String.Join(", ", museumkatlistDE.ToArray());
            poipropertylistde.Add(mypropertykategoriesde);

            PoiProperty mypropertykategoriesit = new PoiProperty();
            //mypropertykategoriesit.Name = "Categorie";
            mypropertykategoriesit.Name = "categories";
            mypropertykategoriesit.Value = String.Join(", ", museumkatlistIT.ToArray());
            poipropertylistit.Add(mypropertykategoriesit);

            PoiProperty mypropertykategoriesen = new PoiProperty();
            //mypropertykategoriesen.Name = "Categories";
            mypropertykategoriesen.Name = "categories";
            mypropertykategoriesen.Value = String.Join(", ", museumkatlistEN.ToArray());
            poipropertylisten.Add(mypropertykategoriesen);

            //Service
            PoiProperty mypropertyservicede = new PoiProperty();
            mypropertyservicede.Name = "service";
            mypropertyservicede.Value = String.Join(", ", museumservicelistDE.ToArray());
            poipropertylistde.Add(mypropertyservicede);

            PoiProperty mypropertyserviceit = new PoiProperty();
            //mypropertyserviceit.Name = "Servizi";
            mypropertyserviceit.Name = "service";
            mypropertyserviceit.Value = String.Join(", ", museumservicelistIT.ToArray());
            poipropertylistit.Add(mypropertyserviceit);

            PoiProperty mypropertyserviceen = new PoiProperty();
            //mypropertyserviceen.Name = "Services";
            mypropertyserviceen.Name = "service";
            mypropertyserviceen.Value = String.Join(", ", museumservicelistEN.ToArray());
            poipropertylisten.Add(mypropertyserviceen);

            //Träger
            PoiProperty mypropertytraegerde = new PoiProperty();
            //mypropertytraegerde.Name = "Träger";
            mypropertytraegerde.Name = "supporter";
            mypropertytraegerde.Value = String.Join(", ", museumtraegerlistDE.ToArray());
            poipropertylistde.Add(mypropertytraegerde);

            PoiProperty mypropertytraegerit = new PoiProperty();
            //mypropertytraegerit.Name = "Träger";
            mypropertytraegerit.Name = "supporter";
            mypropertytraegerit.Value = String.Join(", ", museumtraegerlistIT.ToArray());
            poipropertylistit.Add(mypropertytraegerit);

            PoiProperty mypropertytraegeren = new PoiProperty();
            //mypropertytraegeren.Name = "Träger";
            mypropertytraegeren.Name = "supporter";
            mypropertytraegeren.Value = String.Join(", ", museumtraegerlistEN.ToArray());
            poipropertylisten.Add(mypropertytraegeren);

            mymuseum.PoiProperty.TryAddOrUpdate("de", poipropertylistde);
            mymuseum.PoiProperty.TryAddOrUpdate("it", poipropertylistit);
            mymuseum.PoiProperty.TryAddOrUpdate("en", poipropertylisten);

            //Restliche Sprachen? Clearen.....
            if (mymuseum.PoiProperty.ContainsKey("nl"))
                mymuseum.PoiProperty["nl"].Clear();
            if (mymuseum.PoiProperty.ContainsKey("cs"))
                mymuseum.PoiProperty["cs"].Clear();
            if (mymuseum.PoiProperty.ContainsKey("pl"))
                mymuseum.PoiProperty["pl"].Clear();

            //Services
            //mymuseum.PoiServices = museumservicelistDE.ToList();

            //ImageGallery
            var smgimages = default(ICollection<ImageGallery>);

            if (mymuseum.ImageGallery != null)
                smgimages = mymuseum.ImageGallery.Where(x => x.ImageSource == "idm").ToList();

            var imagelistfull = new List<ImageGallery>();

            imagelistfull.AddRange(imagegallerylist);
            if (smgimages != null)
                imagelistfull.AddRange(smgimages);

            if (mymuseum.ImageGallery != null)
                mymuseum.ImageGallery.Clear();

            mymuseum.ImageGallery = imagelistfull.ToList();

            mymuseum.LastChange = DateTime.Now;
                    
            if (addbarrierefreitag)
            {
                if (!mymuseum.SmgTags.Contains("barrierefrei"))
                    mymuseum.SmgTags.Add("barrierefrei");
                if (!mymuseum.TagIds.Contains("barrierfree"))
                    mymuseum.TagIds.Add("barrierfree");
            }
            if (addfamilytag)
            {
                if (!mymuseum.SmgTags.Contains("familientip"))
                    mymuseum.SmgTags.Add("familientip");                
            }

            mymuseum.Type = null;
            mymuseum.SubType = null;
            mymuseum.PoiType = null;
            mymuseum.AgeFrom = null;
            mymuseum.AgeTo = null;
            mymuseum.AltitudeDifference = null;
            mymuseum.MaxSeatingCapacity = null;
            mymuseum.AltitudeLowestPoint = null;
            mymuseum.AltitudeHighestPoint = null;
            mymuseum.DistanceLength = null;
            mymuseum.DistanceDuration = null;
            mymuseum.AltitudeSumUp = null;
            mymuseum.AltitudeSumDown = null;

            return mymuseum;
        }

        public static IEnumerable<TagLinked> ParseSiagResponseToTags(IEnumerable<XElement> mymuseumxml)
        {
            XNamespace ax211 = "http://data.service.kks.siag/xsd";

            Dictionary<string, Dictionary<string,string>> tagdict = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> catdict = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> servicedict = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> traegerdict = new Dictionary<string, Dictionary<string, string>>();

            foreach (var mysiagmuseum in mymuseumxml)
            {
                foreach (var museumtag in mysiagmuseum.Elements(ax211 + "museumTags"))
                {
                    if(!String.IsNullOrEmpty(museumtag.Element(ax211 + "kateId").Value) && !tagdict.ContainsKey(museumtag.Element(ax211 + "kateId").Value))
                    {
                        var tagkey = museumtag.Element(ax211 + "kateId").Value;
                        var tagvalue = new Dictionary<string, string>();
                        tagvalue.Add("de", museumtag.Element(ax211 + "kateBezeichnungD").Value);
                        tagvalue.Add("it", museumtag.Element(ax211 + "kateBezeichnungI").Value);
                        tagvalue.Add("en", museumtag.Element(ax211 + "kateBezeichnungE").Value);

                        tagdict.Add(tagkey, tagvalue);
                    }    
                }

                foreach (var museumkat in mysiagmuseum.Elements(ax211 + "museumsKategorien"))
                {
                    if (!String.IsNullOrEmpty(museumkat.Element(ax211 + "kateId").Value) && !catdict.ContainsKey(museumkat.Element(ax211 + "kateId").Value))
                    {
                        var tagkey = museumkat.Element(ax211 + "kateId").Value;
                        var tagvalue = new Dictionary<string, string>();
                        tagvalue.Add("de", museumkat.Element(ax211 + "kateBezeichnungD").Value);
                        tagvalue.Add("it", museumkat.Element(ax211 + "kateBezeichnungI").Value);
                        tagvalue.Add("en", museumkat.Element(ax211 + "kateBezeichnungE").Value);
                        
                        catdict.Add(tagkey, tagvalue);
                    }
                }


                foreach (var museumservice in mysiagmuseum.Elements(ax211 + "services"))
                {
                    if (!String.IsNullOrEmpty(museumservice.Element(ax211 + "servId").Value) && !servicedict.ContainsKey(museumservice.Element(ax211 + "servId").Value))
                    {
                        var tagkey = museumservice.Element(ax211 + "servId").Value;
                        var tagvalue = new Dictionary<string, string>();
                        tagvalue.Add("de", museumservice.Element(ax211 + "bezeichnungD").Value);
                        tagvalue.Add("it", museumservice.Element(ax211 + "bezeichnungI").Value);
                        tagvalue.Add("en", museumservice.Element(ax211 + "bezeichnungE").Value);

                        servicedict.Add(tagkey, tagvalue);
                    }
                }

                foreach (var museumtraeger in mysiagmuseum.Elements(ax211 + "traeger"))
                {
                    if (!String.IsNullOrEmpty(museumtraeger.Element(ax211 + "kateId").Value) && !traegerdict.ContainsKey(museumtraeger.Element(ax211 + "kateId").Value))
                    {
                        var tagkey = museumtraeger.Element(ax211 + "kateId").Value;
                        var tagvalue = new Dictionary<string, string>();
                        tagvalue.Add("de", museumtraeger.Element(ax211 + "kateBezeichnungD").Value);
                        tagvalue.Add("it", museumtraeger.Element(ax211 + "kateBezeichnungI").Value);
                        tagvalue.Add("en", museumtraeger.Element(ax211 + "kateBezeichnungE").Value);

                        traegerdict.Add(tagkey, tagvalue);
                    }
                }
            }

            List<TagLinked> taglinkedlist = new List<TagLinked>();

            foreach(var tag in tagdict)
            {
                if (!String.IsNullOrEmpty(tag.Value["en"]))
                {

                    TagLinked taglinked = new TagLinked();
                    taglinked.Source = "siag";
                    taglinked.Id = NormalizeTagId(tag.Value["en"]);
                    taglinked.Types = new List<string>() { "museumtag" };
                    taglinked.TagName = tag.Value;
                    taglinked.Active = true;
                    taglinked.DisplayAsCategory = false;
                    taglinked.MainEntity = "odhactivitypoi";
                    taglinked.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    taglinked.Mapping.Add("siag", new Dictionary<string, string>() { { "kateId", tag.Key } });

                    taglinkedlist.Add(taglinked);
                }
            }

            foreach (var cat in catdict)
            {
                if (!String.IsNullOrEmpty(cat.Value["en"]))
                {
                    TagLinked taglinked = new TagLinked();
                    taglinked.Source = "siag";
                    taglinked.Id = NormalizeTagId(cat.Value["en"]);
                    taglinked.Types = new List<string>() { "museumcategory" };
                    taglinked.TagName = cat.Value;
                    taglinked.Active = true;
                    taglinked.DisplayAsCategory = false;
                    taglinked.MainEntity = "odhactivitypoi";
                    taglinked.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    taglinked.Mapping.Add("siag", new Dictionary<string, string>() { { "kateId", cat.Key } });

                    taglinkedlist.Add(taglinked);
                }
            }

            foreach (var service in servicedict)
            {
                if (!String.IsNullOrEmpty(service.Value["en"]))
                {
                    TagLinked taglinked = new TagLinked();
                    taglinked.Source = "siag";
                    taglinked.Id = NormalizeTagId(service.Value["en"]);
                    taglinked.Types = new List<string>() { "museumservice" };
                    taglinked.TagName = service.Value;
                    taglinked.Active = true;
                    taglinked.DisplayAsCategory = false;
                    taglinked.MainEntity = "odhactivitypoi";
                    taglinked.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    taglinked.Mapping.Add("siag", new Dictionary<string, string>() { { "servId", service.Key } });

                    taglinkedlist.Add(taglinked);
                }
                //Hack where no en title is associated
                else if(service.Key == "8" || service.Key == "10")
                {
                    //8 Familienfreundliches Museum (offers for children)
                    //10 Vermittlungsangebote für Schulen (offers for schools)

                    TagLinked taglinked = new TagLinked();
                    taglinked.Source = "siag";
                    taglinked.TagName = service.Value;

                    if (service.Key == "8")
                    {
                        taglinked.Id = NormalizeTagId("offers for children");
                        taglinked.TagName["en"] = "Offers for children";
                    }
                        
                    if (service.Key == "10")
                    {
                        taglinked.Id = NormalizeTagId("offers for schools");
                        taglinked.TagName["en"] = "Offers for schools";
                    }                        

                    taglinked.Types = new List<string>() { "museumservice" };
                    taglinked.Active = true;
                    taglinked.DisplayAsCategory = false;
                    taglinked.MainEntity = "odhactivitypoi";
                    taglinked.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    taglinked.Mapping.Add("siag", new Dictionary<string, string>() { { "servId", service.Key } });

                    taglinkedlist.Add(taglinked);

                }
            }

            foreach (var traeger in traegerdict)
            {
                if (!String.IsNullOrEmpty(traeger.Value["en"]))
                {
                    TagLinked taglinked = new TagLinked();
                    taglinked.Source = "siag";
                    taglinked.Id = NormalizeTagId(traeger.Value["en"]);
                    taglinked.Types = new List<string>() { "museumsupporter" };
                    taglinked.TagName = traeger.Value;
                    taglinked.Active = true;
                    taglinked.DisplayAsCategory = false;
                    taglinked.MainEntity = "odhactivitypoi";
                    taglinked.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    taglinked.Mapping.Add("siag", new Dictionary<string, string>() { { "servId", traeger.Key } });

                    taglinkedlist.Add(taglinked);
                }
                //Hack where no en title is associated
                else if(traeger.Key == "236")
                {
                    //236 Land
                    TagLinked taglinked = new TagLinked();
                    taglinked.Source = "siag";                    
                    taglinked.Types = new List<string>() { "museumsupporter" };
                    taglinked.TagName = traeger.Value;

                    if (traeger.Key == "236")
                    {
                        taglinked.Id = NormalizeTagId("province");
                        taglinked.TagName["en"] = "Province";
                    }

                    taglinked.Active = true;
                    taglinked.DisplayAsCategory = false;
                    taglinked.MainEntity = "odhactivitypoi";
                    taglinked.ValidForEntity = new List<string>() { "odhactivitypoi" };
                    taglinked.Mapping.Add("siag", new Dictionary<string, string>() { { "servId", traeger.Key } });

                    taglinkedlist.Add(taglinked);
                }
            }

            return taglinkedlist;
        }

        public static string NormalizeTagId(string tagkeyen)
        {
            return "siag:museum:" + tagkeyen.ToLower()
                .Replace(" ", "")
                .Replace("ä", "ae")
                .Replace("ö", "oe")
                .Replace("ü", "ue")
                .Replace("ß", "ss");
        }
    }
}
