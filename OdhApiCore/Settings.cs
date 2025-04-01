// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using Helper;
using Microsoft.Extensions.Configuration;

namespace OdhApiCore
{
    public class Settings : ISettings
    {
        private readonly IConfiguration configuration;
        private readonly Lazy<string> connectionString;
        private readonly Lazy<string> mongoDBConnectionString;
        private readonly XmlConfig xmlConfig;
        private readonly JsonConfig jsonConfig;
        private readonly S3ImageresizerConfig s3imageresizerConfig;
        private readonly RavenConfig ravenConfig;
        private readonly PushServerConfig pushserverConfig;

        //private readonly FCMConfig fcmConfig;
        private readonly List<Field2HideConfig> field2hideConfig;
        private readonly List<RequestInterceptorConfig> requestInterceptorConfig;
        private readonly List<RateLimitConfig> rateLimitConfig;
        private readonly NoRateLimitConfig noRateLimitConfig;
        private readonly List<FCMConfig> fcmConfig;

        private readonly MssConfig mssConfig;
        private readonly LcsConfig lcsConfig;
        private readonly CDBConfig cdbConfig;
        private readonly SiagConfig siagConfig;

        private readonly List<NotifierConfig> notifierConfig;
        private readonly IDictionary<string, S3Config> s3Config;

        private readonly IDictionary<string, DigiWayConfig> digiwayConfig;

        private readonly LTSCredentials ltsCredentials;
        private readonly LTSCredentials ltsCredentialsOpen;

