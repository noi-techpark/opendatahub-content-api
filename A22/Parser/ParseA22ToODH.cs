// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DataModel;
using Helper;

namespace A22
{
    public class ParseA22ToODH
    {
        public static CultureInfo myculture = new CultureInfo("en");

        public static WebcamInfoLinked ParseWebcamToWebcamInfo(
            WebcamInfoLinked? webcam,
            XElement webcamtoparse,
            XElement coordinates,
            string odhid
        )
        {
            var ns = webcamtoparse.GetDefaultNamespace();

            if (webcam == null)
                webcam = new WebcamInfoLinked();

            webcam.Source = "a22";
            webcam.Id = odhid;
            webcam.Active = true;

            //Detail
            Detail detail = new Detail();
            detail.Language = "it";
            detail.Title = webcamtoparse.Element(ns + "Titolo") != null ? webcamtoparse.Element(ns + "Titolo").Value : "";
            detail.BaseText = webcamtoparse.Element(ns + "Descrizione") != null ? webcamtoparse.Element(ns + "Descrizione").Value : "";

            webcam.Detail.TryAddOrUpdate(detail.Language, detail);

            webcam.HasLanguage = new List<string>() { "it" };

            //Imagegallery

            ImageGallery imagegallery = new ImageGallery();
            imagegallery.ImageUrl = webcamtoparse.Element(ns + "Immagine").Value;
            imagegallery.ImageSource = "a22";
            imagegallery.ImageName = webcamtoparse.Element(ns + "Titolo").Value;

            webcam.ImageGallery = new List<ImageGallery>() { imagegallery };

            //ContactInfos

            //Webcamproperties
            WebcamProperties webcamproperties = new WebcamProperties();
            webcamproperties.WebcamUrl = webcamtoparse.Element(ns + "Immagine").Value;

            webcam.WebCamProperties = webcamproperties;

            if (coordinates != null)
            {
                var coordinatesns = coordinates.GetDefaultNamespace();

                webcam.GpsInfo = new List<GpsInfo>();

                //Fill GPSInfo
                GpsInfo gpsinfo = new GpsInfo() { Gpstype = "position" };
                gpsinfo.Latitude = Convert.ToDouble(
                    coordinates.Element(coordinatesns + "Lat").Value,
                    myculture
                );
                gpsinfo.Longitude = Convert.ToDouble(
                    coordinates.Element(coordinatesns + "Lng").Value,
                    myculture
                );

                webcam.GpsInfo.Add(gpsinfo);
            }

            //Mapping
            webcam.Mapping.TryAddOrUpdate(
                "a22",
                new Dictionary<string, string>()
                {
                    { "km", webcamtoparse.Element(ns + "KM").Value },
                }
            );

            //LicenseInfo


            return webcam;
        }

        public static ODHActivityPoiLinked ParseServiceAreaToODHActivityPoi(
            ODHActivityPoiLinked? poi,
            XElement poitoparse,
            XElement coordinates,
            string odhid
        )
        {
            var ns = poitoparse.GetDefaultNamespace();

            if (poi == null)
                poi = new ODHActivityPoiLinked();

            poi.Source = "a22";
            poi.SyncSourceInterface = "servicearea";
            poi.SyncUpdateMode = "full";

            poi.Id = odhid;
            poi.Active = true;

            poi.HasLanguage = new List<string>() { "it" };

            //GPSInfo
            //<Distanza>1.300</Distanza>
            if (coordinates != null)
            {
                var coordinatesns = coordinates.GetDefaultNamespace();

                poi.GpsInfo = new List<GpsInfo>();

                //Fill GPSInfo
                GpsInfo gpsinfo = new GpsInfo() { Gpstype = "position" };
                gpsinfo.Latitude = Convert.ToDouble(
                    coordinates.Element(coordinatesns + "Lat").Value,
                    myculture
                );
                gpsinfo.Longitude = Convert.ToDouble(
                    coordinates.Element(coordinatesns + "Lng").Value,
                    myculture
                );

                poi.GpsInfo.Add(gpsinfo);
            }

            //<ID>34</ID>
            //Mapping
            poi.Mapping.TryAddOrUpdate(
                "a22",
                new Dictionary<string, string>() { { "id", poitoparse.Element(ns + "ID").Value } }
            );

            //Detail
            //<Titolo>Brennero (ex dogana)</Titolo>
            //<Descrizione>Area esterna ad A22. Accessibille da entrame le carreggiate.</Descrizione>

            Detail detailit = new Detail();
            detailit.Language = "it";
            detailit.Title = poitoparse.Element(ns + "Titolo").Value;
            detailit.BaseText = poitoparse.Element(ns + "Descrizione").Value;
            detailit.AdditionalText = poitoparse.Element(ns + "Direzione").Value;

            poi.Detail.TryAddOrUpdate(detailit.Language, detailit);

            //<Direzione>Sud</Direzione>
            //<Gestori/>
            //<Offset>-1</Offset>
            //<MezziPesanti>true</MezziPesanti>
            //<ISAreaDiServizio>false</ISAreaDiServizio>
            //<ColonnineTesla>0</ColonnineTesla>
            //<ChargerMultiStandard>0</ChargerMultiStandard>
            //<ChargerAC>0</ChargerAC>
            //<ChargerUF>0</ChargerUF>

            List<PoiProperty> poipropertylist = new List<PoiProperty>();
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "direction",
                    Value = poitoparse.Element(ns + "Direzione").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "operator",
                    Value = poitoparse.Element(ns + "Gestori").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "offset",
                    Value = poitoparse.Element(ns + "Offset").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "heavyVehicles",
                    Value = poitoparse.Element(ns + "MezziPesanti").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "isServicearea",
                    Value = poitoparse.Element(ns + "ISAreaDiServizio").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "chargerTesla",
                    Value = poitoparse.Element(ns + "ColonnineTesla").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "chargerMultistandard",
                    Value = poitoparse.Element(ns + "ChargerMultiStandard").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "chargerAC",
                    Value = poitoparse.Element(ns + "ChargerAC").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "chargerUF",
                    Value = poitoparse.Element(ns + "ChargerUF").Value,
                }
            );

