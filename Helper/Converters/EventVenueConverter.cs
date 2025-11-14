// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3;
using DataModel;

namespace Helper.Converters
{
    #region Event V2 Converters

    public class EventFlattenedConverter
    {
        public static IEnumerable<EventConversionResult> ConvertEventListToEventFlattened<T>(
            IEnumerable<T> events,
            IEnumerable<EventTypes> eventtypes = null,
            IEnumerable<VenueType> venuetypes = null
        )
            where T : IIdentifiable
        {
            var result = new List<EventConversionResult>();

            foreach (var eventv1 in events)
            {
                if (eventv1 is EventShortLinked)
                {
                    result.Add(
                        ConvertEventShortToEventFlattened(
                            eventv1 as EventShortLinked,
                            eventtypes,
                            venuetypes
                        )
                    );
                }
                if (eventv1 is EventLinked)
                {
                    result.Add(
                        ConvertEventToEventFlattened(eventv1 as EventLinked, eventtypes, venuetypes)
                    );
                }
            }

            return result;
        }

        //Convert Event to EventV2
        private static EventConversionResult ConvertEventToEventFlattened(
            EventLinked eventv1,
            IEnumerable<EventTypes> eventtypes = null,
            IEnumerable<VenueType> venuetypes = null
        )
        {
            List<EventFlattened> eventv2list = new List<EventFlattened>();
            List<VenueFlattened> venuev2list = new List<VenueFlattened>();

            int eventcounter = 0;

            foreach (var eventdate in eventv1.EventDate)
            {
                //Try to map all to EventsV2
                EventFlattened eventv2 = new EventFlattened();

                eventv2.PublishedOn = eventv1.PublishedOn;
                eventv2.Id = eventv1.Id + "_" + eventcounter.ToString();

                eventv2.Id = eventv2.Id.ToUpper();

                eventv2.EventGroupId = eventv1.Id;

                eventv2.ImageGallery = eventv1.ImageGallery;
                eventv2.Shortname = eventv1.Shortname;
                eventv2.FirstImport = eventv1.FirstImport;
                eventv2.LastChange = eventv1.LastChange;
                eventv2.Active = eventv1.Active;
                eventv2.Mapping = eventv1.Mapping;
                eventv2.HasLanguage = eventv1.HasLanguage;
                eventv2.LicenseInfo = eventv1.LicenseInfo;
                eventv2.Source = eventv1.Source;

                eventv2.Detail = eventv1.Detail;
                //eventv2.GpsInfo = eventv1.GpsInfo; add to venue
                eventv2.ContactInfos = eventv1.ContactInfos;

                //Where can we store the Organizerinfo???
                //eventv2.Organizer = eventv1.OrganizerInfos;

                if (eventv2.Source == "lts")
                {
                    eventv2.Mapping.TryAddOrUpdate(
                        "lts",
                        new Dictionary<string, string>()
                        {
                            { "rid", eventv1.Id },
                            { "classificationrid", eventv1.ClassificationRID },
                        }
                    );
                }

                //Topics to Tags
                eventv2.Tags = new List<Tags>();
                eventv2.TagIds = new List<string>();

                if (eventv1.TopicRIDs != null)
                {
                    foreach (var tag in eventv1.TopicRIDs)
                    {
                        //Load the Tag
                        var eventtype = eventtypes.Where(x => x.Id == tag).FirstOrDefault();

                        if (eventtype != null)
                        {
                            //Caution using tag names from lts, they have "/" inside
                            var eventtypeid = eventtype.TypeDesc["en"].ToLower().Replace("/", "-");

                            eventv2.Tags.Add(
                                new Tags() { Id = eventtypeid, Source = eventv1.Source.ToLower() }
                            );
                            eventv2.TagIds.Add(eventtypeid);
                        }
                    }
                }

                //Creating Venue
                VenueFlattened venue = new VenueFlattened();

                string venuename = eventv1
                    .EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary()
                    .Location;

                if (String.IsNullOrEmpty(venuename))
                    venuename = eventv1
                        .EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary()
                        .Mplace;

                //Try to create an Id with this 3 fields
                venue.Id = Regex.Replace(
                    eventv1.ContactInfos.GetEnglishOrFirstKeyFromDictionary().CompanyName,
                    "[^0-9a-zA-Z]+",
                    ""
                ); //What should we use as Id?

                if (String.IsNullOrEmpty(venue.Id))
                    venue.Id = Regex.Replace(
                        eventv1.EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary().Location,
                        "[^0-9a-zA-Z]+",
                        ""
                    ); //What should we use as Id?
                if (String.IsNullOrEmpty(venue.Id))
                    venue.Id = Regex.Replace(
                        eventv1.EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary().Mplace,
                        "[^0-9a-zA-Z]+",
                        ""
                    ); //What should we use as Id?

                venue.Id = venue.Id.ToUpper();

                venue.Shortname = venuename;
                venue.GpsInfo = eventv1.GpsInfo;
                venue.LocationInfo = eventv1.LocationInfo;
                venue.ContactInfos = eventv1.ContactInfos;
                venue.Source = eventv1.Source;

                venue.Detail = new Dictionary<string, Detail>();

                foreach (var lang in eventv1.HasLanguage)
                {
                    Detail venuedetail = new Detail();
                    venuedetail.Language = lang;

                    string venuetitle = eventv1.EventAdditionalInfos[lang].Location;

                    if (String.IsNullOrEmpty(venuename))
                        venuetitle = eventv1.EventAdditionalInfos[lang].Mplace;

                    venuedetail.Title = venuetitle;
                    venuedetail.BaseText = eventv1.EventAdditionalInfos[lang].Reg;

                    venue.Detail[lang] = venuedetail;
                }

                if (venuev2list.Where(x => x.Id == venue.Id).Count() == 0)
                    venuev2list.Add(venue);

                eventv2.Begin = eventdate.From.Date + eventdate.Begin.Value;
                eventv2.End = eventdate.To.Date + eventdate.End.Value;
                eventv2.BeginUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                    eventv2.Begin
                );
                eventv2.EndUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(
                    eventv2.End
                );

                //Add From, To and Begin End
                eventv2.VenueId = venue.Id;

                eventv2list.Add(eventv2);

                eventcounter++;
            }

