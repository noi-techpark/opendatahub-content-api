// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public interface ISettings
    {
        string PostgresConnectionString { get; }
        string MongoDBConnectionString { get; }
        XmlConfig XmlConfig { get; }
        JsonConfig JsonConfig { get; }
        S3ImageresizerConfig S3ImageresizerConfig { get; }
        PushServerConfig PushServerConfig { get; }

        //FCMConfig FCMConfig { get; }
        List<Field2HideConfig> Field2HideConfig { get; }
        List<RequestInterceptorConfig> RequestInterceptorConfig { get; }
        List<RateLimitConfig> RateLimitConfig { get; }
        NoRateLimitConfig NoRateLimitConfig { get; }
        List<FCMConfig> FCMConfig { get; }
        List<NotifierConfig> NotifierConfig { get; }

        IDictionary<string, S3Config> S3Config { get; }

        MssConfig MssConfig { get; }
        LcsConfig LcsConfig { get; }
        CDBConfig CDBConfig { get; }
        SiagConfig SiagConfig { get; }
        DSSConfig DSSConfig { get; }
        EBMSConfig EbmsConfig { get; }
        RavenConfig RavenConfig { get; }
        A22Config A22Config { get; }
        FeratelConfig FeratelConfig { get; }
        PanocloudConfig PanocloudConfig { get; }
        PanomaxConfig PanomaxConfig { get; }
        NinjaConfig NinjaConfig { get; }
        MusportConfig MusportConfig { get; }
        SuedtirolWeinConfig SuedtirolWeinConfig { get; }
        LoopTecConfig LoopTecConfig { get; }
        IDictionary<string, DigiWayConfig> DigiWayConfig { get; }
        LTSCredentials LtsCredentials { get; }
        LTSCredentials LtsCredentialsOpen { get; }
    }

    //Classes for Settings shared between Projects

    public class XmlConfig
    {
        public XmlConfig(string xmldir, string xmldirweather)
        {
            this.Xmldir = xmldir;
            this.XmldirWeather = xmldirweather;
        }

        public string Xmldir { get; private set; }
        public string XmldirWeather { get; private set; }
    }

    public class JsonConfig
    {
        public JsonConfig(string jsondir)
        {
            this.Jsondir = jsondir;
        }

        public string Jsondir { get; private set; }
    }

    public class S3ImageresizerConfig
    {
        public S3ImageresizerConfig(
            string url,
            string docurl,
            string bucketaccesspoint,
            string accesskey,
            string secretkey
        )
        {
            this.Url = url;
            this.DocUrl = docurl;
            this.BucketAccessPoint = bucketaccesspoint;
            this.AccessKey = accesskey;
            this.SecretKey = secretkey;
        }

        public string Url { get; private set; }
        public string DocUrl { get; private set; }
        public string BucketAccessPoint { get; private set; }
        public string AccessKey { get; private set; }
        public string SecretKey { get; private set; }
    }

    public class Field2HideConfig
    {
        public Field2HideConfig(string entity, string fields, string validforroles)
        {
            this.Entity = entity;
            this.DisplayOnRoles = validforroles != null ? validforroles.Split(',').ToList() : null;
            this.Fields = fields != null ? fields.Split(',').ToList() : null;
        }

        public string Entity { get; private set; }
        public List<string>? Fields { get; private set; }
        public List<string>? DisplayOnRoles { get; private set; }
    }

    public class RequestInterceptorConfig
    {
        public RequestInterceptorConfig(
            string action,
            string controller,
            string querystrings,
            string redirectaction,
            string redirectcontroller,
            string redirectquerystrings
        )
        {
            this.Action = action;
            this.Controller = controller;
            this.QueryStrings = querystrings != null ? querystrings.Split('&').ToList() : null;
            this.RedirectAction = redirectaction;
            this.RedirectController = redirectcontroller;
            this.RedirectQueryStrings =
                redirectquerystrings != null ? redirectquerystrings.Split('&').ToList() : null;
        }

        public string Action { get; private set; }
        public string Controller { get; private set; }
        public List<string>? QueryStrings { get; private set; }
        public string RedirectAction { get; private set; }
        public string RedirectController { get; private set; }
        public List<string>? RedirectQueryStrings { get; private set; }
    }

    public class PushServerConfig
    {
        public PushServerConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class FCMConfig
    {
        public FCMConfig(
            string identifier,
            string serverkey,
            string senderid,
            string projectname,
            string serviceaccount,
            string serviceaccountjson
        )
        {
            this.Identifier = identifier;
            this.ServerKey = serverkey;
            this.SenderId = senderid;
            this.ProjecTName = projectname;
            this.ServiceAccount = serviceaccount;
            this.ServiceAccountJson = serviceaccountjson;
        }

        public string Identifier { get; private set; }
        public string ServerKey { get; private set; }
        public string SenderId { get; private set; }
        public string ProjecTName { get; private set; }
        public string ServiceAccount { get; private set; }
        public string ServiceAccountJson { get; private set; }
    }

    public class RateLimitConfig
    {
        public RateLimitConfig(string type, int timewindow, int maxrequests)
        {
            this.Type = type;
            this.TimeWindow = timewindow;
            this.MaxRequests = maxrequests;
        }

        public string Type { get; private set; }
        public int TimeWindow { get; private set; }
        public int MaxRequests { get; private set; }
    }

    public class NoRateLimitConfig
    {
        public NoRateLimitConfig(List<string> noratelimitroutes, List<string> noratelimitrefers)
        {
            NoRateLimitRoutes = noratelimitroutes;
            NoRateLimitReferer = noratelimitrefers;
        }

        public List<string> NoRateLimitRoutes { get; private set; }
        public List<string> NoRateLimitReferer { get; private set; }
    }

    public class NotifierConfig
    {
        public NotifierConfig(string servicename, string url, string user, string pswd, string header, string token)
        {
            this.ServiceName = servicename;
            this.Url = url;
            this.User = user;
            this.Password = pswd;
            this.Header = header;
            this.Token = token;
        }

        public string ServiceName { get; private set; }
        public string Url { get; private set; }
        public string User { get; private set; }
        public string Password { get; private set; }
        public string Header { get; private set; }
        public string Token { get; private set; }
    }

    #region Data Importer Config

    public class MssConfig
    {
        public MssConfig(string username, string password, string serviceurl)
        {
            this.Username = username;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class LcsConfig
    {
        public LcsConfig(
            string username,
            string password,
            string messagepassword,
            string serviceurl
        )
        {
            this.Username = username;
            this.Password = password;
            this.MessagePassword = messagepassword;
            this.ServiceUrl = serviceurl;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string MessagePassword { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class CDBConfig
    {
        public CDBConfig(string username, string password, string serviceurl)
        {
            this.Username = username;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }

        public string ServiceUrl { get; private set; }
    }

    public class SiagConfig
    {
        public SiagConfig(string username, string password, string serviceurl)
        {
            this.Username = username;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string Username { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class SuedtirolWeinConfig
    {
        public SuedtirolWeinConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class MusportConfig
    {
        public MusportConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class EBMSConfig
    {
        public EBMSConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class RavenConfig
    {
        public RavenConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class DSSConfig
    {
        public DSSConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class A22Config
    {
        public A22Config(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class FeratelConfig
    {
        public FeratelConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class PanocloudConfig
    {
        public PanocloudConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class PanomaxConfig
    {
        public PanomaxConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class NinjaConfig
    {
        public NinjaConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class LoopTecConfig
    {
        public LoopTecConfig(string user, string password, string serviceurl)
        {
            this.User = user;
            this.Password = password;
            this.ServiceUrl = serviceurl;
        }

        public string User { get; private set; }
        public string Password { get; private set; }
        public string ServiceUrl { get; private set; }
    }

    public class S3Config
    {
        public S3Config(string accesskey, string secretkey, string bucket, string filename)
        {
            this.AccessKey = accesskey;
            this.AccessSecretKey = secretkey;
            this.Bucket = bucket;
            this.Filename = filename;
        }

        public string AccessKey { get; private set; }
        public string AccessSecretKey { get; private set; }
        public string Bucket { get; private set; }
        public string Filename { get; private set; }
    }

    public class LTSCredentials
    {
        public LTSCredentials(
            string serviceurl,
            string username,
            string password,
            string ltsclientid,
            bool opendata
        )
        {
            this.serviceurl = serviceurl;
            this.ltsclientid = ltsclientid;
            this.username = username;
            this.password = password;
            this.opendata = opendata;
        }

        public string serviceurl { get; set; }
        public string ltsclientid { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public bool opendata { get; set; }
    }

    public class DigiWayConfig
    {
        public DigiWayConfig(
            string serviceurl,
            string username,
            string password,
            string identifier            
        )
        {
            this.ServiceUrl = serviceurl;
            this.Identifier = identifier;
            this.Username = username;
            this.Password = password;            
        }

        public string ServiceUrl { get; set; }
        public string Identifier { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }        
    }


    #endregion


    public class NotifyMeta
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string NotifyType { get; set; }
        public string Mode { get; set; }
        public bool HasImagechanged { get; set; }
        public bool? Roomschanged { get; set; }
        public bool IsDelete { get; set; }
        public string Destination { get; set; }
        public string UdateMode { get; set; }
        public string Origin { get; set; }
        public string? Referer { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
        public List<string> ValidTypes { get; set; }
    }
}
