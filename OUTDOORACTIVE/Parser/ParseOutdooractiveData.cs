using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OUTDOORACTIVE.Parser
{
    public class ParseOutdooractiveData
    {
        public static OutdoorActiveActivity ParseOADataDetail(XDocument oadata)
        {
            try
            {
                if (oadata.Root.HasElements)
                {
                    OutdoorActiveActivity parsedactivity = new OutdoorActiveActivity();


                    XNamespace ns = oadata.Root.GetDefaultNamespace();

                    var oaactivity = oadata.Root.Element(ns + "tour");


                    parsedactivity.tour_id = oadata.Root.Element(ns + "tour").Attribute("id") != null ? Convert.ToInt32(oadata.Root.Element(ns + "tour").Attribute("id").Value) : 0;
                    parsedactivity.tour_type = oadata.Root.Element(ns + "tour").Attribute("type") != null ? Convert.ToInt32(oadata.Root.Element(ns + "tour").Attribute("type").Value) : 0;
                    parsedactivity.tour_ranking = oadata.Root.Element(ns + "tour").Attribute("ranking") != null ? Convert.ToDouble(oadata.Root.Element(ns + "tour").Attribute("ranking").Value) : 0;


                    parsedactivity.category_id = oaactivity.Element(ns + "category").Attribute("id") != null ? Convert.ToInt32(oaactivity.Element(ns + "category").Attribute("id").Value) : 0;
                    parsedactivity.category_name = oaactivity.Element(ns + "category").Attribute("name") != null ? oaactivity.Element(ns + "category").Attribute("name").Value : "";

                    parsedactivity.title = oaactivity.Element(ns + "title") != null ? oaactivity.Element(ns + "title").Value : "";
                    parsedactivity.shorttext = oaactivity.Element(ns + "shortText") != null ? oaactivity.Element(ns + "shortText").Value : "";
                    parsedactivity.longtext = oaactivity.Element(ns + "longText") != null ? oaactivity.Element(ns + "longText").Value : "";

                    if (oaactivity.Element(ns + "primaryImage") != null)
                    {
                        parsedactivity.primaryImage_id = oaactivity.Element(ns + "primaryImage").Attribute("id") != null ? oaactivity.Element(ns + "primaryImage").Attribute("id").Value : "";
                        parsedactivity.primaryImage_title = oaactivity.Element(ns + "primaryImage").Element("title") != null ? oaactivity.Element(ns + "primaryImage").Element("title").Value : "";
                        parsedactivity.primaryImage_author = oaactivity.Element(ns + "primaryImage").Element("author") != null ? oaactivity.Element(ns + "primaryImage").Element("author").Value : "";
                    }

                    parsedactivity.geometry = oaactivity.Element(ns + "geometry") != null ? oaactivity.Element(ns + "geometry").Value : "";

                    List<OutdoorActiveImage> oaimages = new List<OutdoorActiveImage>();

                    if (oaactivity.Element(ns + "images") != null)
                    {
                        foreach (var myimage in oaactivity.Element(ns + "images").Elements(ns + "image"))
                        {
                            OutdoorActiveImage oaimage = new OutdoorActiveImage();
                            oaimage.primary = myimage.Attribute("primary") != null ? Convert.ToBoolean(myimage.Attribute("primary").Value) : false;
                            oaimage.gallery = myimage.Attribute("gallery") != null ? Convert.ToBoolean(myimage.Attribute("gallery").Value) : false;
                            oaimage.id = myimage.Attribute("id") != null ? Convert.ToInt32(myimage.Attribute("id").Value) : 0;
                            oaimage.title = myimage.Element(ns + "title") != null ? myimage.Element(ns + "title").Value : "";
                            oaimage.author = myimage.Element(ns + "author") != null ? myimage.Element(ns + "author").Value : "";
                            oaimage.geometry = myimage.Element(ns + "geometry") != null ? myimage.Element(ns + "geometry").Value : "";
                            oaimage.created = myimage.Element(ns + "meta").Element(ns + "date").Attribute("created") != null ? Convert.ToDateTime(myimage.Element(ns + "meta").Element(ns + "date").Attribute("created").Value) : DateTime.MinValue;
                            oaimage.modified = myimage.Element(ns + "meta").Element(ns + "date").Attribute("lastModified") != null ? Convert.ToDateTime(myimage.Element(ns + "meta").Element(ns + "date").Attribute("lastModified").Value) : DateTime.MinValue;

                            oaimages.Add(oaimage);
                        }
                    }

                    parsedactivity.images = oaimages.ToList();

                    List<OutdoorActiveRegion> oaregions = new List<OutdoorActiveRegion>();

                    if (oaactivity.Element(ns + "regions") != null)
                    {
                        foreach (var myregion in oaactivity.Element(ns + "regions").Elements(ns + "region"))
                        {
                            OutdoorActiveRegion oaregion = new OutdoorActiveRegion();

                            oaregion.id = myregion.Attribute("id") != null ? Convert.ToInt32(myregion.Attribute("id").Value) : 0;
                            oaregion.type = myregion.Attribute("type") != null ? myregion.Attribute("type").Value : "";
                            oaregion.isStartRegion = myregion.Attribute("isStartRegion") != null ? Convert.ToBoolean(myregion.Attribute("isStartRegion").Value) : false;
                            oaregion.name = myregion.Attribute("name") != null ? myregion.Attribute("name").Value : "";

                            oaregions.Add(oaregion);
                        }
                    }

                    parsedactivity.regions = oaregions.ToList();

                    parsedactivity.winterActivity = oaactivity.Element(ns + "winterActivity") != null ? Convert.ToBoolean(oaactivity.Element(ns + "winterActivity").Value) : false;

                    parsedactivity.time = oaactivity.Element(ns + "time").Attribute("min") != null ? Convert.ToInt32(oaactivity.Element(ns + "time").Attribute("min").Value) : 0;
                    parsedactivity.length = oaactivity.Element(ns + "length") != null ? Convert.ToDouble(oaactivity.Element(ns + "length").Value) : 0;


                    parsedactivity.elevation_ascent = oaactivity.Element(ns + "elevation").Attribute("ascent") != null ? Convert.ToInt32(oaactivity.Element(ns + "elevation").Attribute("ascent").Value) : 0;
                    parsedactivity.elevation_descent = oaactivity.Element(ns + "elevation").Attribute("descent") != null ? Convert.ToInt32(oaactivity.Element(ns + "elevation").Attribute("descent").Value) : 0;
                    parsedactivity.elevation_maxAltitude = oaactivity.Element(ns + "elevation").Attribute("minAltitude") != null ? Convert.ToInt32(oaactivity.Element(ns + "elevation").Attribute("minAltitude").Value) : 0;
                    parsedactivity.elevation_minAltitude = oaactivity.Element(ns + "elevation").Attribute("maxAltitude") != null ? Convert.ToInt32(oaactivity.Element(ns + "elevation").Attribute("maxAltitude").Value) : 0;


                    parsedactivity.rating_condition = oaactivity.Element(ns + "rating").Attribute("condition") != null ? Convert.ToInt32(oaactivity.Element(ns + "rating").Attribute("condition").Value) : 0;
                    parsedactivity.rating_difficulty = oaactivity.Element(ns + "rating").Attribute("difficulty") != null ? Convert.ToInt32(oaactivity.Element(ns + "rating").Attribute("difficulty").Value) : 0;
                    parsedactivity.rating_landscape = oaactivity.Element(ns + "rating").Attribute("landscape") != null ? Convert.ToInt32(oaactivity.Element(ns + "rating").Attribute("landscape").Value) : 0;
                    parsedactivity.rating_qualityOfExperience = oaactivity.Element(ns + "rating").Attribute("qualityOfExperience") != null ? Convert.ToInt32(oaactivity.Element(ns + "rating").Attribute("qualityOfExperience").Value) : 0;

                    parsedactivity.gpsstart_latitude = oaactivity.Element(ns + "startingPoint").Attribute("lat") != null ? Convert.ToDouble(oaactivity.Element(ns + "startingPoint").Attribute("lat").Value) : 0;
                    parsedactivity.gpsstart_longitude = oaactivity.Element(ns + "startingPoint").Attribute("lon") != null ? Convert.ToDouble(oaactivity.Element(ns + "startingPoint").Attribute("lon").Value) : 0;

                    parsedactivity.season_jan = oaactivity.Element(ns + "season").Attribute("jan") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("jan").Value) : false;
                    parsedactivity.season_feb = oaactivity.Element(ns + "season").Attribute("feb") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("feb").Value) : false;
                    parsedactivity.season_mar = oaactivity.Element(ns + "season").Attribute("mar") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("mar").Value) : false;
                    parsedactivity.season_apr = oaactivity.Element(ns + "season").Attribute("apr") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("apr").Value) : false;
                    parsedactivity.season_may = oaactivity.Element(ns + "season").Attribute("may") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("may").Value) : false;
                    parsedactivity.season_jun = oaactivity.Element(ns + "season").Attribute("jun") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("jun").Value) : false;
                    parsedactivity.season_jul = oaactivity.Element(ns + "season").Attribute("jul") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("jul").Value) : false;
                    parsedactivity.season_aug = oaactivity.Element(ns + "season").Attribute("aug") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("aug").Value) : false;
                    parsedactivity.season_sep = oaactivity.Element(ns + "season").Attribute("sep") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("sep").Value) : false;
                    parsedactivity.season_oct = oaactivity.Element(ns + "season").Attribute("oct") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("oct").Value) : false;
                    parsedactivity.season_nov = oaactivity.Element(ns + "season").Attribute("nov") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("nov").Value) : false;
                    parsedactivity.season_dec = oaactivity.Element(ns + "season").Attribute("dec") != null ? Convert.ToBoolean(oaactivity.Element(ns + "season").Attribute("dec").Value) : false;

                    parsedactivity.startingpointdesc = oaactivity.Element(ns + "startingPointDescr") != null ? oaactivity.Element(ns + "startingPointDescr").Value : "";

                    parsedactivity.gettingThere = oaactivity.Element(ns + "gettingThere") != null ? oaactivity.Element(ns + "gettingThere").Value : "";
                    parsedactivity.directions = oaactivity.Element(ns + "directions") != null ? oaactivity.Element(ns + "directions").Value : "";
                    parsedactivity.tip = oaactivity.Element(ns + "tip") != null ? oaactivity.Element(ns + "tip").Value : "";


                    List<OutdoorActiveProperty> oaproperties = new List<OutdoorActiveProperty>();

                    if (oaactivity.Element(ns + "properties") != null)
                    {
                        foreach (var mypropertiy in oaactivity.Element(ns + "properties").Elements(ns + "property"))
                        {
                            OutdoorActiveProperty oaproperty = new OutdoorActiveProperty();

                            oaproperty.hasicon = mypropertiy.Attribute("hasIcon") != null ? Convert.ToBoolean(mypropertiy.Attribute("hasIcon").Value) : false;
                            oaproperty.text = mypropertiy.Attribute("text") != null ? mypropertiy.Attribute("text").Value : "";
                            oaproperty.tag = mypropertiy.Attribute("tag") != null ? mypropertiy.Attribute("tag").Value : "";
                            oaproperty.iconurl = mypropertiy.Attribute("iconURL") != null ? mypropertiy.Attribute("iconURL").Value : "";

                            oaproperties.Add(oaproperty);
                        }
                    }

                    parsedactivity.properties = oaproperties.ToList();


                    List<string> oapois = new List<string>();

                    if (oaactivity.Element(ns + "pois") != null)
                    {
                        foreach (var mypropertiy in oaactivity.Element(ns + "pois").Elements(ns + "poi"))
                        {

                            oapois.Add(mypropertiy.Attribute("id") != null ? mypropertiy.Attribute("id").Value : "");
                        }
                    }

                    parsedactivity.pois = oapois.ToList();


                    parsedactivity.elevationprofile_id = oaactivity.Element(ns + "elevationProfile").Attribute("id") != null ? Convert.ToInt32(oaactivity.Element(ns + "elevationProfile").Attribute("id").Value) : 0;

                    List<OutdoorActiveWayType> oawaytypes = new List<OutdoorActiveWayType>();

                    if (oaactivity.Element(ns + "wayType") != null)
                    {
                        foreach (var mywaytype in oaactivity.Element(ns + "wayType").Elements(ns + "legend"))
                        {
                            OutdoorActiveWayType oawaytype = new OutdoorActiveWayType();

                            oawaytype.length = mywaytype.Attribute("length") != null ? Convert.ToDouble(mywaytype.Attribute("length").Value) : 0;
                            oawaytype.title = mywaytype.Attribute("title") != null ? mywaytype.Attribute("title").Value : "";
                            oawaytype.type = mywaytype.Attribute("type") != null ? Convert.ToInt32(mywaytype.Attribute("type").Value) : 0;

                            oawaytypes.Add(oawaytype);
                        }
                    }

                    parsedactivity.wayType = oawaytypes.ToList();

                    List<string> bookworks = new List<string>();

                    foreach (var mybookwork in oaactivity.Elements(ns + "bookWorks"))
                    {
                        bookworks.Add(mybookwork.Attribute("id") != null ? mybookwork.Attribute("id").Value : "");
                    }

                    parsedactivity.bookWorks = bookworks.ToList();


                    return parsedactivity;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {

                Console.WriteLine("ERROR: " + ex.Message);
                return null;
            }
        }

        public static OutdoorActiveSyncList ParseOADataSyncList(XDocument oadata)
        {
            try
            {
                OutdoorActiveSyncList parsedsynclist = new OutdoorActiveSyncList();

                var oacontents = oadata.Root.Element("contents");
                parsedsynclist.count = Convert.ToInt32(oacontents.Attribute("count").Value);

                List<OutdoorActiveSyncListElements> parsedsynclistcontents = new List<OutdoorActiveSyncListElements>();

                var oacontentlist = oacontents.Elements("content");

                foreach (var oacontent in oacontentlist)
                {
                    var parsedsynclistcontent = new OutdoorActiveSyncListElements();

                    parsedsynclistcontent.id = oacontent.Attribute("id").Value;
                    parsedsynclistcontent.foreignKey = oacontent.Attribute("foreignKey").Value;
                    parsedsynclistcontent.state = oacontent.Attribute("state").Value;
                    parsedsynclistcontent.lastModifiedAt = DateTime.Parse(oacontent.Attribute("lastModifiedAt").Value);

                    parsedsynclistcontents.Add(parsedsynclistcontent);
                }

                parsedsynclist.content = parsedsynclistcontents.ToList();

                return parsedsynclist;
            }
            catch (Exception ex)
            {

                Console.WriteLine("ERROR: " + ex.Message);
                return null;
            }
        }
    }

    public class OutdoorActiveActivity
    {
        public OutdoorActiveActivity()
        {

        }

        public int tour_id { get; set; }
        public int tour_type { get; set; }
        public double tour_ranking { get; set; }

        public int category_id { get; set; }
        public string category_name { get; set; }

        public string title { get; set; }
        public string shorttext { get; set; }
        public string longtext { get; set; }

        public string primaryImage_id { get; set; }
        public string primaryImage_title { get; set; }
        public string primaryImage_author { get; set; }

        public string geometry { get; set; }

        public List<OutdoorActiveImage> images { get; set; }
        public List<OutdoorActiveRegion> regions { get; set; }

        public bool winterActivity { get; set; }

        public int time { get; set; }
        public double length { get; set; }

        public int elevation_ascent { get; set; }
        public int elevation_descent { get; set; }
        public int elevation_minAltitude { get; set; }
        public int elevation_maxAltitude { get; set; }

        public int rating_condition { get; set; }
        public int rating_difficulty { get; set; }
        public int rating_qualityOfExperience { get; set; }
        public int rating_landscape { get; set; }

        public double gpsstart_longitude { get; set; }
        public double gpsstart_latitude { get; set; }

        public bool season_jan { get; set; }
        public bool season_feb { get; set; }
        public bool season_mar { get; set; }
        public bool season_apr { get; set; }
        public bool season_may { get; set; }
        public bool season_jun { get; set; }
        public bool season_jul { get; set; }
        public bool season_aug { get; set; }
        public bool season_sep { get; set; }
        public bool season_oct { get; set; }
        public bool season_nov { get; set; }
        public bool season_dec { get; set; }

        public string startingpointdesc { get; set; }
        public string directions { get; set; }
        public string gettingThere { get; set; }

        public string tip { get; set; }
        public string destination { get; set; }

        public List<OutdoorActiveProperty> properties { get; set; }

        public List<string> pois { get; set; }

        public int elevationprofile_id { get; set; }

        public List<string> bookWorks { get; set; }

        public List<OutdoorActiveWayType> wayType { get; set; }

    }

    public class OutdoorActiveImage
    {
        public bool primary { get; set; }
        public bool gallery { get; set; }
        public string title { get; set; }
        public int id { get; set; }
        public string author { get; set; }
        public string geometry { get; set; }
        public DateTime created { get; set; }
        public DateTime modified { get; set; }
    }

    public class OutdoorActiveRegion
    {
        public bool isStartRegion { get; set; }
        public string type { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }

    public class OutdoorActiveProperty
    {
        public bool hasicon { get; set; }
        public string tag { get; set; }
        public string text { get; set; }
        public string iconurl { get; set; }
    }

    public class OutdoorActiveWayType
    {
        public double length { get; set; }
        public string title { get; set; }
        public int type { get; set; }
    }

    public class OutdoorActiveSyncList
    {
        public int count { get; set; }

        public List<OutdoorActiveSyncListElements> content { get; set; }
    }

    public class OutdoorActiveSyncListElements
    {
        public string foreignKey { get; set; }
        public string id { get; set; }
        public string state { get; set; }
        public DateTime lastModifiedAt { get; set; }
    }
}
