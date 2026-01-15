// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helper;
using Newtonsoft.Json;
using OdhApiCore.Responses;
using SqlKata.Execution;

namespace OdhApiCore.GenericHelpers
{
    public class STARequestHelper
    {
        public static List<string> GetUrlstoCheck(string domain)
        {
            List<string> urltorequest = new List<string>();
            urltorequest.Add(
                domain
                    + "/v1/Poi?language=de&poitype=447&active=true&fields=Id,Detail.de.Title,ContactInfos.de.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Accommodation?language=de&poitype=447&active=true&fields=Id,AccoDetail.de.Name,AccoDetail.de.City&pagesize=10000"
            );
            urltorequest.Add(
                domain
                    + "/v1/ODHActivityPoi?language=de&poitype=447&active=true&fields=Id,Detail.de.Title,ContactInfos.de.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Poi?language=it&poitype=447&active=true&fields=Id,Detail.it.Title,ContactInfos.it.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Accommodation?language=it&poitype=447&active=true&fields=Id,AccoDetail.it.Name,AccoDetail.it.City&pagesize=10000"
            );
            urltorequest.Add(
                domain
                    + "/v1/ODHActivityPoi?language=it&poitype=447&active=true&fields=Id,Detail.it.Title,ContactInfos.it.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Poi?language=en&poitype=447&active=true&fields=Id,Detail.en.Title,ContactInfos.en.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Accommodation?language=en&poitype=447&active=true&fields=Id,AccoDetail.en.Name,AccoDetail.en.City&pagesize=10000"
            );
            urltorequest.Add(
                domain
                    + "/v1/ODHActivityPoi?language=en&poitype=447&active=true&fields=Id,Detail.en.Title,ContactInfos.en.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Poi?language=de&poitype=447&active=true&fields=Id,Detail.de.Title,ContactInfos.de.City&pagesize=20000"
            );
            urltorequest.Add(
                domain
                    + "/v1/Accommodation?language=de&poitype=447&active=true&fields=Id,AccoDetail.de.Name,AccoDetail.de.City&pagesize=10000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/ODHActivityPoi?language=de&poitype=447&active=true&fields=Id,Detail.de.Title,ContactInfos.de.City&pagesize=20000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/Poi?language=it&poitype=447&active=true&fields=Id,Detail.it.Title,ContactInfos.it.City&pagesize=20000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/Accommodation?language=it&poitype=447&active=true&fields=Id,AccoDetail.it.Name,AccoDetail.it.City&pagesize=10000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/ODHActivityPoi?language=it&poitype=447&active=true&fields=Id,Detail.it.Title,ContactInfos.it.City&pagesize=20000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/Poi?language=en&poitype=447&active=true&fields=Id,Detail.en.Title,ContactInfos.en.City&pagesize=20000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/Accommodation?language=en&poitype=447&active=true&fields=Id,AccoDetail.en.Name,AccoDetail.en.City&pagesize=10000&key="
            );
            urltorequest.Add(
                domain
                    + "/v1/ODHActivityPoi?language=en&poitype=447&active=true&fields=Id,Detail.en.Title,ContactInfos.en.City&pagesize=20000&key="
            );

            return urltorequest;
        }

        public static async Task GenerateJSONAccommodationsForSTA(
            QueryFactory queryFactory,
            string jsondir
        )
        {
            List<string> languagelist = new List<string>() { "de", "it", "en" };

            var serializer = new JsonSerializer();
            foreach (var language in languagelist)
            {
                string select =
                    $"data->>'Id' as \"Id\", data->'AccoDetail'->'{language}'->>'Name' AS \"AccoDetail.{language}.Name\", data->'AccoDetail'->'{language}'->>'City' AS \"AccoDetail.{language}.City\"";
                string orderby = "data ->>'Shortname' ASC";
                //List<string> fieldselectorlist = new List<string>() { "Id", "AccoDetail." + language + ".Name", "AccoDetail." + language + ".City" };

                var query = queryFactory
                    .Query()
                    .SelectRaw(select)
                    .From("accommodations")
                    .AccommodationWhereExpression(
                        idlist: new List<string>(),
                        accotypelist: new List<string>(),
                        categorylist: new List<string>(),
                        featurelist: new Dictionary<string, bool>(),
                        featureidlist: new List<string>(),
                        badgelist: new List<string>(),
                        themelist: new Dictionary<string, bool>(),
                        boardlist: new List<string>(),
                        smgtaglist: new List<string>(),
                        tagdict: new Dictionary<string,List<string>>(),
                        districtlist: new List<string>(),
                        municipalitylist: new List<string>(),
                        tourismvereinlist: new List<string>(),
                        regionlist: new List<string>(),
                        apartmentfilter: null,
                        bookable: null,
                        altitude: false,
                        altitudemin: 0,
                        altitudemax: 0,
                        activefilter: true,
                        smgactivefilter: null,
                        publishedonlist: new List<string>(),
                        sourcelist: new List<string>() { "lts" },
                        searchfilter: null,
                        language: language,
                        lastchange: null,
                        languagelist: new List<string>() { language },
                        additionalfilter: null,
                        userroles: new List<string>() { "STA" }
                    )
                    .OrderByRaw(orderby);

                var data = (await query.GetAsync()).ToList();
                var result = ResponseHelpers.GetResult(1, 1, (uint)data.Count, null, data, null);

                //Save json
                string fileName = Path.Combine(jsondir, $"STAAccommodations_{language}.json");
                using (var writer = File.CreateText(fileName))
                {
                    serializer.Serialize(writer, result);
                }
            }
        }