            return new EventConversionResult(eventv2list, venuev2list);
        }

        //Convert EventShort to EventV2
        private static EventConversionResult ConvertEventShortToEventFlattened(
            EventShortLinked eventv1,
            IEnumerable<EventTypes> eventtypes = null,
            IEnumerable<VenueType> venuetypes = null
        )
        {
            List<EventFlattened> eventv2list = new List<EventFlattened>();
            List<VenueFlattened> venuev2list = new List<VenueFlattened>();

            int eventcounter = 0;

            foreach (var room in eventv1.RoomBooked)
            {
                //Try to map all to EventsV2
                EventFlattened eventv2 = new EventFlattened();

                eventv2.PublishedOn = eventv1.PublishedOn;
                eventv2.Id = eventv1.Id + "_" + eventcounter;
                eventv2.Id = eventv2.Id.ToUpper();

                eventv2.EventGroupId = eventv1.Id;

                eventv2.ImageGallery = eventv1.ImageGallery;
                eventv2.Shortname = eventv1.Shortname;
                eventv2.FirstImport = eventv1.FirstImport;
                eventv2.LastChange = eventv1.LastChange;
                eventv2.Active = eventv1.Active.Value;
                eventv2.Mapping = eventv1.Mapping;
                eventv2.HasLanguage = eventv1.HasLanguage;
                //eventv2.GpsInfo = eventv1.GpsInfo; //todo add to Venue
                eventv2.LicenseInfo = eventv1.LicenseInfo;
                eventv2.Source = eventv1.Source;

                if (eventv2.Mapping == null)
                    eventv2.Mapping.Add(
                        "ebms",
                        new Dictionary<string, string>() { { "id", eventv1.EventId.ToString() } }
                    );

                //Putting all info into Detail
                eventv2.Detail = new Dictionary<string, Detail>();
                foreach (var lang in eventv2.HasLanguage)
                {
                    Detail detail = new Detail()
                    {
                        Title = eventv1.EventTitle[lang],
                        Language = lang,
                        BaseText =
                            eventv1.EventText != null && eventv1.EventText.ContainsKey(lang)
                                ? eventv1.EventText[lang]
                                : "",
                    };

                    eventv2.Detail.Add(lang, detail);
                }

                //Adding CustomTagging, TechnologyFields to Tags
                eventv2.Tags = new List<Tags>();

                if (eventv1.TechnologyFields != null)
                {
                    foreach (var tag in eventv1.TechnologyFields)
                    {
                        eventv2.Tags.Add(
                            new Tags()
                            {
                                Id = tag,
                                Source = "noi",
                                Type = "TechnologyFields",
                            }
                        );
                    }
                }
                if (eventv1.CustomTagging != null)
                {
                    foreach (var tag in eventv1.CustomTagging)
                    {
                        eventv2.Tags.Add(
                            new Tags()
                            {
                                Id = tag,
                                Source = "noi",
                                Type = "CustomTagging",
                            }
                        );
                    }
                }

                //Adding EventDocument, Documents as DocumentDetailed
                eventv2.Documents = new Dictionary<string, List<DocumentDetailed>>();
                foreach (var documentkvp in eventv1.Documents)
                {
                    List<DocumentDetailed> documents = new List<DocumentDetailed>();
                    foreach (var doc in documentkvp.Value)
                    {
                        documents.Add(
                            new DocumentDetailed()
                            {
                                DocumentName = doc.DocumentName,
                                DocumentURL = doc.DocumentURL,
                                Language = doc.Language,
                            }
                        );
                    }

                    eventv2.Documents.Add(documentkvp.Key, documents);
                }

                //WebAddress adding to contactinfo
                eventv2.ContactInfos = new Dictionary<string, ContactInfos>();
                foreach (var lang in eventv1.HasLanguage)
                {
                    ContactInfos contactinfo = new ContactInfos();
                    contactinfo.Url = eventv1.WebAddress;
                    contactinfo.Language = lang;

                    contactinfo.Email = eventv1.ContactEmail;
                    contactinfo.City = eventv1.ContactCity;
                    contactinfo.Address = eventv1.ContactAddressLine1;
                    contactinfo.Phonenumber = eventv1.ContactPhone;
                    contactinfo.CountryName = eventv1.ContactCountry;
                    contactinfo.Surname = eventv1.ContactLastName;
                    contactinfo.Givenname = eventv1.ContactFirstName;
                    contactinfo.Faxnumber = eventv1.ContactFax;
                    contactinfo.ZipCode = eventv1.ContactPostalCode;
                    contactinfo.Tax = eventv1.ContactCode;

                    eventv2.ContactInfos.Add(lang, contactinfo);
                }

                //Where to store the OrganizerInfo??
                //eventv2.Organizer = new Dictionary<string, ContactInfos>();
                //foreach (var lang in eventv1.HasLanguage)
                //{
                //    ContactInfos contactinfo = new ContactInfos();
                //    contactinfo.Url = eventv1.WebAddress;
                //    contactinfo.Language = lang;

                //    contactinfo.CompanyName = eventv1.CompanyName;
                //    contactinfo.Email = eventv1.CompanyMail;
                //    contactinfo.City = eventv1.CompanyCity;
                //    contactinfo.Address = eventv1.CompanyAddressLine1;
                //    contactinfo.Phonenumber = eventv1.CompanyPhone;
                //    contactinfo.CountryName = eventv1.CompanyCountry;
                //    contactinfo.Faxnumber = eventv1.CompanyFax;
                //    contactinfo.ZipCode = eventv1.CompanyPostalCode;
                //    contactinfo.Url = eventv1.CompanyUrl;
                //    contactinfo.Tax = eventv1.CompanyId;

                //    eventv2.Organizer.Add(lang, contactinfo);
                //}

                //Adding EventLocation, AnchorVenue, AnchorVenueRoomMapping, AnchorVenueShort, EndDate, StartDate, StartDateUTC, EndDateUTC, RoomBooked

                eventv2.Begin = room.StartDate;
                eventv2.BeginUTC = room.StartDateUTC;
                eventv2.End = room.EndDate;
                eventv2.EndUTC = room.EndDateUTC;

                //TODO SUBTITLE
                //eventinfo.Detail = new Dictionary<string, Detail>()
                //{
                //    { "en", new Detail(){ Title = room.Subtitle } }
                //};

                //Space, SpaceDesc, SpaceType, Comment, SpaceAbbrev, SpaceDescRoomMapping

                //Create Venue
                VenueFlattened venue = new VenueFlattened();
                venue.Id = "eventeuracnoi_" + room.Space.ToLower() + "_" + room.SpaceType;
                venue.Id = venue.Id.ToUpper();

                venue.Shortname = room.SpaceAbbrev;
                //venue.LocationInfo = Todo create locationinfo
                venue.GpsInfo = eventv1.GpsInfo;
                venue.Source = eventv1.Source;

                venue.Detail = new Dictionary<string, Detail>()
                {
                    {
                        "en",
                        new Detail() { Title = room.SpaceDesc }
                    },
                };

                eventv2.VenueId = venue.Id;

                if (!venuev2list.Select(x => x.Id).ToList().Contains(venue.Id))
                    venuev2list.Add(venue);

                //Video
                if (!String.IsNullOrEmpty(eventv1.VideoUrl))
                {
                    eventv2.VideoItems = new Dictionary<string, ICollection<VideoItems>>()
                    {
                        {
                            "en",
                            new List<VideoItems>() { new VideoItems() { Url = eventv1.VideoUrl } }
                        },
                    };
                }

                //ExternalOrganizer, SoldOut, TypicalAgeRange
                EventEuracNoiInfo additionalinfo = new EventEuracNoiInfo();
                additionalinfo.ExternalOrganizer = eventv1.ExternalOrganizer;
                additionalinfo.SoldOut = eventv1.SoldOut;
                additionalinfo.TypicalAgeRange = eventv1.TypicalAgeRange;
                additionalinfo.EventLocation = eventv1.EventLocation;

                eventv2.AdditionalProperties = new Dictionary<string, dynamic>()
                {
                    { "additionalinfo", additionalinfo },
                };

                eventv2list.Add(eventv2);

                eventcounter++;
            }

            return new EventConversionResult(eventv2list, venuev2list);
        }

