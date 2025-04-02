// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using Helper;
using Microsoft.Extensions.Configuration;

namespace OdhApiImporter
{
    public class Settings : ISettings
    {
        private readonly IConfiguration configuration;
        private readonly Lazy<string> connectionString;
        private readonly Lazy<string> mongoDBConnectionString;
        private readonly MssConfig mssConfig;
        private readonly LcsConfig lcsConfig;
        private readonly CDBConfig cdbConfig;
        private readonly SiagConfig siagConfig;
        private readonly XmlConfig xmlConfig;
        private readonly JsonConfig jsonConfig;
        private readonly S3ImageresizerConfig s3imageresizerConfig;
        private readonly EBMSConfig ebmsConfig;
        private readonly RavenConfig ravenConfig;
        private readonly DSSConfig dssConfig;

        private readonly NinjaConfig ninjaConfig;
        private readonly LoopTecConfig looptecConfig;
        private readonly SuedtirolWeinConfig suedtirolweinConfig;
        private readonly MusportConfig musportConfig;
        private readonly FeratelConfig feratelConfig;
        private readonly PanomaxConfig panomaxConfig;
        private readonly PanocloudConfig panocloudConfig;
        private readonly A22Config a22Config;

        private readonly List<NotifierConfig> notifierConfig;
        private readonly IDictionary<string, S3Config> s3Config;

        private readonly LTSCredentials ltsCredentials;
        private readonly LTSCredentials ltsCredentialsOpen;
        private readonly IDictionary<string, DigiWayConfig> digiwayConfig;

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
            var ebms = this.configuration.GetSection("EBMSConfig");
            this.ebmsConfig = new EBMSConfig(
                ebms.GetValue<string>("Username", ""),
                ebms.GetValue<string>("Password", ""),
                ebms.GetValue<string>("ServiceUrl", "")
            );
            var raven = this.configuration.GetSection("RavenConfig");
            this.ravenConfig = new RavenConfig(
                raven.GetValue<string>("Username", ""),
                raven.GetValue<string>("Password", ""),
                raven.GetValue<string>("ServiceUrl", "")
            );
            var dss = this.configuration.GetSection("DSSConfig");
            this.dssConfig = new DSSConfig(
                dss.GetValue<string>("Username", ""),
                dss.GetValue<string>("Password", ""),
                dss.GetValue<string>("ServiceUrl", "")
            );
            var suedtirolwein = this.configuration.GetSection("SuedtirolWeinConfig");
            this.suedtirolweinConfig = new SuedtirolWeinConfig(
                suedtirolwein.GetValue<string>("Username", ""),
                suedtirolwein.GetValue<string>("Password", ""),
                suedtirolwein.GetValue<string>("ServiceUrl", "")
            );
            var feratel = this.configuration.GetSection("FeratelConfig");
            this.feratelConfig = new FeratelConfig(
                feratel.GetValue<string>("Username", ""),
                feratel.GetValue<string>("Password", ""),
                feratel.GetValue<string>("ServiceUrl", "")
            );
            var panomax = this.configuration.GetSection("PanomaxConfig");
            this.panomaxConfig = new PanomaxConfig(
                panomax.GetValue<string>("Username", ""),
                panomax.GetValue<string>("Password", ""),
                panomax.GetValue<string>("ServiceUrl", "")
            );
            var panocloud = this.configuration.GetSection("PanocloudConfig");
            this.panocloudConfig = new PanocloudConfig(
                panocloud.GetValue<string>("Username", ""),
                panocloud.GetValue<string>("Password", ""),
                panocloud.GetValue<string>("ServiceUrl", "")
            );
            var a22 = this.configuration.GetSection("A22Config");
            this.a22Config = new A22Config(
                a22.GetValue<string>("Username", ""),
                a22.GetValue<string>("Password", ""),
                a22.GetValue<string>("ServiceUrl", "")
            );
            var musport = this.configuration.GetSection("MusportConfig");
            this.musportConfig = new MusportConfig(
                musport.GetValue<string>("Username", ""),
                musport.GetValue<string>("Password", ""),
                musport.GetValue<string>("ServiceUrl", "")
            );
            var ninja = this.configuration.GetSection("NinjaConfig");
            this.ninjaConfig = new NinjaConfig(
                ninja.GetValue<string>("Username", ""),
                ninja.GetValue<string>("Password", ""),
                ninja.GetValue<string>("ServiceUrl", "")
            );
            var looptec = this.configuration.GetSection("LoopTecConfig");
            this.looptecConfig = new LoopTecConfig(
                looptec.GetValue<string>("Username", ""),
                looptec.GetValue<string>("Password", ""),
                looptec.GetValue<string>("ServiceUrl", "")
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
        public EBMSConfig EbmsConfig => this.ebmsConfig;
        public S3ImageresizerConfig S3ImageresizerConfig => this.s3imageresizerConfig;
        public RavenConfig RavenConfig => this.ravenConfig;
        public DSSConfig DSSConfig => this.dssConfig;

        public A22Config A22Config => this.a22Config;
        public FeratelConfig FeratelConfig => this.feratelConfig;
        public PanocloudConfig PanocloudConfig => this.panocloudConfig;
        public PanomaxConfig PanomaxConfig => this.panomaxConfig;
        public SuedtirolWeinConfig SuedtirolWeinConfig => this.suedtirolweinConfig;
        public MusportConfig MusportConfig => this.musportConfig;
        public NinjaConfig NinjaConfig => this.ninjaConfig;
        public LoopTecConfig LoopTecConfig => this.looptecConfig;

        public List<NotifierConfig> NotifierConfig => this.notifierConfig;

        public List<Field2HideConfig> Field2HideConfig => throw new NotImplementedException();
        public List<RequestInterceptorConfig> RequestInterceptorConfig =>
            throw new NotImplementedException();
        public List<RateLimitConfig> RateLimitConfig => throw new NotImplementedException();
        public NoRateLimitConfig NoRateLimitConfig => throw new NotImplementedException();
        public List<FCMConfig> FCMConfig => throw new NotImplementedException();
        public PushServerConfig PushServerConfig => throw new NotImplementedException();
        public IDictionary<string, S3Config> S3Config => this.s3Config;
        public LTSCredentials LtsCredentials => this.ltsCredentials;
        public LTSCredentials LtsCredentialsOpen => this.ltsCredentialsOpen;
        public IDictionary<string, DigiWayConfig> DigiWayConfig => this.digiwayConfig;
    }
}