        public static async Task GenerateJSONODHActivityPoiForSTA(
            QueryFactory queryFactory,
            string jsondir,
            string xmlconfig
        )
        {
            List<string> languagelist = new List<string>() { "de", "it", "en" };
            var serializer = new JsonSerializer();

            foreach (var language in languagelist)
            {
                //string select = $"data->>'Id' as \"Id\", data->'Detail'->'{language}'->>'Title' AS \"Detail.{language}.Title\", data->'ContactInfos'->'{language}'->>'City' AS \"ContactInfos.{language}.City\"";
                string select =
                    $"data->>'Id' as \"Id\", data->'Detail'->'{language}'->>'Title' AS \"Detail.{language}.Title\", data->'LocationInfo'->'MunicipalityInfo'->'Name'->>'{language}' AS \"ContactInfos.{language}.City\"";

                string orderby = "data ->>'Shortname' ASC";
                //List<string> fieldselectorlist = new List<string>() { "Id", "Detail." + language + ".Title", "ContactInfos." + language + ".City" };

                var categoriestoretrieve = GetSTACategoriesToFilter(xmlconfig);

                var query = queryFactory
                    .Query()
                    .SelectRaw(select)
                    .From("smgpois")
                    .ODHActivityPoiWhereExpression(
                        idlist: new List<string>(),
                        typelist: new List<string>(),
                        subtypelist: new List<string>(),
                        level3typelist: new List<string>(),
                        smgtaglist: categoriestoretrieve,
                        smgtaglistand: new List<string>(),
                        districtlist: new List<string>(),
                        municipalitylist: new List<string>(),
                        tourismvereinlist: new List<string>(),
                        regionlist: new List<string>(),
                        arealist: new List<string>(),
                        sourcelist: new List<string>(),
                        languagelist: new List<string>() { language },
                        highlight: null,
                        activefilter: true,
                        smgactivefilter: null,
                        categorycodeslist: new List<string>(),
                        dishcodeslist: new List<string>(),
                        ceremonycodeslist: new List<string>(),
                        facilitycodeslist: new List<string>(),
                        activitytypelist: new List<string>(),
                        poitypelist: new List<string>(),
                        difficultylist: new List<string>(),
                        distance: false,
                        distancemin: 0,
                        distancemax: 0,
                        duration: false,
                        durationmin: 0,
                        durationmax: 0,
                        altitude: false,
                        altitudemin: 0,
                        altitudemax: 0,
                        hasimage: null,
                        tagdict: null,
                        publishedonlist: new List<string>(),
                        searchfilter: null,
                        language: language,
                        lastchange: null,
                        additionalfilter: null,
                        userroles: new List<string>() { "STA" }
                    )
                    .OrderByRaw(orderby);

                var data = (await query.GetAsync()).ToList();
                var result = ResponseHelpers.GetResult(1, 1, (uint)data.Count, null, data, null);

                //Save json

                string fileName = Path.Combine(jsondir, $"STAOdhActivitiesPois_{language}.json");

                using (var writer = File.CreateText(fileName))
                {
                    serializer.Serialize(writer, result);
                }

                //// Create a new file
                //using (FileStream fs = System.IO.File.Create(fileName))
                //{
                //    //var sz = JsonConvert.SerializeObject(data);

                //    var sz = JsonConvert.SerializeObject(
                //         ResponseHelpers.GetResult(
                //            1,
                //            2,
                //            (uint)data.Count(),
                //            null,
                //            data,
                //            null));

                //    Byte[] mybyte = new UTF8Encoding(true).GetBytes(sz);
                //    fs.Write(mybyte, 0, mybyte.Length);


                //}
            }
        }

        public static List<string> GetSTACategoriesToFilter(string xmldir)
        {
            List<string> categories = new List<string>();

            var staconfig = XDocument.Load(xmldir + "STACategories.xml");

            categories.AddRange(
                staconfig
                    .Root?.Element("ODHActivityPois")
                    ?.Element("Categories")
                    ?.Elements("Item")
                    ?.Select(x => x.Value)
                    ?.ToList() ?? new List<string>()
            );
            categories.AddRange(
                staconfig
                    .Root?.Element("ODHActivityPois")
                    ?.Element("SubCategories")
                    ?.Elements("Item")
                    ?.Select(x => x.Value)
                    ?.ToList() ?? new List<string>()
            );
            categories.AddRange(
                staconfig
                    .Root?.Element("ODHActivityPois")
                    ?.Element("PoiCategories")
                    ?.Elements("Item")
                    ?.Select(x => x.Value)
                    ?.ToList() ?? new List<string>()
            );

            return categories;
        }
    }
}