        //Convert EventV2 to Event, pass all the Venues, the Root Event should be the first in List
        private static EventLinked ConvertEventFlattenedToEvent(
            EventFlattened eventv2,
            List<VenueFlattened> venuev2list,
            List<EventTypes> eventtypes
        )
        {
            EventLinked eventv1 = new EventLinked();

            eventv1.Id = eventv2.Id;
            eventv1.ImageGallery = eventv2.ImageGallery;
            eventv1.Shortname = eventv2.Shortname;
            eventv1.FirstImport = eventv2.FirstImport;
            eventv1.LastChange = eventv2.LastChange;
            eventv1.Active = eventv2.Active;
            eventv1.Mapping = eventv2.Mapping;
            eventv1.HasLanguage = eventv2.HasLanguage;
            eventv1.LicenseInfo = eventv2.LicenseInfo;
            eventv1.Source = eventv2.Source;

            eventv1.Detail = eventv2.Detail;
            eventv1.ContactInfos = eventv2.ContactInfos;

            eventv1.PublishedOn = eventv2.PublishedOn;

            eventv1.DateBegin = eventv2.Begin;
            eventv1.DateEnd = eventv2.End;

            eventv1.Mapping = eventv2.Mapping;

            //GpsInfo from venue
            //Gpstype
            //Altitude
            //AltitudeUnitofMeasure
            //Latitude
            //Longitude
            //"OrgRID": "TreviLab",
            //OdhTags
            //SmgTags
            //Districts
            //EventDate
            //GpsPoints
            //ODHActive
            //SmgActive
            //Shortname
            //TopicRiuds
            //DistrictId
            //DistrictIds
            //EventPrice
            //EventPrices
            //DistanceInfo
            //EventBenefit
            //EventBooking
            //EventCrossSelling
            //EventDescAdditional
            //EventAdditionalInfos
            //EventOperationScheduleOverview
            //LocationInfo
            //EventDatesEnd
            //EventDatesBegin
            //EventDateCounter
            //ClassificationRID
            //NextBeginDate
            //EventPublisher
            //OrganizerInfos

            eventv1.TopicRIDs = eventv2.TagIds;

            List<TopicLinked> topiclinkedlist = new List<TopicLinked>();
            foreach (var topicrid in eventv2.TagIds)
            {
                var topic = eventtypes.Where(x => x.Id == topicrid).FirstOrDefault();
                if (topic != null)
                    topiclinkedlist.Add(
                        new TopicLinked() { TopicRID = topicrid, TopicInfo = topic.TypeDesc["de"] }
                    );
            }
            eventv1.Topics = topiclinkedlist.ToList();

            eventv1.Tags = eventv2.Tags;
            eventv1.TagIds = eventv2.TagIds;

            //if (eventv1.TopicRIDs != null)
            //{
            //    foreach (var tag in eventv1.TopicRIDs)
            //    {
            //        //Load the Tag
            //        var eventtype = eventtypes.Where(x => x.Id == tag).FirstOrDefault();

            //        if (eventtype != null)
            //        {
            //            //Caution using tag names from lts, they have "/" inside
            //            var eventtypeid = eventtype.TypeDesc["en"].ToLower().Replace("/", "-");

            //            eventv2.Tags.Add(new Tags() { Id = eventtypeid, Source = eventv1.Source.ToLower() });
            //            eventv2.TagIds.Add(eventtypeid);
            //        }
            //    }
            //}

            ////Creating Venue
            //VenueV2 venue = new VenueV2();

            //string venuename = eventv1.EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary().Location;

            //if (String.IsNullOrEmpty(venuename))
            //    venuename = eventv1.EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary().Mplace;

            ////Try to create an Id with this 3 fields
            //venue.Id = Regex.Replace(eventv1.ContactInfos.GetEnglishOrFirstKeyFromDictionary().CompanyName, "[^0-9a-zA-Z]+", ""); //What should we use as Id?

            //if (String.IsNullOrEmpty(venue.Id))
            //    venue.Id = Regex.Replace(eventv1.EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary().Location, "[^0-9a-zA-Z]+", ""); //What should we use as Id?
            //if (String.IsNullOrEmpty(venue.Id))
            //    venue.Id = Regex.Replace(eventv1.EventAdditionalInfos.GetEnglishOrFirstKeyFromDictionary().Mplace, "[^0-9a-zA-Z]+", ""); //What should we use as Id?


            //venue.Id = venue.Id.ToUpper();

            //venue.Shortname = venuename;
            //venue.GpsInfo = eventv1.GpsInfo;
            //venue.LocationInfo = eventv1.LocationInfo;
            //venue.ContactInfos = eventv1.ContactInfos;
            //venue.Source = eventv1.Source;


            //venue.Detail = new Dictionary<string, Detail>();

            //foreach (var lang in eventv1.HasLanguage)
            //{
            //    Detail venuedetail = new Detail();
            //    venuedetail.Language = lang;

            //    string venuetitle = eventv1.EventAdditionalInfos[lang].Location;

            //    if (String.IsNullOrEmpty(venuename))
            //        venuetitle = eventv1.EventAdditionalInfos[lang].Mplace;

            //    venuedetail.Title = venuetitle;
            //    venuedetail.BaseText = eventv1.EventAdditionalInfos[lang].Reg;

            //    venue.Detail[lang] = venuedetail;
            //}

            //if (venuev2list.Where(x => x.Id == venue.Id).Count() == 0)
            //    venuev2list.Add(venue);

            //eventv2.Begin = eventdate.From.Date + eventdate.Begin.Value;
            //eventv2.End = eventdate.To.Date + eventdate.End.Value;
            //eventv2.BeginUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(eventv2.Begin);
            //eventv2.EndUTC = Helper.DateTimeHelper.DateTimeToUnixTimestampMilliseconds(eventv2.End);





            return eventv1;
        }