            poi.PoiProperty.TryAddOrUpdate("de", poipropertylist);
            poi.PoiProperty.TryAddOrUpdate("it", poipropertylist);
            poi.PoiProperty.TryAddOrUpdate("en", poipropertylist);

            //Imagegallery
            //poi.ImageGallery = new List<ImageGallery>();
            poi.PoiServices = new List<string>();

            foreach (
                var service in poitoparse
                    .Elements(ns + "Servizi")
                    .Elements(ns + "WSOpenData_ServizioAreaDiServizio")
            )
            {
                //ImageGallery imageGallery = new ImageGallery();
                //imageGallery.ImageUrl = service.Element(ns + "Immagine").Value;
                //imageGallery.ImageName = service.Element(ns + "Titolo").Value;
                //imageGallery.ImageSource = "a22";
                //imageGallery.ImageTags = new List<string>();
                //imageGallery.ImageTags.Add(service.Element(ns + "Categoria").Value);

                //poi.ImageGallery.Add(imageGallery);

                poi.PoiServices.Add(service.Element(ns + "Titolo").Value);

                //TO CHECK, image is always 0 bytes, category + IDServizio maybe of interest
            }

            //ODHTags
            if (poi.SmgTags == null)
                poi.SmgTags = new List<string>();

            if (!poi.SmgTags.Contains("autobahnraststaette"))
                poi.SmgTags.Add("autobahnraststaette");
            if (!poi.SmgTags.Contains("mobilität"))
                poi.SmgTags.Add("mobilität");

            //Tags
            if (poi.Tags == null)
                poi.Tags = new List<Tags>();

            if (!poi.Tags.Any(x => x.Id == "servicearea"))
                poi.Tags.Add(new Tags() { Id = "servicearea", Source = "a22" });
            if (!poi.Tags.Any(x => x.Id == "mobility"))
                poi.Tags.Add(new Tags() { Id = "mobility", Source = "idm" });

            //ContactInfos

            //Mapping

            //LicenseInfo

