// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataModel;
using JsonLDTransformer.Objects;

namespace JsonLDTransformer
{
    public static class TransformToLD
    {
        [Obsolete]
        public static List<object> TransformDataToLD<T>(T data, string language, string idtoshow)
        {
            var objectlist = new List<object>();

            var z = data.GetType();

            switch (z.Name)
            {
                case "Accommodation":
                    objectlist.Add(
                        TransformAccommodationToLD((Accommodation)(object)data, language)
                    );
                    break;
                case "Gastronomy":
                    objectlist.Add(TransformGastronomyToLD((ODHActivityPoi)(object)data, language));
                    break;
                case "Event":

                    //Achtung pro EventDate Eintrag 1 Event anlegen
                    //des hoasst i muass a listen zruggeben


                    objectlist.AddRange(TransformEventToLD((Event)(object)data, language));
                    break;
            }

            return objectlist;
        }

        public static HotelLD TransformAccommodationToLD(Accommodation acco, string language)
        {
            HotelLD myhotel = new HotelLD();

            myhotel.context = "http://schema.org";
            myhotel.id = acco.Id;
            myhotel.type = "Hotel";

            myhotel.description = acco.AccoDetail[language].Shortdesc;
            myhotel.email = acco.AccoDetail[language].Email;
            var image = acco.ImageGallery.FirstOrDefault();
            if (image != null)
                myhotel.image = image.ImageUrl + "&W=150&H=150";
            myhotel.name = acco.AccoDetail[language].Name;
            myhotel.telephone = acco.AccoDetail[language].Phone;
            myhotel.url = acco.AccoDetail[language].Website;

            addressLD myaddress = new addressLD();
            myaddress.type = "http://schema.org/PostalAddress";
            myaddress.streetAddress = acco.AccoDetail[language].Street;
            myaddress.postalCode = acco.AccoDetail[language].Zip;
            myaddress.addressLocality = acco.AccoDetail[language].City;
            myaddress.addressRegion = "Südtirol";
            myaddress.addressCountry = "Italy";

            myhotel.address = myaddress;

            geoLD mygeo = new geoLD();
            mygeo.type = "http://schema.org/GeoCoordinates";
            mygeo.latitude = acco.Latitude.GetValueOrDefault(0);
            mygeo.longitude = acco.Longitude.GetValueOrDefault(0);

            myhotel.geo = mygeo;

            return myhotel;
        }

        public static RestaurantLD TransformGastronomyToLD(ODHActivityPoi gastro, string language)
        {
            RestaurantLD mygastro = new RestaurantLD();

            mygastro.context = "http://schema.org";
            mygastro.id = gastro.Id;
            mygastro.type = "Restaurant";

            mygastro.description = gastro.Detail[language].BaseText;
            mygastro.email = gastro.ContactInfos[language].Email;
            var image = gastro.ImageGallery.FirstOrDefault();
            if (image != null)
                mygastro.image = image.ImageUrl + "&W=150&H=150";
            mygastro.name = gastro.Detail[language].Title;
            mygastro.telephone = gastro.ContactInfos[language].Phonenumber;
            mygastro.url = gastro.ContactInfos[language].Url;

            addressLD myaddress = new addressLD();
            myaddress.type = "http://schema.org/PostalAddress";
            myaddress.streetAddress = gastro.ContactInfos[language].Address;
            myaddress.postalCode = gastro.ContactInfos[language].ZipCode;
            myaddress.addressLocality = gastro.ContactInfos[language].City;
            myaddress.addressRegion = "Südtirol";
            myaddress.addressCountry = gastro.ContactInfos[language].CountryName;

            mygastro.address = myaddress;

            geoLD mygeo = new geoLD();
            mygeo.type = "http://schema.org/GeoCoordinates";
            mygeo.latitude = gastro.GpsInfo.Where(x => x.Gpstype == "position").Count() > 0 ?
                gastro.GpsInfo.Where(x => x.Gpstype == "position").FirstOrDefault().Latitude.GetValueOrDefault(0) : 0;
            mygeo.longitude = gastro.GpsInfo.Where(x => x.Gpstype == "position").Count() > 0 ?
                gastro.GpsInfo.Where(x => x.Gpstype == "position").FirstOrDefault().Longitude.GetValueOrDefault(0) : 0;

            mygastro.geo = mygeo;

            personLD founder = new personLD();
            founder.type = "http://schema.org/Person";
            founder.name =
                gastro.ContactInfos["de"].Givenname + " " + gastro.ContactInfos[language].Surname;

            mygastro.founder = founder;

            mygastro.servesCuisine = GetCuisine(gastro.Facilities);

            mygastro.openingHours = GetOpeningDates(gastro.OperationSchedule);

            return mygastro;
        }