        public Settings(IConfiguration configuration)
        {
            this.configuration = configuration;
            this.connectionString = new Lazy<string>(
                () => this.configuration.GetConnectionString("PGConnection")
            );
            this.mongoDBConnectionString = new Lazy<string>(
                () => this.configuration.GetConnectionString("MongoDBConnection")
            );
            var mss = this.configuration.GetSection("MssConfig");
            this.mssConfig = new MssConfig(
                mss.GetValue<string>("Username", ""),
                mss.GetValue<string>("Password", ""),
                mss.GetValue<string>("ServiceUrl", "")
            );
            var lcs = this.configuration.GetSection("LcsConfig");
            this.lcsConfig = new LcsConfig(
                lcs.GetValue<string>("Username", ""),
                lcs.GetValue<string>("Password", ""),
                lcs.GetValue<string>("MessagePassword", ""),
                lcs.GetValue<string>("ServiceUrl", "")
            );
            var cdb = this.configuration.GetSection("CDBConfig");
            this.cdbConfig = new CDBConfig(
                cdb.GetValue<string>("Username", ""),
                cdb.GetValue<string>("Password", ""),
                cdb.GetValue<string>("ServiceUrl", "")
            );
            var siag = this.configuration.GetSection("SiagConfig");
            this.siagConfig = new SiagConfig(
                siag.GetValue<string>("Username", ""),
                siag.GetValue<string>("Password", ""),
                siag.GetValue<string>("ServiceUrl", "")
            );
            var xml = this.configuration.GetSection("XmlConfig");
            this.xmlConfig = new XmlConfig(
                xml.GetValue<string>("Xmldir", ""),
                xml.GetValue<string>("XmldirWeather", "")
            );
            var json = this.configuration.GetSection("JsonConfig");
            this.jsonConfig = new JsonConfig(json.GetValue<string>("Jsondir", ""));
            var s3img = this.configuration.GetSection("S3ImageresizerConfig");
            this.s3imageresizerConfig = new S3ImageresizerConfig(
                s3img.GetValue<string>("Url", ""),
                s3img.GetValue<string>("DocUrl", ""),
                s3img.GetValue<string>("BucketAccessPoint", ""),
                s3img.GetValue<string>("AccessKey", ""),
                s3img.GetValue<string>("SecretKey", "")
            );
            var raven = this.configuration.GetSection("RavenConfig");
            this.ravenConfig = new RavenConfig(
                raven.GetValue<string>("Username", ""),
                raven.GetValue<string>("Password", ""),
                raven.GetValue<string>("ServiceUrl", "")
            );
            var pushserver = this.configuration.GetSection("PushServerConfig");
            this.pushserverConfig = new PushServerConfig(
                pushserver.GetValue<string>("Username", ""),
                pushserver.GetValue<string>("Password", ""),
                pushserver.GetValue<string>("ServiceUrl", "")
            );
            var field2hidelist = this.configuration.GetSection("Field2HideConfig").GetChildren();
            this.field2hideConfig = new List<Field2HideConfig>();
            foreach (var field2hide in field2hidelist)
            {
                this.field2hideConfig.Add(
                    new Field2HideConfig(
                        field2hide.GetValue<string>("Entity", ""),
                        field2hide.GetValue<string>("Fields", ""),
                        field2hide.GetValue<string>("DisplayOnRoles", "")
                    )
                );
            }

            var requestinterceptorlist = this
                .configuration.GetSection("RequestInterceptorConfig")
                .GetChildren();
            this.requestInterceptorConfig = new List<RequestInterceptorConfig>();

            foreach (var requestinterceptor in requestinterceptorlist)
            {
                this.requestInterceptorConfig.Add(
                    new RequestInterceptorConfig(
                        requestinterceptor.GetValue<string>("Action", ""),
                        requestinterceptor.GetValue<string>("Controller", ""),
                        requestinterceptor.GetValue<string>("QueryStrings", ""),
                        requestinterceptor.GetValue<string>("RedirectAction", ""),
                        requestinterceptor.GetValue<string>("RedirectController", ""),
                        requestinterceptor.GetValue<string>("RedirectQueryStrings", "")
                    )
                );
            }

            var ratelimitlist = this.configuration.GetSection("RateLimitConfig").GetChildren();
            this.rateLimitConfig = new List<RateLimitConfig>();
            foreach (var ratelimitconfig in ratelimitlist)
            {
                this.rateLimitConfig.Add(
                    new RateLimitConfig(
                        ratelimitconfig.GetValue<string>("Type", ""),
                        ratelimitconfig.GetValue<int>("TimeWindow", 0),
                        ratelimitconfig.GetValue<int>("MaxRequests", 0)
                    )
                );
            }

            var noratelimitroutes = this
                .configuration.GetSection("NoRateLimitConfig")
                .GetSection("NoRateLimitRoutesConfig")
                .GetChildren();
            var noratelimitreferers = this
                .configuration.GetSection("NoRateLimitConfig")
                .GetSection("NoRateLimitRefererConfig")
                .GetChildren();
            this.noRateLimitConfig = new NoRateLimitConfig(new List<string>(), new List<string>());
            foreach (var routepath in noratelimitroutes)
            {
                this.noRateLimitConfig.NoRateLimitRoutes.Add(
                    routepath.GetValue<string>("Path", "")
                );
            }
            foreach (var referer in noratelimitreferers)
            {
                this.noRateLimitConfig.NoRateLimitReferer.Add(
                    referer.GetValue<string>("Referer", "")
                );
            }

            this.fcmConfig = new List<FCMConfig>();

            var fcmdict = this.configuration.GetSection("FCMConfig").GetChildren();
            if (fcmdict != null)
            {
                foreach (var fcmkey in fcmdict)
                {
                    var fcmconfigobj = new FCMConfig(
                        fcmkey.Key,
                        fcmkey.GetValue<string>("ServerKey", ""),
                        fcmkey.GetValue<string>("SenderId", ""),
                        fcmkey.GetValue<string>("ProjectName", ""),
                        fcmkey.GetValue<string>("FCMServiceAccount", ""),
                        fcmkey.GetValue<string>("FCMServiceAccountJson", "")
                    );
                    this.fcmConfig.Add(fcmconfigobj);
                }
            }

            this.notifierConfig = new List<NotifierConfig>();

            var notifierconfigdict = this.configuration.GetSection("NotifierConfig").GetChildren();
            if (notifierconfigdict != null)
            {
                foreach (var notifiercfg in notifierconfigdict)
                {
                    this.notifierConfig.Add(
                        new NotifierConfig(
                            notifiercfg.Key,
                            notifiercfg.GetValue<string>("Url", ""),
                            notifiercfg.GetValue<string>("User", ""),
                            notifiercfg.GetValue<string>("Password", ""),
                            notifiercfg.GetValue<string>("Header", ""),
                            notifiercfg.GetValue<string>("Token", "")
                        )
                    );
                }
            }

            this.s3Config = new Dictionary<string, S3Config>();

            var s3configdict = this.configuration.GetSection("S3Config").GetChildren();
            if (s3configdict != null)
            {
                foreach (var s3cfg in s3configdict)
                {
                    this.s3Config.TryAddOrUpdate(
                        s3cfg.Key,
                        new S3Config(
                            s3cfg.GetValue<string>("AccessKey", ""),
                            s3cfg.GetValue<string>("AccessSecretKey", ""),
                            s3cfg.Key,
                            s3cfg.GetValue<string>("Filename", "")
                        )
                    );
                }
            }

            this.digiwayConfig = new Dictionary<string, DigiWayConfig>();

            var digiwayconfigdict = this.configuration.GetSection("DigiWayConfig").GetChildren();
            if (digiwayconfigdict != null)
            {
                foreach (var digiwaycfg in digiwayconfigdict)
                {
                    this.digiwayConfig.TryAddOrUpdate(
                        digiwaycfg.Key,
                        new DigiWayConfig(
                            digiwaycfg.GetValue<string>("ServiceUrl", ""),
                            digiwaycfg.GetValue<string>("Username", ""),
                            digiwaycfg.GetValue<string>("Password", ""),
                            digiwaycfg.Key
                        )
                    );
                }
            }

            var ltsapi = this.configuration.GetSection("LTSApiIDM");
            this.ltsCredentials = new LTSCredentials(
                ltsapi.GetValue<string>("ServiceUrl", ""),
                ltsapi.GetValue<string>("Username", ""),
                ltsapi.GetValue<string>("Password", ""),
                ltsapi.GetValue<string>("XLSClientid", ""),
                ltsapi.GetValue<bool>("Opendata", false)
            );

            var ltsapiopen = this.configuration.GetSection("LTSApiOpen");
            this.ltsCredentialsOpen = new LTSCredentials(
                ltsapiopen.GetValue<string>("ServiceUrl", ""),
                ltsapiopen.GetValue<string>("Username", ""),
                ltsapiopen.GetValue<string>("Password", ""),
                ltsapiopen.GetValue<string>("XLSClientid", ""),
                ltsapiopen.GetValue<bool>("Opendata", true)
            );
        }

