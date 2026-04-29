// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Converters;
using Helper.Generic;
using Helper.JsonHelpers;
using Helper.Tagging;
using LTSAPI;
using LTSAPI.Parser;
using Microsoft.FSharp.Control;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Newtonsoft.Json;
using OdhNotifier;
using SqlKata;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace OdhApiImporter.Helpers
{
    /// <summary>
    /// This class is used for different update operations on the data
    /// </summary>
    public class CustomDataOperation
    {
        private readonly QueryFactory QueryFactory;
        private readonly ISettings settings;

        public CustomDataOperation(ISettings settings, QueryFactory queryfactory)
        {
            this.QueryFactory = queryfactory;
            this.settings = settings;
        }

        #region MetaData

        public async Task<int> UpdateMetaDataApiRecordCount()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("metadata");

            var data = await query.GetObjectListAsync<TourismMetaData>();
            int i = 0;

            foreach (var metadata in data)
            {
                if (!String.IsNullOrEmpty(metadata.OdhType))
                {
                    metadata.RecordCount = await MetaDataApiRecordCount.GetRecordCountfromDB(
                        metadata.ApiFilter,
                        metadata.OdhType,
                        QueryFactory
                    );

                    //Save tp DB
                    var queryresult = await QueryFactory
                        .Query("metadata")
                        .Where("id", metadata.Id)
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = metadata.Id?.ToLower() ?? "",
                                data = new JsonRaw(metadata),
                            }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> ResaveMetaData(string host, bool correcturls)
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("metadata");

            var data = await query.GetObjectListAsync<TourismMetaData>();
            int i = 0;

            foreach (var metadata in data)
            {
                //fix swaggerurl mess
                //var swaggerurl = "swagger/index.html#" + metadata.SwaggerUrl.Split("#").LastOrDefault();

                //metadata.SwaggerUrl = swaggerurl;

                //modify domain

                //if (correcturls && !host.StartsWith("importer.tourism") && metadata.BaseUrl.StartsWith("https://api.tourism.testingmachine.eu"))
                //{
                //    metadata.BaseUrl = "https://tourism.api.opendatahub.com";
                //    if(!String.IsNullOrEmpty(metadata.SwaggerUrl))
                //        metadata.SwaggerUrl = metadata.SwaggerUrl.Replace("https://api.tourism.testingmachine.eu", "https://tourism.api.opendatahub.com");
                //}

                //if (correcturls && !host.StartsWith("importer.tourism") && metadata.ImageGallery != null && metadata.ImageGallery.Count() > 0)
                //{
                //    foreach (var image in metadata.ImageGallery)
                //    {
                //        if (image.ImageUrl.StartsWith("https://images.tourism.testingmachine.eu"))
                //        {
                //            image.ImageUrl = image.ImageUrl.Replace("https://images.tourism.testingmachine.eu", "https://images.opendatahub.com");
                //        }
                //    }
                //}


                //metadata.Type = metadata.OdhType;
                //metadata.LicenseInfo = new LicenseInfo() { Author = "https://noi.bz.it", ClosedData = false, License = "CC0", LicenseHolder = "https://noi.bz.it" };

                //Adding ApiType
                if (metadata.ApiUrl.Contains("tourism"))
                    metadata.ApiType = "content";
                else if (metadata.ApiUrl.Contains("mobility"))
                    metadata.ApiType = "timeseries";

                //Save tp DB
                var queryresult = await QueryFactory
                    .Query("metadata")
                    .Where("id", metadata.Id)
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = metadata.Id?.ToLower() ?? "",
                            data = new JsonRaw(metadata),
                        }
                    );

                i++;
            }

            return i;
        }

        #endregion

        #region Generic

        public async Task<int> ResaveSourcesOnType<T>(
            string odhtype,
            string sourcetofilter,
            string sourcetochange
        )
            where T : notnull
        {
            string table = ODHTypeHelper.TranslateTypeString2Table(odhtype);
            var mytype = ODHTypeHelper.TranslateTypeString2Type(odhtype);

            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From(table)
                .When(sourcetofilter != "null", x => x.WhereJsonb("Source", sourcetofilter))
                .When(sourcetofilter == "null", x => x.WhereRaw("data->>'Source' is null"));

            var data = await query.GetObjectListAsync<T>();

            int i = 0;

            foreach (var tag in data)
            {
                if (tag is IIdentifiable)
                {
                    if (tag is ISource)
                        ((ISource)tag).Source = sourcetochange;

                    //Save to DB
                    var queryresult = await QueryFactory
                        .Query(table)
                        .Where("id", ((IIdentifiable)tag).Id)
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = ((IIdentifiable)tag).Id ?? "",
                                data = new JsonRaw(tag),
                            }
                        );

                    i = i + queryresult;
                }
            }

            return i;
        }

        #endregion

        #region Articles

        public async Task<int> NewsFeedUpdate()
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("articles")
                .WhereRaw("gen_articletype @> ARRAY['newsfeednoi']");

            var articles = await query.GetObjectListAsync<ArticlesLinked>();
            int i = 0;

            foreach (var article in articles)
            {
                //if (article.Active == null)
                //{
                //    article.Active = false;

                //    //Save tp DB
                //    var queryresult = await QueryFactory.Query("articles").Where("id", article.Id)
                //         .UpdateAsync(new JsonBData() { id = article.Id, data = new JsonRaw(article) });

                //    i++;
                //}
            }

            return i;
        }

        public async Task<int> FillDBWithDummyNews()
        {
            int crudcount = 0;

            for (int i = 1; i <= 120; i++)
            {
                ArticlesLinked myarticle = new ArticlesLinked();
                myarticle.Id = Guid.NewGuid().ToString().ToUpper();
                myarticle.Type = "newsfeednoi";
                myarticle.Active = true;
                myarticle.Detail.TryAddOrUpdate(
                    "de",
                    new Detail()
                    {
                        Title = "TesttitleDE" + i,
                        BaseText = "testtextDE " + i,
                        Language = "de",
                        AdditionalText = "additionaltextde" + i,
                    }
                );
                myarticle.Detail.TryAddOrUpdate(
                    "it",
                    new Detail()
                    {
                        Title = "TesttitleIT" + i,
                        BaseText = "testtextIT " + i,
                        Language = "it",
                        AdditionalText = "additionaltextit" + i,
                    }
                );
                myarticle.Detail.TryAddOrUpdate(
                    "en",
                    new Detail()
                    {
                        Title = "TesttitleEN" + i,
                        BaseText = "testtextEN " + i,
                        Language = "en",
                        AdditionalText = "additionaltexten" + i,
                    }
                );

                myarticle.HasLanguage = new List<string>() { "de", "it", "en" };

                myarticle.LicenseInfo = new LicenseInfo()
                {
                    Author = "",
                    License = "CC0",
                    ClosedData = false,
                    LicenseHolder = "https://noi.bz.it",
                };

                myarticle.ContactInfos.TryAddOrUpdate(
                    "de",
                    new ContactInfos()
                    {
                        Email = "community@noi.bz.it",
                        LogoUrl = "https://databrowser.opendatahub.com/icons/NOI.png",
                        Language = "de",
                        CompanyName = "NOI Techpark",
                    }
                );
                myarticle.ContactInfos.TryAddOrUpdate(
                    "it",
                    new ContactInfos()
                    {
                        Email = "community@noi.bz.it",
                        LogoUrl = "https://databrowser.opendatahub.com/icons/NOI.png",
                        Language = "it",
                        CompanyName = "NOI Techpark",
                    }
                );
                myarticle.ContactInfos.TryAddOrUpdate(
                    "en",
                    new ContactInfos()
                    {
                        Email = "community@noi.bz.it",
                        LogoUrl = "https://databrowser.opendatahub.com/icons/NOI.png",
                        Language = "en",
                        CompanyName = "NOI Techpark",
                    }
                );

                myarticle.ArticleDate = DateTime.Now.Date.AddDays(i);

                if (i % 5 == 0)
                {
                    myarticle.ArticleDateTo = DateTime.Now.Date.AddMonths(i);
                }
                else
                    myarticle.ArticleDateTo = DateTime.MaxValue;

                myarticle.SmgActive = true;
                myarticle.Source = "noi";

                if (i % 3 == 0)
                {
                    myarticle.SmgTags = new List<string>() { "important" };
                }

                var pgcrudresult = await QueryFactory.UpsertData<ArticlesLinked>(
                    myarticle,
                    new DataInfo("articles", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("article.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                if (pgcrudresult.created != null)
                    crudcount = crudcount + pgcrudresult.created.Value;
            }

            return crudcount;
        }

        #endregion

        #region Weather

        public async Task<int> UpdateAllWeatherHistoryWithMetainfo()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("weatherdatahistory");

            var data = await query.GetObjectListAsync<WeatherHistoryLinked>();
            int i = 0;

            foreach (var weatherhistory in data)
            {
                //Setting ID
                if (weatherhistory.Id == null)
                    weatherhistory.Id = weatherhistory.Weather["de"].Id.ToString();

                //Get MetaInfo
                weatherhistory._Meta = MetadataHelper.GetMetadataobject<WeatherHistoryLinked>(
                    weatherhistory
                );

                //Setting MetaInfo
                weatherhistory._Meta.Reduced = false;

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("weatherdatahistory")
                    .Where("id", weatherhistory.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = weatherhistory.Id,
                            data = new JsonRaw(weatherhistory),
                        }
                    );

                i++;
            }

            return i;
        }

        #endregion

        #region Accommodation

        public async Task<int> AccommodationRoomModify()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("accommodationrooms");

            var accorooms = await query.GetObjectListAsync<AccommodationRoomLinked>();
            int i = 0;

            foreach (var accoroom in accorooms)
            {
                if (
                    accoroom.PublishedOn != null
                    && accoroom.PublishedOn.Count == 2
                    && accoroom.PublishedOn.FirstOrDefault() == "idm-marketplace"
                )
                {
                    accoroom.PublishedOn = new List<string>()
                    {
                        "suedtirol.info",
                        "idm-marketplace",
                    };

                    //Save tp DB
                    var queryresult = await QueryFactory
                        .Query("accommodationrooms")
                        .Where("id", accoroom.Id)
                        .UpdateAsync(
                            new JsonBData() { id = accoroom.Id, data = new JsonRaw(accoroom) }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> AccommodationModify(List<string> idlist, bool trim)
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("accommodations")
                .WhereIn("id", idlist);

            var accos = await query.GetObjectListAsync<AccommodationLinked>();
            int i = 0;

            foreach (var acco in accos)
            {
                if (trim)
                {
                    acco.AccoDetail["de"].Name = acco.AccoDetail["de"].Name.Trim();
                }
                else
                {
                    acco.AccoDetail["de"].Name = acco.AccoDetail["de"].Name + " ";
                }

                //Save tp DB
                var queryresult = await QueryFactory
                    .Query("accommodations")
                    .Where("id", acco.Id)
                    .UpdateAsync(new JsonBData() { id = acco.Id, data = new JsonRaw(acco) });

                i++;
            }

            return i;
        }

        public async Task<int> AccommodationModifyToV2(List<string>? idlist)
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("accommodations")
                .When(idlist != null, x => x.WhereIn("id", idlist));

            var accos = await query.GetObjectListAsync<AccommodationLinked>();
            int i = 0;

            foreach (var acco in accos)
            {
                var accov2 = new AccommodationV2();

                accov2.AccoBookingChannel = acco.AccoBookingChannel;
                accov2.AccoCategoryId = acco.AccoCategoryId;
                accov2.AccoDetail = acco.AccoDetail;
                accov2.AccoHGVInfo = acco.AccoHGVInfo;
                accov2.AccoOverview = acco.AccoOverview;

                var accoproperties = new AccoProperties();
                accoproperties.HasApartment = acco.HasApartment;
                accoproperties.HasRoom = acco.HasRoom;
                accoproperties.IsAccommodation = acco.IsAccommodation;
                accoproperties.IsBookable = acco.IsBookable;
                accoproperties.IsCamping = acco.IsCamping;
                accoproperties.IsGastronomy = acco.IsGastronomy;
                accoproperties.TVMember = acco.TVMember;

                accov2.AccoProperties = accoproperties;
                accov2.AccoRoomInfo = acco.AccoRoomInfo;
                accov2.AccoTypeId = acco.AccoTypeId;
                accov2.Active = acco.Active;
                accov2.BadgeIds = acco.BadgeIds;
                accov2.BoardIds = acco.BoardIds;

                //Todo fill tagids and tags?
                //accov2.TagIds =

                accov2.DistanceInfo = acco.DistanceInfo;
                accov2.DistrictId = acco.DistrictId;
                accov2.Features = acco.Features;
                accov2.FirstImport = acco.FirstImport;
                //todo hide this
                accov2.GastronomyId = acco.GastronomyId;
                accov2.GpsInfo = acco.GpsInfo;
                accov2.HasLanguage = acco.HasLanguage;

                //TODO Hide this
                accov2.HgvId = acco.HgvId;
                accov2.Id = acco.Id;
                accov2.ImageGallery = acco.ImageGallery;
                accov2.IndependentData = acco.IndependentData;
                accov2.LastChange = acco.LastChange;
                accov2.LicenseInfo = acco.LicenseInfo;
                accov2.LocationInfo = acco.LocationInfo;

                //TODO Hide this
                accov2.MainLanguage = acco.MainLanguage;

                accov2.Mapping = acco.Mapping;
                accov2.MarketingGroupIds = acco.MarketingGroupIds;
                accov2.MssResponseShort = acco.MssResponseShort;

                //TODO Hide this
                accov2.PublishedOn = acco.PublishedOn;

                accov2.Representation = acco.Representation;

                Review review = new Review();
                review.ReviewId = acco.TrustYouID;
                review.Results = acco.TrustYouResults;
                review.Provider = "trustyou";
                review.Active = acco.TrustYouActive;
                review.Score = acco.TrustYouScore;
                review.StateInteger = acco.TrustYouState;

                if (accov2.Review == null)
                    accov2.Review = new Dictionary<string, DataModel.Review>();

                if (!String.IsNullOrEmpty(acco.TrustYouID))
                    accov2.Review.TryAddOrUpdate("trustyou", review);

                accov2.Shortname = acco.Shortname;
                accov2.SmgActive = acco.SmgActive;
                accov2.SmgTags = acco.SmgTags;
                accov2.Source = acco.Source;
                accov2.SpecialFeaturesIds = acco.SpecialFeaturesIds;
                accov2.ThemeIds = acco.ThemeIds;
                accov2.TourismVereinId = acco.TourismVereinId;
                accov2._Meta = acco._Meta;

                //TODO Fill
                //accov2.OperationSchedule;
                //accov2.TagIds;
                //accov2.Tags;
                //accov2.RatePlan;

                //Save tp DB
                var queryresult = await QueryFactory
                    .Query("accommodations")
                    .Where("id", accov2.Id)
                    .UpdateAsync(new JsonBData() { id = accov2.Id, data = new JsonRaw(accov2) });

                i++;
            }

            return i;
        }

        #endregion

        #region ODHActivityPoi

        //public async Task<int> UpdateAllODHActivityPoiOldTags(string source)
        //{
        //    //Load all data from PG and resave
        //    var query = QueryFactory
        //        .Query()
        //        .SelectRaw("data")
        //        .From("smgpois")
        //        .Where("gen_source", source);

        //    var data = await query.GetObjectListAsync<ODHActivityPoiOld>();
        //    int i = 0;

        //    foreach (var stapoi in data)
        //    {
        //        if (stapoi.Tags != null)
        //        {
        //            //CopyClassHelper.CopyPropertyValues
        //            var tags = stapoi.Tags;

        //            stapoi.Tags = null;

        //            var stapoiv2 = (ODHActivityPoiLinked)stapoi;

        //            stapoiv2.Tags = new List<Tags>();
        //            foreach (var tagdict in tags)
        //            {
        //                foreach (var tagvalue in tagdict.Value)
        //                {
        //                    stapoiv2.Tags.Add(tagvalue);
        //                }
        //            }

        //            //Save tp DB
        //            //TODO CHECK IF THIS WORKS
        //            var queryresult = await QueryFactory
        //                .Query("smgpois")
        //                .Where("id", stapoiv2.Id)
        //                .UpdateAsync(
        //                    new JsonBData()
        //                    {
        //                        id = stapoiv2.Id?.ToLower() ?? "",
        //                        data = new JsonRaw(stapoiv2),
        //                    }
        //                );

        //            i++;
        //        }
        //    }

        //    return i;
        //}

        public async Task<int> UpdateAllODHActivityPoiTagIds(
            string? id,
            bool? forceupdate,
            int? takefirstn
        )
        {
            //Load all data from PG and resave TODO filter only where TagIds = null
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("smgpois")
                .When(forceupdate != true, x => x.WhereRaw($"data#>>'\\{{TagIds\\}}' IS NULL"))
                .When(!String.IsNullOrEmpty(id), x => x.Where("id", id))
                .When(takefirstn != null, x => x.Take(takefirstn.Value));

            var data = await query.GetObjectListAsync<ODHActivityPoiLinked>();
            int i = 0;

            foreach (var poi in data)
            {
                if (poi.TagIds != null && poi.TagIds.Count > 0)
                    poi.TagIds = poi.TagIds.Distinct().ToList();

                //Update the TagIds
                await GenericTaggingHelper.AddTagIdsToODHActivityPoi(
                    poi,
                    await GenericTaggingHelper.GetAllGenericTagsfromJson(settings.JsonConfig.Jsondir)
                );

                //Ensure LTSTagIds are into TagIds
                if (poi.LTSTags != null && poi.LTSTags.Count > 0)
                {
                    foreach (var ltstag in poi.LTSTags)
                    {
                        if (!poi.TagIds.Contains(ltstag.LTSRID))
                            poi.TagIds.Add(ltstag.LTSRID);
                    }
                }

                //Recreate Tags
                await poi.UpdateTagsExtension(QueryFactory);

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("smgpois")
                    .Where("id", poi.Id)
                    .UpdateAsync(new JsonBData() { id = poi.Id, data = new JsonRaw(poi) });

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllSTAVendingpoints()
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("smgpois")
                .Where("gen_source", "sta");

            var data = await query.GetObjectListAsync<ODHActivityPoiLinked>();
            int i = 0;

            foreach (var stapoi in data)
            {
                //Setting MetaInfo
                stapoi._Meta.Reduced = false;
                stapoi.Source = "sta";

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("smgpois")
                    .Where("id", stapoi.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = stapoi.Id?.ToLower() ?? "",
                            data = new JsonRaw(stapoi),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> CleanODHActivityPoiNullTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("smgpois")
                .WhereRaw("data->'TagIds' @> '\\[null\\]'::jsonb")
                .Where("gen_source", "lts")
                .FilterReducedDataByRoles(new List<string>() { "IDM" });

            var data = await query.GetObjectListAsync<ODHActivityPoiLinked>();
            int i = 0;

            foreach (var stapoi in data)
            {
                bool save = false;

                if (stapoi.TagIds != null)
                {
                    if (stapoi.TagIds.Contains(null))
                    {
                        stapoi.TagIds.Remove(null);
                        save = true;
                    }


                    if (save)
                    {
                        //Save tp DB
                        //TODO CHECK IF THIS WORKS
                        var queryresult = await QueryFactory
                            .Query("smgpois")
                            .Where("id", stapoi.Id)
                            .UpdateAsync(
                                new JsonBData()
                                {
                                    id = stapoi.Id,
                                    data = new JsonRaw(stapoi),
                                }
                            );

                        i++;

                    }

                }
            }

            return i;
        }


        #endregion

        #region EventShort

        public async Task<int> CleanEventShortstEventDocumentField()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data.Where(x => x.Documents != null))
            {
                bool resave = false;

                List<string> keystoremove = new List<string>();

                foreach (var kvp in eventshort.Documents)
                {
                    if (kvp.Value == null || kvp.Value.Count == 0)
                    {
                        keystoremove.Add(kvp.Key);
                        resave = true;
                    }
                }
                foreach (string key in keystoremove)
                {
                    eventshort.Documents.Remove(key);
                }

                if (resave)
                {
                    //Save tp DB
                    //TODO CHECK IF THIS WORKS
                    var queryresult = await QueryFactory
                        .Query("eventeuracnoi")
                        .Where("id", eventshort.Id)
                        //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = eventshort.Id?.ToLower() ?? "",
                                data = new JsonRaw(eventshort),
                            }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortBrokenLinks()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                bool resave = false;

                if (eventshort.ImageGallery != null && eventshort.ImageGallery.Count > 0)
                {
                    //ImageGallery link
                    foreach (var image in eventshort.ImageGallery)
                    {
                        //https://tourism.opendatahub.com/imageresizer/ImageHandler.ashx?src=images/eventshort/

                        if (
                            image.ImageUrl.Contains(
                                "imageresizer/ImageHandler.ashx?src=images/eventshort/"
                            )
                        )
                        {
                            if (image.ImageUrl.StartsWith("https"))
                                image.ImageUrl = image.ImageUrl.Replace(
                                    "https://tourism.opendatahub.com/imageresizer/ImageHandler.ashx?src=images/eventshort/eventshort/",
                                    "https://tourism.images.opendatahub.com/api/Image/GetImage?imageurl="
                                );
                            else
                                image.ImageUrl = image.ImageUrl.Replace(
                                    "http://tourism.opendatahub.com/imageresizer/ImageHandler.ashx?src=images/eventshort/eventshort/",
                                    "https://tourism.images.opendatahub.com/api/Image/GetImage?imageurl="
                                );

                            resave = true;
                        }
                    }
                }

                if (eventshort.EventDocument != null && eventshort.EventDocument.Count > 0)
                {
                    //EventDocument link
                    foreach (var doc in eventshort.EventDocument)
                    {
                        if (doc.DocumentURL.Contains("imageresizer/images/eventshort/pdf/"))
                        {
                            if (doc.DocumentURL.StartsWith("https"))
                                doc.DocumentURL = doc.DocumentURL.Replace(
                                    "https://tourism.opendatahub.com/imageresizer/images/eventshort/pdf/",
                                    "https://tourism.images.opendatahub.com/api/File/GetFile/"
                                );
                            else
                                doc.DocumentURL = doc.DocumentURL.Replace(
                                    "http://tourism.opendatahub.com/imageresizer/images/eventshort/pdf/",
                                    "https://tourism.images.opendatahub.com/api/File/GetFile/"
                                );

                            resave = true;
                        }
                    }
                }

                if (resave)
                {
                    //Save tp DB
                    var queryresult = await QueryFactory
                        .Query("eventeuracnoi")
                        .Where("id", eventshort.Id)
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = eventshort.Id?.ToLower() ?? "",
                                data = new JsonRaw(eventshort),
                            }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortPublisherInfo(string id)
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi").When(id != "all", x => x.Where("id", id));

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                PublishedOnHelper.CreatePublishedOnList<EventShortLinked>(eventshort);

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortstEventDocumentField()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                var save = false;

                if (eventshort.EventDocument != null && eventshort.EventDocument.Count > 0)
                {
                    var eventshortdocsde = eventshort
                        .EventDocument.Where(x => x.Language == "de")
                        .Select(x => new Document
                        {
                            Language = x.Language,
                            DocumentName = "",
                            DocumentURL = x.DocumentURL,
                        })
                        .ToList();
                    if (eventshortdocsde != null && eventshortdocsde.Count > 0)
                    {
                        save = true;
                        eventshort.Documents.TryAddOrUpdate("de", eventshortdocsde);
                    }

                    var eventshortdocsit = eventshort
                        .EventDocument.Where(x => x.Language == "it")
                        .Select(x => new Document
                        {
                            Language = x.Language,
                            DocumentName = "",
                            DocumentURL = x.DocumentURL,
                        })
                        .ToList();
                    if (eventshortdocsit != null && eventshortdocsit.Count > 0)
                    {
                        save = true;
                        eventshort.Documents.TryAddOrUpdate("it", eventshortdocsit);
                    }

                    var eventshortdocsen = eventshort
                        .EventDocument.Where(x => x.Language == "en")
                        .Select(x => new Document
                        {
                            Language = x.Language,
                            DocumentName = "",
                            DocumentURL = x.DocumentURL,
                        })
                        .ToList();
                    if (eventshortdocsen != null && eventshortdocsen.Count > 0)
                    {
                        save = true;
                        eventshort.Documents.TryAddOrUpdate("en", eventshortdocsen);
                    }
                }

                if (save)
                {
                    //Save tp DB
                    //TODO CHECK IF THIS WORKS
                    var queryresult = await QueryFactory
                        .Query("eventeuracnoi")
                        .Where("id", eventshort.Id)
                        //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = eventshort.Id?.ToLower() ?? "",
                                data = new JsonRaw(eventshort),
                            }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortstActiveTodayField()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            //foreach (var eventshort in data)
            //{
            //    if (eventshort.Display1 == "Y")
            //        eventshort.ActiveToday = true;
            //    if (eventshort.Display1 == "N")
            //        eventshort.ActiveToday = false;

            //    //Save tp DB
            //    //TODO CHECK IF THIS WORKS
            //    var queryresult = await QueryFactory.Query("eventeuracnoi").Where("id", eventshort.Id)
            //        //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
            //        .UpdateAsync(new JsonBData() { id = eventshort.Id?.ToLower() ?? "", data = new JsonRaw(eventshort) });

            //    i++;
            //}

            return i;
        }

        public async Task<int> UpdateAllEventShortstonewDataModelV2()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                if (!String.IsNullOrEmpty(eventshort.EventTextDE))
                    eventshort.EventText.TryAddOrUpdate("de", eventshort.EventTextDE);
                //Beschreibung IT
                if (!String.IsNullOrEmpty(eventshort.EventTextIT))
                    eventshort.EventText.TryAddOrUpdate("it", eventshort.EventTextIT);
                //Beschreibung EN
                if (!String.IsNullOrEmpty(eventshort.EventTextEN))
                    eventshort.EventText.TryAddOrUpdate("en", eventshort.EventTextEN);

                if (!String.IsNullOrEmpty(eventshort.EventDescriptionDE))
                    eventshort.EventTitle.TryAddOrUpdate("de", eventshort.EventDescriptionDE);
                //Beschreibung IT
                if (!String.IsNullOrEmpty(eventshort.EventDescriptionIT))
                    eventshort.EventTitle.TryAddOrUpdate("it", eventshort.EventDescriptionIT);
                //Beschreibung EN
                if (!String.IsNullOrEmpty(eventshort.EventDescriptionEN))
                    eventshort.EventTitle.TryAddOrUpdate("en", eventshort.EventDescriptionEN);

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortstonewDataModel()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                if (eventshort.LastChange == null)
                    eventshort.LastChange = eventshort.ChangedOn;

                //Setting MetaInfo
                eventshort._Meta = MetadataHelper.GetMetadataobject<EventShortLinked>(
                    eventshort,
                    MetadataHelper.GetMetadataforEventShort
                );
                eventshort._Meta.LastUpdate = eventshort.LastChange;

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortstActiveFieldToTrue()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                eventshort.Active = true;

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortstHasLanguage()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                eventshort.CheckMyInsertedLanguages();

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> FillEventShortTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            //Load all eventshortdata from PG
            var queryeventshorttypes = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("eventshorttypes");

            var eventshorttypes = await queryeventshorttypes.GetObjectListAsync<SmgPoiTypes>();

            foreach (var eventshort in data)
            {
                if (
                    (eventshort.CustomTagging != null && eventshort.CustomTagging.Count > 0)
                    || (
                        eventshort.TechnologyFields != null && eventshort.TechnologyFields.Count > 0
                    )
                )
                {
                    if (eventshort.TagIds == null)
                        eventshort.TagIds = new List<string>();

                    //TODO TRANSFORM KEYS used in CustomTagging and TechnologyFields to IDs!


                    //Add CustomTagging + Technologyfields to Tags
                    foreach (var tag in eventshort.CustomTagging ?? new List<string>())
                    {
                        if (!String.IsNullOrEmpty(tag))
                        {
                            //Search by KEy
                            var toadd = eventshorttypes.Where(x => x.Key == tag).FirstOrDefault();
                            if (toadd != null)
                            {
                                if (!eventshort.TagIds.Contains(toadd.Id))
                                {
                                    eventshort.TagIds.Add(toadd.Id);
                                }
                            }
                        }
                    }
                    foreach (var technofields in eventshort.TechnologyFields ?? new List<string>())
                    {
                        if (!String.IsNullOrEmpty(technofields))
                        {
                            //Search by KEy
                            var toadd = eventshorttypes
                                .Where(x => x.Key == technofields)
                                .FirstOrDefault();
                            if (toadd != null)
                            {
                                if (!eventshort.TagIds.Contains(toadd.Id))
                                {
                                    eventshort.TagIds.Add(toadd.Id);
                                }
                            }
                        }
                    }

                    //Populate Tags (Id/Source/Type)
                    await eventshort.UpdateTagsExtension(QueryFactory);

                    //Save tp DB
                    var queryresult = await QueryFactory
                        .Query("eventeuracnoi")
                        .Where("id", eventshort.Id)
                        //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = eventshort.Id?.ToLower() ?? "",
                                data = new JsonRaw(eventshort),
                            }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> ResaveEventShortWithTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {
                if (
                    eventshort.TagIds != null
                    && eventshort.TagIds.Count != eventshort.TagIds.Distinct().Count()
                )
                {
                    eventshort.TagIds = eventshort.TagIds.Distinct().ToList();
                }

                //Save tp DB
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> UpdateAllEventShortsDetailDataModel()
        {
            //Load all data from PG and resave with Detail Object
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi");

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            foreach (var eventshort in data)
            {

                eventshort.Detail = new Dictionary<string, Detail>();

                foreach (var eventtitle in eventshort.EventTitle)
                {
                    Detail detail = new Detail();
                    detail.Language = eventtitle.Key;
                    detail.Title = eventtitle.Value;
                    eventshort.Detail.TryAddOrUpdate(eventtitle.Key, detail);
                }

                foreach (var eventtext in eventshort.EventText)
                {
                    Detail detail = new Detail();
                    if (eventshort.Detail.ContainsKey(eventtext.Key))
                        detail = eventshort.Detail[eventtext.Key];

                    detail.Language = eventtext.Key;
                    detail.BaseText = eventtext.Value;
                    eventshort.Detail.TryAddOrUpdate(eventtext.Key, detail);
                }


                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("eventeuracnoi")
                    .Where("id", eventshort.Id)
                    //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = eventshort.Id?.ToLower() ?? "",
                            data = new JsonRaw(eventshort),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> SaveEventShortsToEvents(string idlist, string lastchange)
        {
            //Load all data from PG and resave with Detail Object
            var query = QueryFactory.Query().SelectRaw("data").From("eventeuracnoi")
                .IdIlikeFilter(!String.IsNullOrEmpty(idlist) ? idlist.Split(",") : new List<string>() { })
                .SourceFilter_GeneratedColumn(new List<string>() { "noi", "ebms", "eurac", "unibz", "nobis" })
                .LastChangedFilter_GeneratedColumn(lastchange);

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            var total = data.Count();

            foreach (var eventshort in data)
            {
                var eventv1 = EventEventShortConverter.ConvertEventShortToEventByType(eventshort, false, null, false).FirstOrDefault();

                if (eventv1 != null)
                {
                    //Check if present
                    var datapresent = await QueryFactory
                        .Query("events")
                        .Select("data")
                        .Where("id", eventv1.Id)
                        .FirstOrDefaultAsync<JsonRaw>();

                    if (datapresent != null)
                    {
                        //Save tp DB
                        //TODO CHECK IF THIS WORKS
                        var queryresult = await QueryFactory
                        .Query("events")
                        .Where("id", eventv1.Id)
                        //.UpdateAsync(new JsonBData() { id = eventshort.Id.ToLower(), data = new JsonRaw(eventshort) });
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = eventv1.Id?.ToLower() ?? "",
                                data = new JsonRaw(eventv1)
                            }
                         );

                        i += queryresult;
                    }
                    else
                    {
                        //Update to DB                        
                        var queryresult = await QueryFactory
                            .Query("events")
                            .InsertAsync(
                                new JsonBData()
                                {
                                    id = eventv1.Id?.ToLower(),
                                    data = new JsonRaw(eventv1)
                                }
                        );

                        i += queryresult;
                    }
                }
            }

            return i;
        }

        public async Task<int> SaveEventShortsToVenues()
        {
            //Load all data from PG and resave with Detail Object
            var query = QueryFactory
                .Query()
                .SelectRaw("data").From("eventeuracnoi")
                .SourceFilter_GeneratedColumn(new List<string>() { "noi", "ebms", "eurac", "unibz", "nobis" });

            var data = await query.GetObjectListAsync<EventShortLinked>();
            int i = 0;

            var venuelist = EventEventShortConverter.ConvertEventShortsToVenueList(data);

            var total = data.Count();

            foreach (var venue in venuelist)
            {                
                if (venue != null)
                {
                    //Check if present
                    var datapresent = await QueryFactory
                        .Query("venues")
                        .Select("data")
                        .Where("id", venue.Id)
                        .GetObjectSingleAsync<VenueV2>();

                    if (datapresent != null)
                    {
                        //preservce Mapping, Shortname and Detail                        
                        venue.Mapping = datapresent.Mapping;
                        venue.Detail = datapresent.Detail;
                        venue.Shortname = datapresent.Shortname;
                        venue.ContactInfos = datapresent.ContactInfos;
                        venue.GpsInfo = datapresent.GpsInfo;
                        venue.ImageGallery = datapresent.ImageGallery;
                        venue.Source = datapresent.Source;
                        venue.TagIds = datapresent.TagIds;
                        venue.Tags = datapresent.Tags;
                        venue.HasLanguage = datapresent.HasLanguage;
                        venue._Meta = datapresent._Meta;
                        venue.Active = datapresent.Active;
                        venue.DistanceInfo = datapresent.DistanceInfo;
                        //venue.DistrictId = datapresent.DistrictId;
                        venue.LocationInfo = datapresent.LocationInfo;
                        venue.OperationSchedule = datapresent.OperationSchedule;
                        venue.PublishedOn = datapresent.PublishedOn;
                        venue.FirstImport = datapresent.FirstImport;
                        venue.LicenseInfo = datapresent.LicenseInfo;
                        venue.RelatedContent = datapresent.RelatedContent;
                        venue.AdditionalProperties = datapresent.AdditionalProperties;                        

                        //Update to DB                        
                        var queryresult = await QueryFactory
                            .Query("venues")
                            .Where("id", venue.Id)
                            .UpdateAsync(
                                new JsonBData()
                                {
                                    id = venue.Id?.ToLower() ?? "",
                                    data = new JsonRaw(venue)
                                }
                        );

                        i += queryresult;
                    }
                    else
                    {


                        //Update to DB                        
                        var queryresult = await QueryFactory
                            .Query("venues")
                            .InsertAsync(
                                new JsonBData()
                                {
                                    id = venue.Id?.ToLower(),
                                    data = new JsonRaw(venue)
                                }
                        );

                        i += queryresult;
                    }                        
                }
            }

            return i;
        }
     

        #endregion

        #region Wine

        public async Task<int> UpdateAllWineHasLanguage()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("wines");

            var data = await query.GetObjectListAsync<WineLinked>();
            int i = 0;

            foreach (var wine in data)
            {
                wine.CheckMyInsertedLanguages(new List<string>() { "de", "it", "en" });

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("wines")
                    .Where("id", wine.Id)
                    .UpdateAsync(
                        new JsonBData() { id = wine.Id?.ToLower() ?? "", data = new JsonRaw(wine) }
                    );

                i++;
            }

            return i;
        }

        #endregion

        #region ODHTags

        public async Task<int> UpdateAllODHTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("smgtags");

            var data = await query.GetObjectListAsync<ODHTagLinked>();
            int i = 0;

            foreach (var odhtag in data)
            {
                //Setting LicenseInfo
                //Adding LicenseInfo to ODHTag (not present on sinfo instance)
                odhtag.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject<ODHTagLinked>(
                    odhtag,
                    Helper.LicenseHelper.GetLicenseforODHTag
                );

                //Save tp DB
                //TODO CHECK IF THIS WORKS
                var queryresult = await QueryFactory
                    .Query("smgtags")
                    .Where("id", odhtag.Id)
                    .UpdateAsync(
                        new JsonBData()
                        {
                            id = odhtag.Id?.ToLower() ?? "",
                            data = new JsonRaw(odhtag),
                        }
                    );

                i++;
            }

            return i;
        }

        public async Task<Tuple<int, List<IDictionary<string, NotifierResponse>>>> RemoveODHTagsFromEntity<T>(string type, string table, string tagname, IOdhPushNotifier odhpushnotifier)
            where T : IIdentifiable, IPublishedOn
        {

            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From(table)
                .SmgTagFilterOr_GeneratedColumn(new List<string>() { tagname })
                .FilterReducedDataByRoles(new List<string>() { "IDM" });

            var list = await query.GetObjectListAsync<T>();

            int i = 0;
            List<IDictionary<string, NotifierResponse>> pushresults = new List<IDictionary<string, NotifierResponse>>();

            foreach (var jdata in list)
            {
                if (jdata is ISmgTags)
                {
                    bool update = false;
                    if ((jdata as ISmgTags).SmgTags != null && (jdata as ISmgTags).SmgTags.Contains(tagname))
                    {
                        update = true;
                        (jdata as ISmgTags).SmgTags.Remove(tagname);
                    }

                    if (update)
                    {
                        //Save tp DB                        
                        var queryresult = await QueryFactory
                            .Query(table)
                            .Where("id", jdata.Id)
                            .UpdateAsync(
                                new JsonBData()
                                {
                                    id = jdata.Id,
                                    data = new JsonRaw(jdata),
                                }
                            );

                        //Push Data if changed
                        //push modified data to all published Channels
                        var pushed = await ImportUtils.CheckIfObjectChangedAndPush(
                            odhpushnotifier,
                            new PGCRUDResult() { id = jdata.Id, objectchanged = 1, compareobject = true, updated = 1, pushchannels = jdata.PublishedOn },
                            jdata.Id,
                            type,
                            null,
                            type + ".custommodify.update"
                        );

                        if (pushed != null)
                            pushresults.Add(pushed);

                        i++;
                    }
                }
            }

            return Tuple.Create(i, pushresults);
        }


        #endregion

        #region Tags

        public async Task<int> ResaveTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("tags");

            var data = await query.GetObjectListAsync<TagLinked>();
            int i = 0;

            foreach (var tag in data)
            {
                tag._Meta.Type = "tag";

                //Save to DB
                var queryresult = await QueryFactory
                    .Query("tags")
                    .Where("id", tag.Id)
                    .UpdateAsync(
                        new JsonBData() { id = tag.Id?.ToLower() ?? "", data = new JsonRaw(tag) }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> TagSourceFix()
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("data")
                .From("tags")
                .TagTypesFilter(new List<string>() { "ltscategory", "odhcategory" });

            var data = await query.GetObjectListAsync<TagLinked>();
            int i = 0;

            foreach (var tag in data)
            {
                var source = "idm";

                tag.Source = source;
                tag._Meta.Source = source;

                //Save to DB
                var queryresult = await QueryFactory
                    .Query("tags")
                    .Where("id", tag.Id)
                    .UpdateAsync(
                        new JsonBData() { id = tag.Id?.ToLower() ?? "", data = new JsonRaw(tag) }
                    );

                i++;
            }

            return i;
        }

        public async Task<int> TagTypesFix()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("tags");

            var data = await query.GetObjectListAsync<TagLinked>();
            int i = 0;

            foreach (var tag in data)
            {
                if (tag.Types != null && tag.Types.Count > 0)
                {
                    tag.Types = tag.Types.Select(x => x.ToLower()).ToList();

                    //Save to DB
                    var queryresult = await QueryFactory
                        .Query("tags")
                        .Where("id", tag.Id)
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = tag.Id?.ToLower() ?? "",
                                data = new JsonRaw(tag),
                            }
                        );

                    i++;
                }
            }

            return i;
        }

        public async Task<int> TagParentIdFix()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("tags")
                    .TagTypesFilter(new List<string>() { "ltscategory", "odhcategory" });


            var data = await query.GetObjectListAsync<TagLinked>();
            int i = 0;

            foreach (var tag in data)
            {
                if (tag.Mapping != null && tag.Mapping.ContainsKey("lts"))
                {
                    if (tag.Mapping["lts"].ContainsKey("parent_id"))
                    {
                        if (tag.LTSTaggingInfo != null && tag.LTSTaggingInfo.ParentLTSRID != null)
                        {
                            tag.Mapping["lts"]["parent_id"] = tag.LTSTaggingInfo.ParentLTSRID;

                            //Save to DB
                            var queryresult = await QueryFactory
                                .Query("tags")
                                .Where("id", tag.Id)
                                .UpdateAsync(
                                    new JsonBData()
                                    {
                                        id = tag.Id?.ToLower() ?? "",
                                        data = new JsonRaw(tag),
                                    }
                                );

                            i++;
                        }

                    }


                }
            }

            return i;
        }

        public async Task<int> EventTopicsToTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventtypes");

            var data = await query.GetObjectListAsync<EventTypes>();
            int i = 0;

            foreach (var topic in data)
            {
                TagLinked tag = new TagLinked();

                tag.Id = topic.Id;
                tag.Source = "lts";
                tag.TagName = topic.TypeDesc;
                tag._Meta = new Metadata()
                {
                    Id = tag.Id,
                    LastUpdate = DateTime.Now,
                    Reduced = false,
                    Source = "lts",
                    Type = "tag",
                    UpdateInfo = new UpdateInfo()
                    {
                        UpdatedBy = "import",
                        UpdateSource = "importer",
                    },
                };
                tag.DisplayAsCategory = false;
                tag.ValidForEntity = new List<string>() { "event" };
                tag.MainEntity = "event";
                tag.LastChange = DateTime.Now;
                tag.LicenseInfo = new LicenseInfo()
                {
                    Author = "https://lts.it",
                    ClosedData = false,
                    License = "CC0",
                    LicenseHolder = "https://lts.it",
                };
                tag.Shortname = tag.TagName.ContainsKey("en")
                    ? tag.TagName["en"]
                    : tag.TagName.FirstOrDefault().Value;
                tag.FirstImport = DateTime.Now;
                tag.PublishedOn = null;
                tag.Types = new List<string>() { "eventtopic", "eventcategory" };

                tag.PublishDataWithTagOn = null;
                tag.Mapping = null;
                tag.IDMCategoryMapping = null;
                tag.LTSTaggingInfo = null;
                tag.MappedTagIds = null;

                var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                    tag,
                    new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("tag.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                i++;
            }

            return i;
        }

        public async Task<int> EventShortTypesToTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("eventshorttypes");

            var data = await query.GetObjectListAsync<SmgPoiTypes>();
            int i = 0;

            foreach (var topic in data)
            {
                TagLinked tag = new TagLinked();

                tag.Id = topic.Id;
                tag.Source = "noi";
                tag.TagName = topic.TypeDesc;
                tag._Meta = new Metadata()
                {
                    Id = tag.Id,
                    LastUpdate = DateTime.Now,
                    Reduced = false,
                    Source = "noi",
                    Type = "tag",
                    UpdateInfo = new UpdateInfo()
                    {
                        UpdatedBy = "import",
                        UpdateSource = "importer",
                    },
                };
                tag.DisplayAsCategory = false;
                tag.ValidForEntity = new List<string>() { "event", "eventshort" };
                tag.MainEntity = "event";
                tag.LastChange = DateTime.Now;
                tag.LicenseInfo = new LicenseInfo()
                {
                    Author = "https://lts.it",
                    ClosedData = false,
                    License = "CC0",
                    LicenseHolder = "https://lts.it",
                };
                tag.Shortname = tag.TagName.ContainsKey("en")
                    ? tag.TagName["en"]
                    : tag.TagName.FirstOrDefault().Value;
                tag.FirstImport = DateTime.Now;
                tag.PublishedOn = null;
                tag.Types = new List<string>() { topic.Type.ToLower() };

                tag.PublishDataWithTagOn = null;
                tag.Mapping = null;
                tag.IDMCategoryMapping = null;
                tag.LTSTaggingInfo = null;
                tag.MappedTagIds = null;

                var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                    tag,
                    new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("tag.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                i++;
            }

            return i;
        }

        public async Task<int> GastronomyTypesToTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("gastronomytypes");

            var data = await query.GetObjectListAsync<GastronomyTypes>();
            int i = 0;

            foreach (var topic in data)
            {
                TagLinked tag = new TagLinked();

                tag.Id = topic.Id;
                tag.Source = "lts";
                tag.TagName = topic.TypeDesc;
                tag._Meta = new Metadata()
                {
                    Id = tag.Id,
                    LastUpdate = DateTime.Now,
                    Reduced = false,
                    Source = "lts",
                    Type = "tag",
                    UpdateInfo = new UpdateInfo()
                    {
                        UpdatedBy = "import",
                        UpdateSource = "importer",
                    },
                };
                tag.DisplayAsCategory = false;
                tag.ValidForEntity = new List<string>() { "odhactivitypoi", "gastronomy" };
                tag.MainEntity = "odhactivitypoi";
                tag.LastChange = DateTime.Now;
                tag.LicenseInfo = new LicenseInfo()
                {
                    Author = "https://lts.it",
                    ClosedData = false,
                    License = "CC0",
                    LicenseHolder = "https://lts.it",
                };
                tag.Shortname = tag.TagName.ContainsKey("en")
                    ? tag.TagName["en"]
                    : tag.TagName.FirstOrDefault().Value;
                tag.FirstImport = DateTime.Now;
                tag.PublishedOn = null;
                tag.Types = new List<string>() { topic.Type.ToLower() };

                tag.PublishDataWithTagOn = null;
                tag.Mapping = null;
                tag.IDMCategoryMapping = null;
                tag.LTSTaggingInfo = null;
                tag.MappedTagIds = null;

                var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                    tag,
                    new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("tag.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                i++;
            }

            return i;
        }

        public async Task<int> VenueTypesToTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("venuetypes");

            var data = await query.GetObjectListAsync<DDVenueCodes>();
            int i = 0;

            foreach (var topic in data)
            {
                TagLinked tag = new TagLinked();

                tag.Id = topic.Id;
                tag.Source = "lts";
                tag.TagName = topic.TypeDesc;
                tag._Meta = new Metadata()
                {
                    Id = tag.Id,
                    LastUpdate = DateTime.Now,
                    Reduced = false,
                    Source = "lts",
                    Type = "tag",
                    UpdateInfo = new UpdateInfo()
                    {
                        UpdatedBy = "import",
                        UpdateSource = "importer",
                    },
                };
                tag.DisplayAsCategory = false;
                tag.ValidForEntity = new List<string>() { "venue" };
                tag.MainEntity = "venue";
                tag.LastChange = DateTime.Now;
                tag.LicenseInfo = new LicenseInfo()
                {
                    Author = "https://lts.it",
                    ClosedData = false,
                    License = "CC0",
                    LicenseHolder = "https://lts.it",
                };
                tag.Shortname = tag.TagName.ContainsKey("en")
                    ? tag.TagName["en"]
                    : tag.TagName.FirstOrDefault().Value;
                tag.FirstImport = DateTime.Now;
                tag.PublishedOn = null;
                tag.Types = new List<string>() { "venue" + topic.Type.ToLower() };

                tag.PublishDataWithTagOn = null;
                tag.Mapping = null;
                tag.IDMCategoryMapping = null;
                tag.LTSTaggingInfo = null;
                tag.MappedTagIds = null;

                var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                    tag,
                    new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("tag.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                i++;
            }

            return i;
        }

        public async Task<int> VenueSeatTypesToTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("venuetypes").WhereRaw("data->>'Type' = 'seatType'");

            var data = await query.GetObjectListAsync<DDVenueCodes>();
            int i = 0;

            foreach (var topic in data)
            {
                TagLinked tag = new TagLinked();

                tag.Id = topic.Id;
                tag.Source = "lts";
                tag.TagName = topic.TypeDesc;
                tag._Meta = new Metadata()
                {
                    Id = tag.Id,
                    LastUpdate = DateTime.Now,
                    Reduced = false,
                    Source = "lts",
                    Type = "tag",
                    UpdateInfo = new UpdateInfo()
                    {
                        UpdatedBy = "import",
                        UpdateSource = "importer",
                    },
                };
                tag.DisplayAsCategory = false;
                tag.ValidForEntity = new List<string>() { "venue" };
                tag.MainEntity = "venue";
                tag.LastChange = DateTime.Now;
                tag.LicenseInfo = new LicenseInfo()
                {
                    Author = "https://lts.it",
                    ClosedData = false,
                    License = "CC0",
                    LicenseHolder = "https://lts.it",
                };
                tag.Shortname = tag.TagName.ContainsKey("en")
                    ? tag.TagName["en"]
                    : tag.TagName.FirstOrDefault().Value;
                tag.FirstImport = DateTime.Now;
                tag.PublishedOn = null;
                tag.Types = new List<string>() { "seattype" };

                tag.PublishDataWithTagOn = null;
                tag.Mapping = null;
                tag.IDMCategoryMapping = null;
                tag.LTSTaggingInfo = null;
                tag.MappedTagIds = null;

                var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                    tag,
                    new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("tag.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                i++;
            }

            return i;
        }

        public async Task<int> ArticleTypesToTags()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("articletypes");

            var data = await query.GetObjectListAsync<ArticleTypes>();
            int i = 0;

            foreach (var topic in data)
            {
                TagLinked tag = new TagLinked();

                tag.Id = topic.Id;
                tag.Source = "idm"; //TO CHECK
                tag.TagName = topic.TypeDesc; //TO CHECK
                tag._Meta = new Metadata()
                {
                    Id = tag.Id,
                    LastUpdate = DateTime.Now,
                    Reduced = false,
                    Source = "idm",
                    Type = "tag",
                    UpdateInfo = new UpdateInfo()
                    {
                        UpdatedBy = "import",
                        UpdateSource = "importer",
                    },
                };
                tag.DisplayAsCategory = false;
                tag.ValidForEntity = new List<string>() { "article" };
                tag.MainEntity = "article";
                tag.LastChange = DateTime.Now;
                tag.LicenseInfo = new LicenseInfo()
                {
                    Author = "https://lts.it",
                    ClosedData = false,
                    License = "CC0",
                    LicenseHolder = "https://lts.it",
                };
                tag.Shortname = tag.TagName.ContainsKey("en")
                    ? tag.TagName["en"]
                    : tag.TagName.FirstOrDefault().Value;
                tag.FirstImport = DateTime.Now;
                tag.PublishedOn = null;
                tag.Types = new List<string>() { topic.Type.ToLower() };

                tag.PublishDataWithTagOn = null;
                tag.Mapping = null;
                tag.IDMCategoryMapping = null;
                tag.LTSTaggingInfo = null;
                tag.MappedTagIds = null;

                var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                    tag,
                    new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                    new EditInfo("tag.modify", "importer"),
                    new CRUDConstraints(),
                    new CompareConfig(false, false)
                );

                i++;
            }

            return i;
        }

        public async Task<int> TranslatedODHTagsFix()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("smgtags")
                  .ODHTagSourcesFilter_GeneratedColumn(new List<string>() { "odhcategory", "ltscategory", "LTSCategory", "ODHCategory" }); //Using custom source filter

            var data = await query.GetObjectListAsync<ODHTagLinked>();
            int i = 0;

            List<string> notfoundtags = new List<string>();

            foreach (var odhtag in data)
            {

                var tagquery = QueryFactory
                    .Query("tags")
                    .Select("data")
                    .WhereInJsonb(new List<string>() { odhtag.Id }, tag => new { ODHTagIds = new[] { tag.ToLower() } });

                var taglist = await tagquery.GetObjectListAsync<TagLinked>();

                var tag = taglist.FirstOrDefault();

                if (tag != null)
                {
                    bool save = false;
                    if (odhtag.TagName.ContainsKey("cs") || odhtag.TagName.ContainsKey("fr") || odhtag.TagName.ContainsKey("nl") || odhtag.TagName.ContainsKey("pl"))
                        save = true;

                    //Update Translations in FR/CS/PL/NL                                            
                    if (odhtag.TagName.ContainsKey("cs"))
                        tag.TagName.TryAddOrUpdate("cs", odhtag.TagName["cs"]);

                    if (odhtag.TagName.ContainsKey("fr"))
                        tag.TagName.TryAddOrUpdate("fr", odhtag.TagName["fr"]);

                    if (odhtag.TagName.ContainsKey("nl"))
                        tag.TagName.TryAddOrUpdate("nl", odhtag.TagName["nl"]);

                    if (odhtag.TagName.ContainsKey("pl"))
                        tag.TagName.TryAddOrUpdate("pl", odhtag.TagName["pl"]);


                    if (save)
                    {
                        var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                           tag,
                           new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                           new EditInfo("tag.modify", "importer"),
                           new CRUDConstraints(),
                           new CompareConfig(false, false)
                       );

                        if (pgcrudresult.updated != null && pgcrudresult.updated > 0)
                            i++;
                    }


                }
                else
                {
                    //cannot find tag
                    notfoundtags.Add(odhtag.Id);
                }
            }

            return i;
        }

        public async Task<int> TagsValidForEntityFix()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("tags")
                  .SourceFilter_GeneratedColumn(new List<string>() { "idm" });

            var data = await query.GetObjectListAsync<TagLinked>();
            int i = 0;

            List<string> updatedid = new List<string>();

            foreach (var tag in data)
            {
                if (tag.ValidForEntity != null && tag.ValidForEntity.Contains("smgpoi") && !tag.ValidForEntity.Contains("odhactivitypoi"))
                {
                    tag.ValidForEntity.Add("odhactivitypoi");
                    updatedid.Add(tag.Id);

                    var pgcrudresult = await QueryFactory.UpsertData<TagLinked>(
                                     tag,
                                     new DataInfo("tags", CRUDOperation.Update) { ErrorWhendataIsNew = false },
                                     new EditInfo("tag.modify", "importer"),
                                     new CRUDConstraints(),
                                     new CompareConfig(false, false)
                                 );

                    if (pgcrudresult.updated != null && pgcrudresult.updated > 0)
                        i++;
                }
            }

            return i;
        }


        #endregion

        #region GeoShape

        public async Task<int> UpdateGeoshapeMetaInfo()
        {
            //Load all data from PG and resave
            var query = QueryFactory
                .Query()
                .SelectRaw("id,name,country,type,licenseinfo,meta,mapping,source,srid")
                .From("geoshapes");

            //ST_AsText(geometry) as geometry,
            var shapes = await query.GetAsync<GeoShapeDB>();

            int i = 0;

            foreach (var shape in shapes)
            {
                var metainfo = MetadataHelper.GetMetadataobject<GeoShapeJson>(new GeoShapeJson()
                {
                    Source = shape.source,
                    Id = shape.id
                });

                shape.meta = new JsonRaw(metainfo);

                //Save tp DB
                var queryresult = await QueryFactory.Query("geoshapes").Where("id", shape.id)
                     .UpdateAsync(shape);

                i++;
            }

            return i;
        }


        #endregion

        #region ReducedIdCleanup

        public async Task<int> UpdateAllReducedIdsonData(string table)
        {
            try
            {
                //Load all data from PG and resave
                var query = QueryFactory.Query().SelectRaw("data").From(table)
                    .WhereRaw("gen_id ILIKE $$", "%_REDUCED");

                var datalist = await query.GetObjectListAsync(Helper.ODHTypeHelper.TranslateTable2Type(table));
                int i = 0;

                foreach (var data in datalist)
                {
                    string originalid = data.Id;

                    if (Helper.IdGenerator.GetIDStyle(data) == IDStyle.lowercase)
                        data.Id = data.Id.Replace("_reduced", "");
                    else
                        data.Id = data.Id.Replace("_REDUCED", "");


                    //Save tp DB
                    //TODO CHECK IF THIS WORKS
                    var queryresult = await QueryFactory
                        .Query(table)
                        .Where("id", originalid)
                        .UpdateAsync(
                            new JsonBData()
                            {
                                id = originalid,
                                data = new JsonRaw(data),
                            }
                        );

                    i++;
                }

                return i;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }


        #endregion

        #region WineAward

        public async Task<int> UpdateAllWineAwardIds()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query()
                .SelectRaw("data")
                .From("wines");

            var data = await query.GetObjectListAsync<WineLinked>();
            int i = 0;

            foreach (var wine in data)
            {
                var oldwineid = wine.Id.ToUpper();
                wine.Id = wine.Id.ToLower();
                wine._Meta.Id = wine.Id.ToLower();
                //Save to DB
                var queryresult = await QueryFactory
                    .Query("wines")
                    .Where("id", wine.Id)
                    .InsertAsync(
                        new JsonBData() { id = wine.Id, data = new JsonRaw(wine) }
                    );
                i++;

                //Delete the uppercase id if present

                var querydeleteresult = await QueryFactory
                    .Query("wines")
                    .Where("id", oldwineid)
                    .DeleteAsync();
            }

            return i;
        }




        #endregion

        #region Venue

        public async Task<Tuple<int, string>> VenueToVenueV2()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("venues_v2");

            var data = await query.GetObjectListAsync<VenueLinked>();
            int i = 0;

            List<Tuple<int, string>> results = new List<Tuple<int, string>>();

            foreach (var venue in data)
            {
                results.Add(await UpdateVenueToNewDataModel(venue));
            }

            var failed = results.Where(x => x.Item1 == 0).Select(x => x.Item2);
            var updatedcount = results.Where(x => x.Item1 > 0).Sum(x => x.Item1);

            return Tuple.Create(updatedcount, String.Join(",", failed));
        }

        public async Task<Tuple<int, string>> UpdateVenueToNewDataModel(VenueLinked venue)
        {
            try
            {
                string reduced = "";
                //If it is a reduced object
                if (venue._Meta.Reduced)
                    reduced = "_REDUCED";

                //Save tp DB
                //TODO Add all missing values
                var venue2 = new VenueV2();
                venue2.Id = venue.Id;
                venue2.Active = venue.Active;
                //venue2.AdditionalProperties 
                //venue2.Beds = venue.Beds;
                venue2.ContactInfos = venue.ContactInfos;
                venue2.Detail = venue.Detail;
                venue2.DistanceInfo = venue.DistanceInfo;
                venue2.FirstImport = venue.FirstImport;
                venue2.GpsInfo = venue.GpsInfo;
                venue2.HasLanguage = venue.HasLanguage;
                venue2.ImageGallery = venue.ImageGallery;
                venue2.LastChange = venue.LastChange;
                venue2.LicenseInfo = venue.LicenseInfo;
                venue2.LocationInfo = venue.LocationInfo;
                venue2.Mapping = venue.Mapping;
                venue2.OperationSchedule = venue.OperationSchedule;
                venue2.PublishedOn = venue.PublishedOn;
                venue2.Shortname = venue.Shortname;
                venue2.Source = venue.Source;
                venue2._Meta = venue._Meta;

                if (venue.RoomDetails != null)
                {
                    venue2.RoomDetails = new List<VenueRoomDetailsV2>();

                    foreach (var roomdetail in venue.RoomDetails)
                    {
                        VenueRoomDetailsV2 roomdetailv2 = new VenueRoomDetailsV2();
                        roomdetailv2.ImageGallery = roomdetail.ImageGallery;
                        roomdetailv2.Detail = roomdetail.Detail;
                        roomdetailv2.Id = roomdetail.Id;
                        roomdetailv2.Shortname = roomdetail.Shortname;

                        roomdetailv2.VenueRoomProperties = new VenueRoomProperties();
                        roomdetailv2.VenueRoomProperties.SquareMeters = roomdetail.SquareMeters;
                        roomdetailv2.Placement = roomdetail.Indoor != null && roomdetail.Indoor.Value ? "indoor" : null;

                        //Roomdeatil VenueFeature
                        if (roomdetail.VenueFeatures != null)
                        {
                            if (roomdetailv2.TagIds == null)
                                roomdetailv2.TagIds = new List<string>();

                            foreach (var venuefeat in roomdetail.VenueFeatures)
                            {
                                roomdetailv2.TagIds.Add(venuefeat.Id);
                            }
                        }
                        if (roomdetail.VenueSetup != null)
                        {
                            if (roomdetailv2.TagIds == null)
                            {
                                roomdetailv2.TagIds = new List<string>();
                                roomdetailv2.Tags = new List<Tags>();
                            }

                            foreach (var venuesetup in roomdetail.VenueSetup)
                            {
                                roomdetailv2.TagIds.Add(venuesetup.Id);
                                roomdetailv2.Tags.Add(new Tags() { Id = venuesetup.Id, TagEntry = new Dictionary<string, string>() { { "maxCapacity", venuesetup.Capacity.ToString() } } });
                            }
                        }

                        //Create Tags
                        await roomdetailv2.UpdateTagsExtension(QueryFactory, await FillTagsObject.GetTagEntrysToPreserve(roomdetailv2));

                        venue2.RoomDetails.Add(roomdetailv2);
                    }
                }


                //venue.VenueCategory
                if (venue.VenueCategory != null)
                {
                    venue2.TagIds = new List<string>();
                    foreach (var venuecat in venue.VenueCategory)
                    {
                        venue2.TagIds.Add(venuecat.Id);
                    }
                }

                //Create Tags
                await venue2.UpdateTagsExtension(QueryFactory);

                //not needed anymore
                //venue.RoomCount
                //venue.SyncSourceInterface
                //venue.SmgTags
                //venue.SmgActive
                //venue.ODHTags
                //venue.OdhActive




                //If Reduced use the ID without reduced
                if (venue._Meta.Reduced)
                {
                    venue2.Id = venue2.Id.Replace("_REDUCED", "");
                    venue2._Meta.Id = venue2.Id.Replace("_REDUCED", "");
                }


                var idtoupdate = venue2.Id;
                if (!venue2.Id.Contains("_REDUCED"))
                    idtoupdate = venue2.Id + reduced;

                //Clear the table and insert
                var queryresult = await QueryFactory
                    .Query("venues")
                    .Where("id", idtoupdate)
                    .InsertAsync(
                        new JsonBData()
                        {
                            id = idtoupdate,
                            data = new JsonRaw(venue2),
                        }
                    );

                return Tuple.Create<int, string>(queryresult, venue2.Id);
            }
            catch (Exception ex)
            {
                return Tuple.Create<int, string>(0, venue.Id);
            }
        }


        #endregion

        #region Municipality

        public async Task<Tuple<int, string>> UpdateMunicipalityMapping()
        {
            //Load all data from PG and resave
            var query = QueryFactory.Query().SelectRaw("data").From("municipalities");

            var data = await query.GetObjectListAsync<MunicipalityLinked>();
            int i = 0;

            List<Tuple<int, string>> results = new List<Tuple<int, string>>();

            foreach (var municipality in data)
            {
                //Add Mapping
                if (municipality.SiagId != null)
                    municipality.Mapping.TryAddOrUpdate("siag", new Dictionary<string, string>() { { "id", municipality.SiagId } });

                //CustomId ??
                if (municipality.CustomId != null)
                    municipality.Mapping.TryAddOrUpdate("idm", new Dictionary<string, string>() { { "id", municipality.CustomId } });


                if (municipality.IstatNumber != null)
                    municipality.Mapping.TryAddOrUpdate("istat", new Dictionary<string, string>() { { "istatnumber", municipality.IstatNumber }, { "inhabitants", municipality.Inhabitants.ToString() } });

                var queryresult = await QueryFactory
                   .Query("municipalities")
                   .Where("id", municipality.Id)
                   .UpdateAsync(
                       new JsonBData()
                       {
                           id = municipality.Id,
                           data = new JsonRaw(municipality),
                       }
                   );

                results.Add(Tuple.Create<int, string>(queryresult, municipality.Id));
            }

            var failed = results.Where(x => x.Item1 == 0).Select(x => x.Item2);
            var updatedcount = results.Where(x => x.Item1 > 0).Sum(x => x.Item1);

            return Tuple.Create(updatedcount, String.Join(",", failed));
        }


        #endregion
    }
}