        private static List<string> GetCuisine(ICollection<Facilities> facilities)
        {
            var validcuisine = ValidCuisineCodes();

            return facilities
                .Where(x => validcuisine.Contains(x.Id))
                .Select(x => x.Shortname)
                .ToList();
        }

        private static List<string> ValidCuisineCodes()
        {
            return new List<string>()
            {
                "3091F5B92F534F67986C08151E6F4454",
                "71A7D4A821F7437EA1DC05CEE9655A5A",
                "11A6BEA7EEFC4716BDF8FBD5E15C0CFB",
                "A469B187953944A0AF49C5EBE13DCF00",
                "F42DBD202D6E4289AF48D138DA09ECB7",
                "2476B5BBAEB7467C9A0099F06D0ED004",
                "30DC854F943D42CF8DB140CF4A90EC7E",
                "D1F124A123554B14AB9600F2313ED051",
                "CB8AF7CB80E844758B18E9C4E2D84035",
                "50FFF83EB75944DE9F6F15CC51E85E7A",
                "6322DE8AFE8E406F886E7C40D0DC1ADD",
                "0E9721E540FB4D84BADC0DFA24F0543B",
                "C48E7E7679B04835B6744650E129BABF",
                "167850CF26984D50A59A5F42EB24A0AD",
                "4F9335FDAB834B11B36CD4C163F990A7",
                "69621AE51DF942A1BBED32D460E65132",
                "22F0D9C42B06423EB63E1F2F27B7CA3A",
                "BC08B00995564BB28997C55C870120D1",
                "0E55D7C2A7BC4866BF8438C522C17254",
                "FC627623C6994E37927F6048E32B79C2",
                "21A903DE35654070803DFDDF29C67291",
                "8E28215F82BA430EA016BA5D1C776A30",
                "D413EF912D18462CA0055A44F55351D1",
                "BC6B57D90AFB496098DD0D059D04EE7C",
                "B36D855D60CB4D79BA78F3FEFEE9F9D3",
                "AD8426538FCF4D8A81E06BE044088BAA",
                "5C84265DA5F84F84A7896808ACCB675A",
            };
        }

        private static List<string> GetOpeningDates(
            ICollection<OperationSchedule> operationschedules
        )
        {
            List<string> openingtime = new List<string>();
            //openingtime.Add("Mo-Sa 11:00-14:30");
            //openingtime.Add("Mo-Th 17:00-21:30");

            foreach (
                var operationschedule in operationschedules.Where(x =>
                    x.Start <= DateTime.Now && x.Stop >= DateTime.Now
                )
            )
            {
                //Zersch schaugn obs ollaweil offen isch!



                //Wenn Zeiten drinn

                //Achtung do muassi die richtigen fir heint finden sischt sein mearere!

                if (operationschedule.OperationScheduleTime != null)
                {
                    foreach (
                        var operationtime in operationschedule.OperationScheduleTime.Where(x =>
                            x.State == 2 && x.Timecode == 1
                        )
                    )
                    {
                        if (operationtime.Monday)
                            openingtime.Add(
                                "Mo " + operationtime.Start + " - " + operationtime.End
                            );
                        if (operationtime.Tuesday)
                            openingtime.Add(
                                "Tu " + operationtime.Start + " - " + operationtime.End
                            );
                        if (operationtime.Wednesday)
                            openingtime.Add(
                                "We " + operationtime.Start + " - " + operationtime.End
                            );
                        if (operationtime.Thuresday)
                            openingtime.Add(
                                "Th " + operationtime.Start + " - " + operationtime.End
                            );
                        if (operationtime.Friday)
                            openingtime.Add(
                                "Fr " + operationtime.Start + " - " + operationtime.End
                            );
                        if (operationtime.Saturday)
                            openingtime.Add(
                                "Sa " + operationtime.Start + " - " + operationtime.End
                            );
                        if (operationtime.Sunday)
                            openingtime.Add(
                                "Su " + operationtime.Start + " - " + operationtime.End
                            );
                    }
                }

                //Keine Zeiten drinn
            }

            return openingtime;
        }