            return poi;
        }

        public static ODHActivityPoiLinked ParseTollStationToODHActivityPoi(
            ODHActivityPoiLinked? poi,
            XElement poitoparse,
            XElement coordinates,
            string odhid
        )
        {
            var ns = poitoparse.GetDefaultNamespace();

            if (poi == null)
                poi = new ODHActivityPoiLinked();

            poi.Source = "a22";
            poi.SyncSourceInterface = "tollstation";
            poi.Id = odhid;
            poi.Active = true;
            poi.HasLanguage = new List<string>() { "de", "it", "en" };

            //<IDCasello>3</IDCasello>
            //<IDPedaggio>680</IDPedaggio>
            //Mapping
            poi.Mapping.TryAddOrUpdate(
                "a22",
                new Dictionary<string, string>()
                {
                    { "idcasello", poitoparse.Element(ns + "IDCasello").Value },
                    { "idpedaggio", poitoparse.Element(ns + "IDPedaggio").Value },
                }
            );

            //<Titolo_IT>Vipiteno</Titolo_IT>
            //<Titolo_DE>Sterzing</Titolo_DE>
            //<Titolo_EN>Vipiteno</Titolo_EN>
            //<Descrizione_IT/>
            //<Descrizione_DE/>
            //<Descrizione_EN/>
            //Detail
            Detail detailde = new Detail();
            detailde.Language = "de";
            detailde.Title = poitoparse.Element(ns + "Titolo_DE").Value;
            detailde.BaseText = poitoparse.Element(ns + "Descrizione_DE").Value;

            Detail detailit = new Detail();
            detailit.Language = "it";
            detailit.Title = poitoparse.Element(ns + "Titolo_IT").Value;
            detailit.BaseText = poitoparse.Element(ns + "Descrizione_IT").Value;

            Detail detailen = new Detail();
            detailen.Language = "en";
            detailen.Title = poitoparse.Element(ns + "Titolo_EN").Value;
            detailen.BaseText = poitoparse.Element(ns + "Descrizione_EN").Value;

            poi.Detail.TryAddOrUpdate(detailde.Language, detailde);
            poi.Detail.TryAddOrUpdate(detailit.Language, detailit);
            poi.Detail.TryAddOrUpdate(detailen.Language, detailen);

            //<KM>16</KM>
            //GPSInfo
            if (coordinates != null)
            {
                var coordinatesns = coordinates.GetDefaultNamespace();

                poi.GpsInfo = new List<GpsInfo>();

                //Fill GPSInfo
                GpsInfo gpsinfo = new GpsInfo() { Gpstype = "position" };
                gpsinfo.Latitude = Convert.ToDouble(
                    coordinates.Element(coordinatesns + "Lat").Value,
                    myculture
                );
                gpsinfo.Longitude = Convert.ToDouble(
                    coordinates.Element(coordinatesns + "Lng").Value,
                    myculture
                );

                poi.GpsInfo.Add(gpsinfo);
            }

            //<Entrate_Totali>2</Entrate_Totali>  --> entry_total
            //<Entrate_Telepass>2</Entrate_Telepass> --> entry_telepass
            //<Entrate_Dedicate>0</Entrate_Dedicate> --> entry_dedicated
            //<Entrate_Allargate>1</Entrate_Allargate> --> entry_enlarged
            //<Uscite_Totali>3</Uscite_Totali>
            //<Uscite_Telepass>3</Uscite_Telepass>
            //<Uscite_Dedicate>0</Uscite_Dedicate>
            //<Uscite_Allargate>1</Uscite_Allargate>

            List<PoiProperty> poipropertylist = new List<PoiProperty>();
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "entry_total",
                    Value = poitoparse.Element(ns + "Entrate_Totali").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "entry_telepass",
                    Value = poitoparse.Element(ns + "Entrate_Telepass").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "entry_dedicated",
                    Value = poitoparse.Element(ns + "Entrate_Dedicate").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "entry_enlarged",
                    Value = poitoparse.Element(ns + "Entrate_Allargate").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "exit_total",
                    Value = poitoparse.Element(ns + "Uscite_Totali").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "exit_telepass",
                    Value = poitoparse.Element(ns + "Uscite_Telepass").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "exit_dedicated",
                    Value = poitoparse.Element(ns + "Uscite_Dedicate").Value,
                }
            );
            poipropertylist.Add(
                new PoiProperty()
                {
                    Name = "exit_enlarged",
                    Value = poitoparse.Element(ns + "Uscite_Allargate").Value,
                }
            );

            poi.PoiProperty.TryAddOrUpdate("de", poipropertylist);
            poi.PoiProperty.TryAddOrUpdate("it", poipropertylist);
            poi.PoiProperty.TryAddOrUpdate("en", poipropertylist);

            //<ItinerariSUD_IT>
            //<string>COLLE ISARCO</string>
            //<string>CAMPO TRENS</string>
            //<string>RACINES</string>
            //<string>PASSO GIOVO</string>
            //<string>PASSO PENNES</string>
            //<string>VAL RIDANNA</string>
            //<string>VAL DI VIZZE</string>
            //</ItinerariSUD_IT>
            //<ItinerariSUD_DE>
            //<string>GOSSENSASS</string>
            //<string>FREIENFELD</string>
            //<string>RATSCHINGS</string>
            //<string>JAUFENPASS</string>
            //<string>PENSERJOCH</string>
            //<string>RIDNAUNTAL</string>
            //<string>PFITSCHERTAL</string>
            //</ItinerariSUD_DE>
            //<ItinerariSUD_EN>
            //<string>COLLE ISARCO</string>
            //<string>CAMPO TRENS</string>
            //<string>RACINES</string>
            //<string>PASSO GIOVO</string>
            //<string>PASSO PENNES</string>
            //<string>VAL RIDANNA</string>
            //<string>VAL DI VIZZE</string>
            //</ItinerariSUD_EN>
            //<ItinerariNORD_IT/>
            //<ItinerariNORD_DE/>
            //<ItinerariNORD_EN>
            //<string>-</string>
            //</ItinerariNORD_EN>

            var drivingroutesouth_de = new PoiProperty()
            {
                Name = "drivingroute_south",
                Value =
                    poitoparse.Element(ns + "ItinerariSUD_DE") != null
                        ? string.Join(
                            ",",
                            poitoparse
                                .Element(ns + "ItinerariSUD_DE")
                                .Elements(ns + "string")
                                .Select(x => x.Value)
                                .ToList()
                        )
                        : "",
            };
            var drivingroutenorth_de = new PoiProperty()
            {
                Name = "drivingroute_north",
                Value =
                    poitoparse.Element(ns + "ItinerariNORD_DE") != null
                        ? string.Join(
                            ",",
                            poitoparse
                                .Element(ns + "ItinerariNORD_DE")
                                .Elements(ns + "string")
                                .Select(x => x.Value)
                                .ToList()
                        )
                        : "",
            };

            var drivingroutesouth_it = new PoiProperty()
            {
                Name = "drivingroute_south",
                Value =
                    poitoparse.Element(ns + "ItinerariSUD_IT") != null
                        ? string.Join(
                            ",",
                            poitoparse
                                .Element(ns + "ItinerariSUD_IT")
                                .Elements(ns + "string")
                                .Select(x => x.Value)
                                .ToList()
                        )
                        : "",
            };
            var drivingroutenorth_it = new PoiProperty()
            {
                Name = "drivingroute_north",
                Value =
                    poitoparse.Element(ns + "ItinerariNORD_IT") != null
                        ? string.Join(
                            ",",
                            poitoparse
                                .Element(ns + "ItinerariNORD_IT")
                                .Elements(ns + "string")
                                .Select(x => x.Value)
                                .ToList()
                        )
                        : "",
            };

            var drivingroutesouth_en = new PoiProperty()
            {
                Name = "drivingroute_south",
                Value =
                    poitoparse.Element(poitoparse.GetDefaultNamespace() + "ItinerariSUD_EN") != null
                        ? string.Join(
                            ",",
                            poitoparse
                                .Element(ns + "ItinerariSUD_EN")
                                .Elements(ns + "string")
                                .Select(x => x.Value)
                                .ToList()
                        )
                        : "",
            };
            var drivingroutenorth_en = new PoiProperty()
            {
                Name = "drivingroute_north",
                Value =
                    poitoparse.Element(ns + "ItinerariNORD_EN") != null
                        ? string.Join(
                            ",",
                            poitoparse
                                .Element(ns + "ItinerariNORD_EN")
                                .Elements(ns + "string")
                                .Select(x => x.Value)
                                .ToList()
                        )
                        : "",
            };

            poi.PoiProperty["de"].Add(drivingroutesouth_de);
            poi.PoiProperty["de"].Add(drivingroutesouth_de);

            poi.PoiProperty["it"].Add(drivingroutesouth_de);
            poi.PoiProperty["it"].Add(drivingroutesouth_de);

            poi.PoiProperty["en"].Add(drivingroutesouth_de);
            poi.PoiProperty["en"].Add(drivingroutesouth_de);

            //Imagegallery

            //ContactInfos

            //ODHTAgs
            if (poi.SmgTags == null)
                poi.SmgTags = new List<string>();

            if (!poi.SmgTags.Contains("mautstelle"))
                poi.SmgTags.Add("mautstelle");
            if (!poi.SmgTags.Contains("mobilität"))
                poi.SmgTags.Add("mobilität");

            //Tags
            if (poi.Tags == null)
                poi.Tags = new List<Tags>();

            if (!poi.Tags.Any(x => x.Id == "tollstation"))
                poi.Tags.Add(new Tags() { Id = "tollstation", Source = "a22" });
            if (!poi.Tags.Any(x => x.Id == "mobility"))
                poi.Tags.Add(new Tags() { Id = "mobility", Source = "idm" });

            //LicenseInfo

            //LocationInfo

            return poi;
        }
    }
}
