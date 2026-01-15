// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataModel;
using GenericHelper;
using Helper.Tagging;
using LTSAPI;
using Newtonsoft.Json.Linq;
using SqlKata.Execution;

namespace OdhApiImporter.Helpers.RAVEN
{
    public class LtsApiv2Operations
    {
        public static async Task UpdateAccommodationWithLTSV2Data(
            AccommodationV2 accommodation,
            QueryFactory queryFactory,
            Helper.ISettings settings,
            bool updatecincode,
            bool updateguestcards,
            bool updateaccoltsinfo, 
            bool updateaccessibilityinfo
        )
        {
            var ltsdata = await GetAccommodationFromLTSV2(accommodation, settings);

            //TODO IF Ltsdata is not accessible deactivate it

            if (ltsdata != null)
            {
                //Assign the CinCode
                if (updatecincode)
                    await AssignCinCodeFromNewLtsApi(accommodation, ltsdata);

                //Assign AccoLTSInfo
                if (updateaccoltsinfo)
                    await AssignAccoLTSInfoFromNewLtsApi(accommodation, ltsdata);

                //Guestcard Tag
                if (updateguestcards)
                    await AssignGuestcardDataFromNewLtsApi(accommodation, ltsdata, queryFactory);

                //Guestcard Tag
                if (updateaccessibilityinfo)
                    await AssignAccessibilityDataFromNewLtsApi(accommodation, ltsdata);
            }
        }

        private static async Task<JObject> GetAccommodationFromLTSV2(
            AccommodationV2 accommodation,
            Helper.ISettings settings
        )
        {
            try
            {
                LtsApi ltsapi = new LtsApi(
                    settings.LtsCredentials.serviceurl,
                    settings.LtsCredentials.username,
                    settings.LtsCredentials.password,
                    settings.LtsCredentials.ltsclientid,
                    false
                );
                var qs = new LTSQueryStrings()
                {
                    page_size = 1,
                    fields = "cinCode,amenities,suedtirolGuestPass,roomGroups,type,category,accessibility",   //amenities,mealPlans
                };
                var dict = ltsapi.GetLTSQSDictionary(qs);

                var ltsdata = await ltsapi.AccommodationDetailRequest(accommodation.Id, dict);

                return ltsdata.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private static async Task AssignCinCodeFromNewLtsApi(
            AccommodationV2 accommodation,
            JObject ltsdata
        )
        {
            try
            {
                //Todo parse response
                var cincode =
                    ltsdata["data"] != null ? ltsdata["data"].Value<string?>("cinCode") : null;

                //If no lts mapping is there
                if (accommodation.Mapping == null)
                    accommodation.Mapping = new Dictionary<string, IDictionary<string, string>>();

                var ltsdict = default(IDictionary<string, string>);

                if (accommodation.Mapping.ContainsKey("lts"))
                    ltsdict = accommodation.Mapping["lts"];

                if (ltsdict == null)
                    ltsdict = new Dictionary<string, string>();

                ltsdict.TryAddOrUpdate("cincode", cincode);

                accommodation.Mapping.TryAddOrUpdate("lts", ltsdict);

                GenericResultsHelper.GetSuccessUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update CinCode",
                    "single",
                    "Update CinCode success",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 1,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 0,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    true
                );
            }
            catch (Exception ex)
            {
                GenericResultsHelper.GetErrorUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update CinCode",
                    "single",
                    "Update CinCode failed",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 0,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 1,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    ex,
                    true
                );
            }
        }