        public string PostgresConnectionString => this.connectionString.Value;
        public string MongoDBConnectionString => this.mongoDBConnectionString.Value;
        public MssConfig MssConfig => this.mssConfig;
        public LcsConfig LcsConfig => this.lcsConfig;
        public CDBConfig CDBConfig => this.cdbConfig;
        public SiagConfig SiagConfig => this.siagConfig;
        public XmlConfig XmlConfig => this.xmlConfig;
        public JsonConfig JsonConfig => this.jsonConfig;
        public S3ImageresizerConfig S3ImageresizerConfig => this.s3imageresizerConfig;
        public PushServerConfig PushServerConfig => this.pushserverConfig;
        public List<FCMConfig> FCMConfig => this.fcmConfig;
        public List<Field2HideConfig> Field2HideConfig => this.field2hideConfig;
        public List<RequestInterceptorConfig> RequestInterceptorConfig =>
            this.requestInterceptorConfig;
        public List<RateLimitConfig> RateLimitConfig => this.rateLimitConfig;
        public NoRateLimitConfig NoRateLimitConfig => this.noRateLimitConfig;

        public DSSConfig DSSConfig => throw new NotImplementedException();
        public EBMSConfig EbmsConfig => throw new NotImplementedException();
        public A22Config A22Config => throw new NotImplementedException();
        public FeratelConfig FeratelConfig => throw new NotImplementedException();
        public PanomaxConfig PanomaxConfig => throw new NotImplementedException();
        public PanocloudConfig PanocloudConfig => throw new NotImplementedException();
        public MusportConfig MusportConfig => throw new NotImplementedException();
        public SuedtirolWeinConfig SuedtirolWeinConfig => throw new NotImplementedException();
        public NinjaConfig NinjaConfig => throw new NotImplementedException();
        public LoopTecConfig LoopTecConfig => throw new NotImplementedException();

        public RavenConfig RavenConfig => this.ravenConfig;
        public List<NotifierConfig> NotifierConfig => this.notifierConfig;
        public IDictionary<string, S3Config> S3Config => this.s3Config;
        public IDictionary<string, DigiWayConfig> DigiWayConfig => this.digiwayConfig;

        public LTSCredentials LtsCredentials => this.ltsCredentials;

        public LTSCredentials LtsCredentialsOpen => this.ltsCredentialsOpen;
    }
}