        public static TagLinked ConvertEventTopicToTag(EventTypes eventType)
        {
            TagLinked tag = new TagLinked();
            tag.Id = eventType.TypeDesc["en"].ToLower().Replace("/", "-");
            tag.FirstImport = DateTime.Now;
            tag.LastChange = DateTime.Now;
            tag.MainEntity = "event";
            tag.ValidForEntity = new List<string>() { "event" };
            tag.TagName = eventType.TypeDesc;
            tag.Source = "lts";
            tag.DisplayAsCategory = true;
            tag.LicenseInfo = null;
            tag.LTSTaggingInfo = new LTSTaggingInfo() { LTSRID = eventType.Id, ParentLTSRID = "" };
            Dictionary<string, string> mapping = new Dictionary<string, string>()
            {
                { "rid", eventType.Id },
            };
            tag.Mapping = new Dictionary<string, IDictionary<string, string>>()
            {
                { "lts", mapping },
            };

            return tag;
        }

        public static TagLinked ConvertEventShortTopicToTag(SmgPoiTypes eventType)
        {
            TagLinked tag = new TagLinked();
            tag.Id = eventType.Id.ToLower().Replace("/", "-");
            tag.FirstImport = DateTime.Now;
            tag.LastChange = DateTime.Now;
            tag.MainEntity = "event";
            tag.ValidForEntity = new List<string>() { "event" };
            tag.TagName = eventType.TypeDesc;
            tag.Source = "noi";
            tag.DisplayAsCategory = true;
            tag.LicenseInfo = null;

            Dictionary<string, string> mapping = new Dictionary<string, string>()
            {
                { "type", eventType.Type },
            };
            tag.Mapping = new Dictionary<string, IDictionary<string, string>>()
            {
                { "odh", mapping },
            };

            return tag;
        }
    }

    #endregion

    #region VenueV2 Converter

    public class VenueFlattenedConverter
    {
        public static IEnumerable<VenueFlattened> ConvertVenueListToVenueFlattened(
            IEnumerable<VenueLinked> venues,
            IEnumerable<DDVenueCodes> venuecodes
        )
        {
            var venuestoreturn = new List<VenueFlattened>();

            foreach (var venue in venues)
            {
                var result = ConvertVenueLinkedToVenueFlattened(venue, venuecodes);
                venuestoreturn.AddRange(result);
            }

            return venuestoreturn;
        }

        private static IEnumerable<VenueFlattened> ConvertVenueLinkedToVenueFlattened(
            VenueLinked venuev1,
            IEnumerable<DDVenueCodes> venuecodes
        )
        {
            List<VenueFlattened> venues = new List<VenueFlattened>();

            //Root Venue
            VenueFlattened venuev2 = new VenueFlattened();

            venuev2.Id = venuev1.Id;
            venuev2.HasLanguage = venuev1.HasLanguage;
            venuev2.Active = venuev1.Active;
            venuev2._Meta = venuev1._Meta;
            venuev2.LastChange = venuev1.LastChange;
            venuev2.FirstImport = venuev1.FirstImport;
            venuev2.LicenseInfo = venuev1.LicenseInfo;
            venuev2.Shortname = venuev1.Shortname;
            venuev2.PublishedOn = venuev1.PublishedOn;
            venuev2.Source = venuev1.Source;

            venuev2.IsRoot = true;
            venuev2.Detail = venuev1.Detail;
            venuev2.ImageGallery = venuev1.ImageGallery;
            venuev2.ContactInfos = venuev1.ContactInfos;
            venuev2.GpsInfo = venuev1.GpsInfo;

            venuev2.VenueInfo = new VenueInfo();
            venuev2.VenueInfo.Indoor = null;
            venuev2.VenueInfo.SquareMeters = null;
            venuev2.VenueInfo.Beds = venuev1.Beds;
            venuev2.VenueInfo.Rooms = venuev1.RoomCount;

            venuev2.Tags = ConvertVenueFeatureToTag(venuev1.VenueCategory, venuecodes).ToList();

            venuev2.OperationSchedule = venuev1.OperationSchedule;
            venuev2.LocationInfo = venuev1.LocationInfo;

            //Add mapping
            venuev2.Mapping.Add("lts", new Dictionary<string, string>() { { "rid", venuev2.Id } });

            venuev2.RelatedContent = new List<RelatedContent>();

            //Subvenue
            foreach (var subvenuev1 in venuev1.RoomDetails)
            {
                VenueFlattened subvenuev2 = new VenueFlattened();
                subvenuev2.Id = subvenuev1.Id;
                //Infos from root
                subvenuev2.HasLanguage = venuev1.HasLanguage;
                subvenuev2.Active = venuev1.Active;
                subvenuev2._Meta = venuev1._Meta;
                subvenuev2.LastChange = venuev1.LastChange;
                subvenuev2.FirstImport = venuev1.FirstImport;
                subvenuev2.LicenseInfo = venuev1.LicenseInfo;
                subvenuev2.PublishedOn = venuev1.PublishedOn;
                subvenuev2.Source = venuev1.Source;
                subvenuev2.GpsInfo = venuev1.GpsInfo;
                subvenuev2.LocationInfo = venuev1.LocationInfo;

                subvenuev2.IsRoot = false;
                subvenuev2.Detail = subvenuev1.Detail;
                subvenuev2.ImageGallery = subvenuev1.ImageGallery;
                subvenuev2.Shortname = subvenuev1.Shortname;
                subvenuev2.VenueInfo = new VenueInfo();
                subvenuev2.VenueInfo.Indoor = subvenuev1.Indoor;
                subvenuev2.VenueInfo.SquareMeters = subvenuev1.SquareMeters;
                subvenuev2.VenueInfo.Beds = null;
                subvenuev2.VenueInfo.Rooms = null;

                subvenuev2.Tags = ConvertVenueFeatureToTag(subvenuev1.VenueFeatures, venuecodes)
                    .ToList();
                subvenuev2.Capacity = ConvertVenueSetupToTag(subvenuev1.VenueSetup).ToList();

                //Add relation
                subvenuev2.RelatedContent = new List<RelatedContent>();
                subvenuev2.RelatedContent.Add(
                    new RelatedContent() { Id = venuev2.Id, Type = "venue" }
                );

                //Add mapping
                subvenuev2.Mapping.Add(
                    "lts",
                    new Dictionary<string, string>() { { "rid", subvenuev2.Id } }
                );
                subvenuev2.Mapping.Add(
                    "noi",
                    new Dictionary<string, string>() { { "parent_Id", venuev2.Id } }
                );

                venues.Add(subvenuev2);

                venuev2.RelatedContent.Add(
                    new RelatedContent() { Id = subvenuev2.Id, Type = "venue" }
                );
            }

            venues.Add(venuev2);

            return venues;
        }

        private static IEnumerable<VenueSetupV2> ConvertVenueSetupToTag(
            IEnumerable<VenueSetup> venuesetups
        )
        {
            List<VenueSetupV2> tagstoreturn = new List<VenueSetupV2>();
            foreach (var venuesetup in venuesetups)
            {
                VenueSetupV2 tagv2 = new VenueSetupV2();

                tagv2.Capacity = venuesetup.Capacity;
                tagv2.TagId = venuesetup.VenueCode.Replace("lts/", "").Replace("/", "-");
                tagv2.Tag = new Tags()
                {
                    Source = "lts",
                    Id = venuesetup.VenueCode,
                    Type = "seatType",
                };

                tagstoreturn.Add(tagv2);
            }

            return tagstoreturn;
        }

        private static IEnumerable<Tags> ConvertVenueFeatureToTag(
            IEnumerable<VenueType> venuetypes,
            IEnumerable<DDVenueCodes> venuecodes
        )
        {
            List<Tags> tagstoreturn = new List<Tags>();
            foreach (var venuetype in venuetypes)
            {
                Tags tagv2 = new Tags();

                tagv2.Id = venuetype.VenueCode.Replace("lts/", "").Replace("/", "-");
                if (venuetype.VenueCode == "lts/faci_" || venuetype.VenueCode == "lts/type_")
                {
                    tagv2.Id =
                        venuetype.VenueCode.Replace("lts/", "").Replace("/", "-")
                        + venuecodes
                            .Where(x => x.Id == venuetype.Id)
                            .FirstOrDefault()
                            .Name["en"]
                            .Replace(" ", "_")
                            .ToLower();
                }

                tagv2.Source = "lts";
                if (venuetype.VenueCode.StartsWith("lts/type_"))
                    tagv2.Type = "category";
                if (venuetype.VenueCode.StartsWith("lts/faci"))
                    tagv2.Type = "features";

                tagstoreturn.Add(tagv2);
            }

            return tagstoreturn;
        }

        public static TagLinked ConvertVenueTagToTag(DDVenueCodes venueType)
        {
            TagLinked tag = new TagLinked();
            tag.Id = venueType.Code.ToLower().Replace("lts/", "").Replace("/", "-");
            tag.FirstImport = DateTime.Now;
            tag.LastChange = DateTime.Now;
            tag.MainEntity = "venue";
            tag.ValidForEntity = new List<string>() { "venue" };
            tag.TagName = venueType.TypeDesc;
            tag.Source = "lts";
            tag.DisplayAsCategory = true;
            tag.LicenseInfo = null;

            tag.LTSTaggingInfo = new LTSTaggingInfo() { LTSRID = venueType.Id, ParentLTSRID = "" };
            Dictionary<string, string> mapping = new Dictionary<string, string>()
            {
                { "rid", venueType.Id },
                { "type", venueType.Type },
            };
            tag.Mapping = new Dictionary<string, IDictionary<string, string>>()
            {
                { "lts", mapping },
            };

            return tag;
        }
    }

    #endregion

    #region Event Conversion Result Class

    public class EventConversionResult
    {
        public EventConversionResult() { }

        public EventConversionResult(IEnumerable<EventFlattened> events, IEnumerable<VenueFlattened> venues)
        {
            this.Events = events;
            this.Venues = venues;
        }

        public IEnumerable<EventFlattened> Events { get; set; }
        public IEnumerable<VenueFlattened> Venues { get; set; }
    }

    #endregion

    public static class DictionaryExtensionsTemp
    {
        //TODO Migrate
        public static T GetEnglishOrFirstKeyFromDictionary<T>(this IDictionary<string, T> dict)
        {
            if (dict.ContainsKey("en"))
                return dict["en"];
            else
                return dict.FirstOrDefault().Value;
        }
    }
}