        private static async Task AssignAccoLTSInfoFromNewLtsApi(
           AccommodationV2 accommodation,
           JObject ltsdata
       )
        {
            try
            {
                //Todo parse response
                var rooms =
                    ltsdata["data"] != null
                    ? ltsdata["data"]["roomGroups"] != null 
                        ? ltsdata["data"]["roomGroups"].ToObject<IList<LtsRoomGroupList>>() : null
                       : null;

                float? pricefrom = rooms != null ? rooms.Where(x => x.isActive == true && x.minAmountPerPersonPerDay != null).OrderBy(x => x.minAmountPerPersonPerDay).Select(x => x.minAmountPerPersonPerDay).FirstOrDefault() : null;
                float? pricefromperunit = rooms != null ? rooms.Where(x => x.isActive == true && x.minAmountPerUnitPerDay != null).OrderBy(x => x.minAmountPerUnitPerDay).Select(x => x.minAmountPerUnitPerDay).FirstOrDefault() : null;

                //If lts info is null
                if (accommodation.AccoLTSInfo == null)
                    accommodation.AccoLTSInfo = new AccoLTSInfo();

                accommodation.AccoLTSInfo.PriceFrom = (int?)pricefrom;
                accommodation.AccoLTSInfo.PriceFromPerUnit = (int?)pricefromperunit;

                GenericResultsHelper.GetSuccessUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update AccoLTSInfo",
                    "single",
                    "Update AccoLTSInfo success",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 1,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 0,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    true
                );
            }
            catch (Exception ex)
            {
                GenericResultsHelper.GetErrorUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update AccoLTSInfo",
                    "single",
                    "Update AccoLTSInfo failed",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 0,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 1,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    ex,
                    true
                );
            }
        }
        
        private static async Task AssignGuestcardDataFromNewLtsApi(
            AccommodationV2 accommodation,
            JObject ltsdata,
            QueryFactory queryFactory
        )
        {
            try
            {
                //Todo parse response
                var guestcardactive =
                    ltsdata["data"] != null
                        ? ltsdata["data"]["suedtirolGuestPass"] != null
                            ? ltsdata["data"]["suedtirolGuestPass"].Value<bool?>("isActive")
                            : null
                        : null;

                if (accommodation.TagIds == null)
                    accommodation.TagIds = new HashSet<string>();

                if (accommodation.SmgTags == null)
                    accommodation.SmgTags = new List<string>();

                if (accommodation.SpecialFeaturesIds == null)
                    accommodation.SpecialFeaturesIds = new List<string>();

                AccoFeatureLinked guestcardfeature = new AccoFeatureLinked() { Id = "8192350ABF6B41DA89B255B340003991", Name = "Südtirol Alto Adige Guest Pass", OtaCodes = null, RoomAmenityCodes = null, HgvId = "" };

                //IF guestcard active Add Tag "guestcard"
                if (guestcardactive == null || guestcardactive == false)
                {
                    accommodation.SmgTags.TryRemoveOnList("guestcard");
                    accommodation.TagIds.TryRemoveOnList("guestcard");
                    //NEW TO Remove, add speciafeature Guestcard
                    accommodation.SpecialFeaturesIds.TryRemoveOnList("Guestcard");
                    //NEW Remove on Features
                    if (accommodation.Features != null)
                    {
                        if (accommodation.Features.Where(x => x.Id == "8192350ABF6B41DA89B255B340003991").Count() > 0)
                        {
                            var featurestoremove = accommodation.Features.Where(x => x.Id == "8192350ABF6B41DA89B255B340003991").ToList();
                            if (featurestoremove != null)
                            {
                                foreach (var featuretoremove in featurestoremove)
                                {
                                    accommodation.Features.Remove(featuretoremove);
                                }
                            }

                        }
                    }

                }
                else
                {
                    accommodation.SmgTags.TryAddOrUpdateOnList("guestcard");
                    accommodation.TagIds.TryAddOrUpdateOnList("guestcard");
                    //NEW TO Remove, add speciafeature Guestcard
                    accommodation.SpecialFeaturesIds.TryAddOrUpdateOnList("Guestcard");
                    //Update also the Features
                    if (accommodation.Features == null)
                        accommodation.Features = new List<AccoFeatureLinked>();
                    if (accommodation.Features.Count() == 0)
                    {
                        accommodation.Features.Add(guestcardfeature);
                    }
                    else
                    {
                        if (accommodation.Features.Where(x => x.Id == "8192350ABF6B41DA89B255B340003991").Count() == 0)
                            accommodation.Features.Add(guestcardfeature);
                        else if (accommodation.Features.Where(x => x.Id == "8192350ABF6B41DA89B255B340003991").Count() == 2)
                        {
                            //Clear Duplicates
                            var featuretoremove = accommodation.Features.Where(x => x.Id == "8192350ABF6B41DA89B255B340003991").FirstOrDefault();
                            accommodation.Features.Remove(featuretoremove);
                        }
                    }

                }

                //Compatiblity features to Tags
                var amenities =
                    ltsdata["data"] != null
                        ? ltsdata["data"]["amenities"] != null
                            ? ltsdata["data"]["amenities"].ToObject<IList<LtsRidList>>()
                            : null
                        : null;

                //Check if cardtypes should be assigned
                foreach (var guestcard in GuestCardIdMapping())
                {
                    if (amenities != null && amenities.Select(x => x.rid).Contains(guestcard.Key))
                        accommodation.SmgTags.TryAddOrUpdateOnList(guestcard.Value);
                    else
                        accommodation.SmgTags.TryRemoveOnList(guestcard.Value);
                }

                //Add to Tags
                var cardtypes =
                    ltsdata["data"] != null
                        ? ltsdata["data"]["suedtirolGuestPass"] != null
                            ? ltsdata["data"]
                                ["suedtirolGuestPass"]["cardTypes"]
                                .ToObject<IList<LtsRidList>>()
                            : null
                        : null;

                if (cardtypes != null)
                {
                    foreach (var card in cardtypes)
                    {
                        accommodation.TagIds.Add(card.rid);
                    }
                }

                //Add also Type and Category to TagIds
                //Accommodationtype
                var typerid =
                    ltsdata["data"] != null
                        ? ltsdata["data"]["type"] != null
                            ? ltsdata["data"]["type"].Value<string?>("rid")
                            : null
                        : null;

                var categoryrid =
                           ltsdata["data"] != null
                               ? ltsdata["data"]["category"] != null
                                   ? ltsdata["data"]["category"].Value<string?>("rid")
                                   : null
                               : null;

                if (typerid != null)
                    accommodation.TagIds.TryAddOrUpdateOnList(typerid);
                if (categoryrid != null)
                    accommodation.TagIds.TryAddOrUpdateOnList(categoryrid);


                //Populate Tags (Id/Source/Type)
                await accommodation.UpdateTagsExtension(queryFactory);

                GenericResultsHelper.GetSuccessUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update Guestcard",
                    "single",
                    "Update Guestcard success",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 1,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 0,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    true
                );
            }
            catch (Exception ex)
            {
                GenericResultsHelper.GetErrorUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update Guestcard",
                    "single",
                    "Update Guestcard failed",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 0,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 1,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    ex,
                    true
                );
            }
        }

        private static async Task AssignAccessibilityDataFromNewLtsApi(
           AccommodationV2 accommodation,
           JObject ltsdata
       )
        {
            try
            {
                //Todo parse response
                var accessibility =
                    ltsdata["data"] != null
                    ? ltsdata["data"]["accessibility"] != null
                        ? ltsdata["data"]["accessibility"].ToObject<LtsAccessibilityAcco>() : null
                       : null;


                //If lts accessibility info is not null and we have already independentdata
                if (accessibility != null && accommodation.IndependentData != null && accommodation.IndependentData.IndependentDescription != null)
                {
                    foreach(var commitmenttoAccessibilityurl in accessibility.commitmentToAccessibilityUrl)
                    {
                        if (accommodation.IndependentData.IndependentDescription.ContainsKey(commitmenttoAccessibilityurl.Key))
                        {
                            var independentdesc = accommodation.IndependentData.IndependentDescription[commitmenttoAccessibilityurl.Key];
                            independentdesc.CommitmentToAccessibilityUrl = commitmenttoAccessibilityurl.Value;

                            accommodation.IndependentData.IndependentDescription.TryAddOrUpdate(commitmenttoAccessibilityurl.Key, independentdesc);
                        }                        
                    }                    
                }                                

                GenericResultsHelper.GetSuccessUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update AccoIndependentData",
                    "single",
                    "Update AccoIndependentData success",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 1,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 0,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    true
                );
            }
            catch (Exception ex)
            {
                GenericResultsHelper.GetErrorUpdateResult(
                    accommodation.Id,
                    "api",
                    "Update AccoIndependentData",
                    "single",
                    "Update AccoIndependentData failed",
                    "accommodation",
                    new UpdateDetail()
                    {
                        updated = 0,
                        changes = null,
                        comparedobjects = null,
                        created = 0,
                        deleted = 0,
                        error = 1,
                        objectchanged = 0,
                        objectimagechanged = 0,
                        pushed = null,
                        pushchannels = null,
                    },
                    ex,
                    true
                );
            }
        }

        private static IDictionary<string, string> GuestCardIdMapping()
        {
            return new Dictionary<string, string>()
            {
                { "035577098B254201A865684EF050C851", "bozencardplus" },
                { "CEE3703E4E3B44E3BD1BEE3F559DD31C", "rittencard" },
                { "C7758584EFDE47B398FADB6BDBD0F198", "klausencard" },
                { "C3C7ABEB0F374A0F811788B775D96AC0", "brixencard" },
                { "3D703D2EA16645BD9EA3273069A0B918", "almencardplus" },
                { "D02AE2F641A4496AB1D2C4871475293D", "activecard" },
                { "DA4CAD333B8D45448AAEA9E966C68380", "winepass" },
                { "500AEFA8868748899BEC826B5E81951C", "ultentalcard" },
                { "DE13880FA929461797146596FA3FFC07", "merancard" },
                { "49E9FF69F86846BD9915A115988C5484", "vinschgaucard" },
                { "FAEB6769EC564CBF982D454DCEEBCB27", "algundcard" },
                { "3FD7253E3F6340E1AF642EA3DE005128", "holidaypass" },
                { "24E475F20FF64D748EBE7033C2DBC3A8", "valgardenamobilcard" },
                { "056486AFBEC4471EA32B3DB658A96D48", "dolomitimobilcard" },
                { "9C8140EB332F46E794DFDDB240F9A9E4", "mobilactivcard" },
                { "8192350ABF6B41DA89B255B340003991", "suedtirolguestpass" },
                { "3CB7D42AD51C4E2BA061CF9838A3735D", "holidaypass3zinnen" },
                { "19ABB47430F64287BEA96237A2E99899", "seiseralm_balance" },
                { "D1C1C206AA0B4025A98EE83C2DBC2DFA", "workation" },
                { "C414648944CE49D38506D176C5B58486", "merancard_allyear" },
                { "6ACF61213EA347C6B1EB409D4A473B6D", "dolomiti_museumobilcard" },
                { "99803FF36D51415CAFF64183CC26F736", "sarntalcard" },
                { "B69F991C1E45422B9D457F716DEAA82B", "suedtirolguestpass_passeiertal_premium" },
                { "F4D3B02B107843C894ED517FC7DC8A39", "suedtirolguestpass_mobilcard" },
                { "895C9B57E0D54B449C82F035538D4A79", "suedtirolguestpass_museumobilcard" },
                { "742AA043BD5847C79EE93EEADF0BE0D2", "natzschabscard" },
                { "05988DB63E5146E481C95279FB285C6A", "accomodation bed bike" },
                { "5F22AD3E93D54E99B7E6F97719A47153", "accomodation bett bike sport" },
            };
        }
    }

    public class LtsRidList
    {
        public string rid { get; set; }
    }

    public class LtsRoomGroupList
    {
        public string? classification { get; set; }
        public string? code { get; set; }
        public string? rid { get; set; }
        public string? type { get; set; }
        public int? diningRooms { get; set; }
        public int? livingRooms { get; set; }
        public int? sleepingRooms { get; set; }
        public int? toilets { get; set; }
        public int? roomQuantity { get; set; }
        public int? baths { get; set; }        
        public bool isActive { get; set; }
        public float? minAmountPerPersonPerDay { get; set; }
        public float? minAmountPerUnitPerDay { get; set; }
        public float? squareMeters { get; set; }
    }

    public class LtsAccessibilityAcco
    {
        public IDictionary<string, string>? website { get; set; }

        public IDictionary<string, string>? description { get; set; }

        public IDictionary<string, string>? commitmentToAccessibilityUrl { get; set; }
    }
}