        public static List<EventLD> TransformEventToLD(Event theevent, string language)
        {
            List<EventLD> myeventlist = new List<EventLD>();

            foreach (var theeventsingle in theevent.EventDate)
            {
                EventLD myevent = new EventLD();

                myevent.context = "http://schema.org";
                myevent.id = theevent.Id;
                myevent.type = "Event";

                myevent.description = theevent.Detail[language].BaseText;
                myevent.name = theevent.Detail[language].Title;
                var image = theevent.ImageGallery.FirstOrDefault();
                if (image != null)
                    myevent.image = image.ImageUrl + "&W=150&H=150";

                myevent.url = theevent.ContactInfos[language].Url;
                //myevent.email = theevent.ContactInfos[language].Email;
                //myevent.telephone = theevent.ContactInfos[language].Phonenumber;

                addressLD myaddress = new addressLD();
                myaddress.type = "http://schema.org/PostalAddress";
                myaddress.streetAddress = theevent.ContactInfos["de"].Address;
                myaddress.postalCode = theevent.ContactInfos["de"].ZipCode;
                myaddress.addressLocality = theevent.ContactInfos["de"].City;
                myaddress.addressRegion = "Südtirol";
                myaddress.addressCountry = theevent.ContactInfos["de"].CountryName;

                PlaceLD location = new PlaceLD();
                location.address = myaddress;
                location.type = "Place";
                location.name = theevent.ContactInfos["de"].CompanyName;
                myevent.location = location;

                OrganizationLD organization = new OrganizationLD();

                organization.id = "http://www.suedtirol.info";
                organization.type = "http://schema.org/Organization";
                organization.context = "http://schema.org";

                organization.name = theevent.OrganizerInfos[language].CompanyName;
                organization.url = theevent.OrganizerInfos[language].Url;
                organization.email = theevent.OrganizerInfos[language].Email;
                organization.telephone = theevent.OrganizerInfos[language].Phonenumber;
                addressLD organizeraddress = new addressLD();
                organizeraddress.type = "http://schema.org/PostalAddress";
                organizeraddress.streetAddress = theevent.OrganizerInfos["de"].Address;
                organizeraddress.postalCode = theevent.OrganizerInfos["de"].ZipCode;
                organizeraddress.addressLocality = theevent.OrganizerInfos["de"].City;
                organizeraddress.addressRegion = "Südtirol";
                organizeraddress.addressCountry = theevent.OrganizerInfos["de"].CountryName;
                organization.address = organizeraddress;
                myevent.organizer = organization;

                //OFfers

                if (theevent.EventPrice != null)
                {
                    if (theevent.EventPrice.ContainsKey(language))
                    {
                        OfferLD myoffer = new OfferLD();
                        myoffer.type = "http://schema.org/Offer";

                        myoffer.price = theevent.EventPrice[language].Price.ToString();
                        myoffer.priceCurrency = "€";
                        myoffer.name = theevent.EventPrice[language].ShortDesc;
                        myoffer.description = theevent.EventPrice[language].Description;

                        myoffer.validFrom =
                            String.Format("{0:yyyy-MM-dd}", theeventsingle.From)
                            + "T"
                            + theeventsingle.Begin.Value.ToString("hh\\:mm");
                        myoffer.validTrough =
                            String.Format("{0:yyyy-MM-dd}", theeventsingle.To)
                            + "T"
                            + theeventsingle.End.Value.ToString("hh\\:mm");

                        myevent.offers = myoffer;
                    }
                }

                myevent.startDate =
                    String.Format("{0:yyyy-MM-dd}", theeventsingle.From)
                    + "T"
                    + theeventsingle.Begin.Value.ToString("hh\\:mm");
                myevent.endDate =
                    String.Format("{0:yyyy-MM-dd}", theeventsingle.To)
                    + "T"
                    + theeventsingle.End.Value.ToString("hh\\:mm");

                myeventlist.Add(myevent);

                //myevent.description = gastro.Detail["de"].BaseText;
                //myevent.email = gastro.ContactInfos["de"].Email;
                //myevent.image = gastro.ImageGallery.FirstOrDefault().ImageUrl + "&W=150&H=150";
                //myevent.name = gastro.Detail["de"].Title;
                //myevent.telephone = gastro.ContactInfos["de"].Phonenumber;
                //myevent.url = gastro.ContactInfos["de"].Url;

                //addressLD myaddress = new addressLD();
                //myaddress.type = "http://schema.org/PostalAddress";
                //myaddress.streetAddress = gastro.ContactInfos["de"].Address;
                //myaddress.postalCode = gastro.ContactInfos["de"].ZipCode;
                //myaddress.addressLocality = gastro.ContactInfos["de"].City;
                //myaddress.addressRegion = "Südtirol";
                //myaddress.addressCountry = gastro.ContactInfos["de"].CountryName;

                //mygastro.address = myaddress;

                //geoLD mygeo = new geoLD();
                //mygeo.type = "http://schema.org/GeoCoordinates";
                //mygeo.latitude = gastro.Latitude;
                //mygeo.longitude = gastro.Longitude;

                //mygastro.geo = mygeo;

                //personLD founder = new personLD();
                //founder.type = "http://schema.org/Person";
                //founder.name = gastro.ContactInfos["de"].Givenname + " " + gastro.ContactInfos["de"].Surname;

                //mygastro.founder = founder;

                //mygastro.servesCuisine = GetCuisine(gastro.Facilities);

                //mygastro.openingHours = GetOpeningDates(gastro.OperationSchedule);
            }

            return myeventlist;
        }
    }
}
