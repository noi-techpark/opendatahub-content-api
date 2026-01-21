// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel;
using Helper;
using Helper.Generic;
using LTSAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OdhNotifier;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdhApiImporter.Helpers.LTSAPI
{
    public class SiagMuseumTagImportHelper : ImportHelper, IImportHelper
    {
        public SiagMuseumTagImportHelper(
            ISettings settings,
            QueryFactory queryfactory,
            string table,
            string importerURL,
            IOdhPushNotifier odhpushnotifier
        )
            : base(settings, queryfactory, table, importerURL, odhpushnotifier) { }


        private async Task<XDocument> ImportList(CancellationToken cancellationToken)
        {
            var myxml = await SIAG.GetMuseumFromSIAG.GetMuseumList(
                settings.MusportConfig.ServiceUrl
            );

            XDocument mymuseumlist = new XDocument();
            XElement mymuseums = new XElement("Museums");

            XNamespace ns = "http://service.kks.siag";
            XNamespace ax211 = "http://data.service.kks.siag/xsd";

            var mymuseumlist2 =
                myxml.Root?.Element(ns + "return")?.Elements(ax211 + "museums")
                ?? Enumerable.Empty<XElement>();

            foreach (XElement idtoimport in mymuseumlist2)
            {
                XElement mymuseum = new XElement("Museum");
                mymuseum.Add(
                    new XAttribute("ID", idtoimport.Element(ax211 + "museId")?.Value ?? "")
                );
                mymuseum.Add(new XAttribute("PLZ", idtoimport.Element(ax211 + "plz")?.Value ?? ""));

                mymuseums.Add(mymuseum);
            }

            mymuseumlist.Add(mymuseums);

            WriteLog.LogToConsole(
                "",
                "dataimport",
                "list.siagmuseum",
                new ImportLog()
                {
                    sourceid = "",
                    sourceinterface = "siag.museum",
                    success = true,
                    error = "",
                }
            );

            return mymuseumlist;
        }

        public async Task<UpdateDetail> SaveDataToODH(
            DateTime? lastchanged = null,
            List<string>? idlist = null,
            CancellationToken cancellationToken = default
        )
        {
            //Import the actual museums List from SIAG
            var museumslist = await ImportList(cancellationToken);
            //Import Single Data & Deactivate Data
            var result = await SaveSiagTagsToPG(museumslist);

            return result;
        }

        private async Task<UpdateDetail> SaveSiagTagsToPG(XDocument mymuseumlist)
        {
            var newimportcounter = 0;
            var updateimportcounter = 0;
            var errorimportcounter = 0;
            var deleteimportcounter = 0;

            List<string> idlistsiag = new List<string>();

            XNamespace ns = "http://service.kks.siag";

            List<XElement> museumdetaillist = new List<XElement>();

            XElement? mymuseumroot = mymuseumlist.Root;
            foreach (
                XElement mymuseumelement in mymuseumroot?.Elements("Museum")
                    ?? Enumerable.Empty<XElement>()
            )
            {
                string museumid = mymuseumelement.Attribute("ID")?.Value ?? "";

                //Import Museum data from Siag
                var mymuseumdata = await SIAG.GetMuseumFromSIAG.GetMuseumDetail(
                    settings.MusportConfig.ServiceUrl,
                    museumid
                );
                var mymuseumxml = mymuseumdata?.Root?.Element(ns + "return");

                if(mymuseumxml != null)
                    museumdetaillist.Add(mymuseumxml);
            }

            var tagsdata = SIAG.Parser.ParseMuseum.ParseSiagResponseToTags(museumdetaillist);

            if (tagsdata != null && tagsdata.Count() > 0)
            {                
                foreach (var data in tagsdata)
                {
                    string id = data.Id;

                    //See if data exists
                    var query = QueryFactory.Query("tags").Select("data").Where("id", id);
                    var objecttosave = await query.GetObjectSingleAsync<TagLinked>();

                    if (objecttosave == null)
                        objecttosave = new TagLinked();

                    data.FirstImport =
                        objecttosave.FirstImport == null ? DateTime.Now : objecttosave.FirstImport;
                    data.LastChange = DateTime.Now;

                    var result = await InsertDataToDB(data);

                    newimportcounter = newimportcounter + result.created ?? 0;
                    updateimportcounter = updateimportcounter + result.updated ?? 0;
                    errorimportcounter = errorimportcounter + result.error ?? 0;

                    idlistsiag.Add(id);

                    WriteLog.LogToConsole(
                        id,
                        "dataimport",
                        "single.museum.tags",
                        new ImportLog()
                        {
                            sourceid = id,
                            sourceinterface = "siag.museum.tags",
                            success = true,
                            error = "",
                        }
                    );
                }

                if (idlistsiag.Count > 0)
                {
                    //Begin SetDataNotinListToInactive
                    var idlistdb = await GetAllDataBySourceAndType(
                        new List<string>() { "siag" },
                        new List<string>() { "museumcategory", "museumtag", "museumservice" }
                    );

                    var idstodelete = idlistdb.Where(p => !idlistsiag.Any(p2 => p2 == p));

                    foreach (var idtodelete in idstodelete)
                    {
                        var deletedisableresult = await DeleteOrDisableData<TagLinked>(
                            idtodelete,
                            false
                        );

                        if (deletedisableresult.Item1 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.museum.tags.deactivate",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "siag.museum.tags",
                                    success = true,
                                    error = "",
                                }
                            );
                        else if (deletedisableresult.Item2 > 0)
                            WriteLog.LogToConsole(
                                idtodelete,
                                "dataimport",
                                "single.museum.tags.delete",
                                new ImportLog()
                                {
                                    sourceid = idtodelete,
                                    sourceinterface = "siag.museum.tags",
                                    success = true,
                                    error = "",
                                }
                            );

                        deleteimportcounter =
                            deleteimportcounter
                            + deletedisableresult.Item1
                            + deletedisableresult.Item2;
                    }
                }
            }
            else
                errorimportcounter = 1;

            return new UpdateDetail()
            {
                updated = updateimportcounter,
                created = newimportcounter,
                deleted = deleteimportcounter,
                error = errorimportcounter,
            };
        }

        private async Task<PGCRUDResult> InsertDataToDB(
            TagLinked objecttosave
        )
        {
            try
            {
                //Set LicenseInfo
                objecttosave.LicenseInfo = Helper.LicenseHelper.GetLicenseInfoobject(
                    objecttosave,
                    Helper.LicenseHelper.GetLicenseforTag
                );

                //Setting MetaInfo (we need the MetaData Object in the PublishedOnList Creator)
                objecttosave._Meta = MetadataHelper.GetMetadataobject(objecttosave);

                //Set PublishedOn
                objecttosave.CreatePublishedOnList();
                
                return await QueryFactory.UpsertData<TagLinked>(
                    objecttosave,
                    new DataInfo("tags", Helper.Generic.CRUDOperation.CreateAndUpdate),
                    new EditInfo("siag.museum.tags.import", importerURL),
                    new CRUDConstraints(),
                    new CompareConfig(true, false)
                );
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }        
    }
}
