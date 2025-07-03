// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using AspNetCore.CacheOutput.InMemory.Extensions;
using Helper;
using Helper.Factories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OdhApiCore.Controllers;
using OdhApiCore.Swagger;
using OdhNotifier;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SqlKata.Execution;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Swagger;

namespace OdhApiCore
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment CurrentEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => true;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});

            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(
            //        Configuration.GetConnectionString("DefaultConnection")));
            //services.AddDefaultIdentity<IdentityUser>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>();

            services
                .AddHttpClient(
                    "mss",
                    client =>
                    {
                        //client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
                    }
                )
                .ConfigureHttpMessageHandlerBuilder(config => new HttpClientHandler()
                {
                    AutomaticDecompression =
                        DecompressionMethods.GZip | DecompressionMethods.Deflate,
                });
            services.AddHttpClient("lcs"); // TODO: put LCS config here

            //Adding Cache Service in Memory
            services.AddInMemoryCacheOutput();
            services.AddSingleton<CustomCacheKeyGenerator>();

            services.AddDistributedMemoryCache();

            services
                .AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddNpgSql(
                    Configuration.GetConnectionString("PgConnection"),
                    tags: new[] { "services" }
                );

            //TO Remove old Quota Config

            //Adding Quota Service in Memory https://github.com/stefanprodan/AspNetCoreRateLimit
            //services.AddMemoryCache();

            ////ClientRateLimit
            //services.Configure<ClientRateLimitOptions>(options =>
            //{
            //    // Enable "global" limit, not per endpoint
            //    options.EnableEndpointRateLimiting = true;
            //    options.StackBlockedRequests = false;
            //    options.HttpStatusCode = 429;
            //    options.ClientIdHeader = "Referer";
            //    //NOT Overwritten by ClientRateLimitPolicies
            //    //options.GeneralRules = new List<RateLimitRule>
            //    //{
            //    //    new RateLimitRule()
            //    //    {
            //    //        Endpoint = "get:/v1",
            //    //                Period = "1m",
            //    //                Limit = 10
            //    //    }
            //    //};
            //});
            ////ClientRateLimitPolicies
            //services.Configure<ClientRateLimitPolicies>(options =>
            //{
            //    options.ClientRules = new List<ClientRateLimitPolicy>
            //    {
            //        new ClientRateLimitPolicy()
            //        {
            //            ClientId = "Anonymous",
            //            Rules = new List<RateLimitRule>()
            //            {
            //                new RateLimitRule()
            //                {
            //                    Endpoint = "get:/v1",
            //                    Period = "1m",
            //                    Limit = 30
            //                }
            //            }
            //        },
            //        new ClientRateLimitPolicy()
            //        {
            //            ClientId = "Authenticated",
            //            Rules = {
            //                new RateLimitRule()
            //                {
            //                    Endpoint = "get:/v1",
            //                    Period = "1m",
            //                    Limit = 60
            //                }
            //            }
            //        }
            //    };
            //});


            //services.Configure<IpRateLimitOptions>(options =>
            //{
            //    // Enable "global" limit, not per endpoint
            //    options.EnableEndpointRateLimiting = true;
            //    options.StackBlockedRequests = false;
            //    options.HttpStatusCode = 429;
            //    options.ClientIdHeader = "Referer";
            //    options.ClientWhitelist = new List<string> { "Anonymous", "Authenticated" };
            //    //General Rule from
            //    options.GeneralRules = new List<RateLimitRule>
            //    {
            //        new RateLimitRule()
            //        {
            //            Endpoint = "get:/v1",
            //                    Period = "1m",
            //                    Limit = 10
            //        }
            //    };
            //});

            ////IpRateLimitPolicies
            //services.Configure<IpRateLimitPolicies>(options =>
            //{
            //    options.IpRules = new List<IpRateLimitPolicy>
            //    {
            //        new IpRateLimitPolicy()
            //        {
            //            Ip = "127.0.0.1",
            //            Rules = new List<RateLimitRule>()
            //            {
            //                new RateLimitRule()
            //                {
            //                    Endpoint = "get:/v1",
            //                    Period = "5m",
            //                    Limit = 10
            //                }
            //            }
            //        }
            //        //new IpRateLimitPolicy()
            //        //{
            //        //    Ip = "Authenticated",
            //        //    Rules = {
            //        //        new RateLimitRule()
            //        //        {
            //        //            Endpoint = "get:/v1",
            //        //            Period = "10m",
            //        //            Limit = 10
            //        //        }
            //        //    }
            //        //}
            //    };
            //});


            //services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            //services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();


            //services.AddInMemoryRateLimiting();

            //services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            //services.AddSingleton<IRateLimitConfiguration, Middleware.OdhRateLimitConfiguration>();

            services.AddLogging(options =>
            {
                options.ClearProviders();

                var levelSwitch = new LoggingLevelSwitch
                {
                    MinimumLevel = CurrentEnvironment.IsDevelopment()
                        ? LogEventLevel.Debug
                        : LogEventLevel.Warning,
                };
                var loggerConfiguration = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "{Message}{NewLine}")
                    .WriteTo.Debug()
                    //.WriteTo.Elasticsearch(
                    //    new ElasticsearchSinkOptions() {
                    //        AutoRegisterTemplate = true,
                    //        IndexFormat = "odh-tourism-{0:yyyy.MM}",
                    //        //ModifyConnectionSettings = (c) => c.GlobalHeaders(new NameValueCollection { { "Authorization", "Basic " + loggerconfig.elkbasicauthtoken } }),
                    //        FailureCallback = e => System.Console.Error.WriteLine("Unable to submit event " + e.MessageTemplate),
                    //        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                    //                           EmitEventFailureHandling.WriteToFailureSink |
                    //                           EmitEventFailureHandling.RaiseCallback,
                    //        //FailureSink = new FileSink(loggerconfig.filepathfailures, new JsonFormatter(), null),
                    //        MinimumLogEventLevel = LogEventLevel.Information
                    //    }
                    //)
                    .CreateLogger();
                options.AddSerilog(loggerConfiguration, dispose: true);

                // Configure Serilogs own configuration to use
                // the configured logger configuration.
                // This allows to Log via Serilog's Log and ILogger.
                Log.Logger = loggerConfiguration;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
                options.Level = System.IO.Compression.CompressionLevel.Optimal
            );
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.AddCors(o =>
            {
                o.AddPolicy(
                    "CorsPolicy",
                    builder =>
                    {
                        builder
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .SetIsOriginAllowed(hostName => true);
                    }
                );
            });

            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    //{
                    //    CamelCaseText = true
                    //});
                });

            services.AddRazorPages();

            services.AddSingleton<ISettings, Settings>();
            services.AddScoped<QueryFactory, PostgresQueryFactory>();
            services.AddScoped<IOdhPushNotifier, OdhPushNotifier>();

            //Initialize JWT Authentication
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(jwtBearerOptions =>
                {
                    jwtBearerOptions.Authority = Configuration
                        .GetSection("OauthServerConfig")
                        .GetValue<string>("Authority");
                    //jwtBearerOptions.Audience = "account";
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "preferred_username",
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidIssuer = Configuration
                            .GetSection("OauthServerConfig")
                            .GetValue<string>("Authority"),
                        ValidateIssuer = true,
                        //RoleClaimTypeRetriever
                        //RoleClaimType = "resource_access.odh-api-core.roles"  //TO TEST
                    };
                    jwtBearerOptions.Events = new JwtBearerEvents()
                    {
                        //When switching to Authorization Services this is not needed anymore
                        //OnTokenValidated = context =>
                        //{
                        //    RoleHelper.AddRoleClaims(context.Principal, "odh-api-core");
                        //    return Task.CompletedTask;
                        //},
                        OnAuthenticationFailed = c =>
                        {
                            c.NoResult();
                            c.Response.StatusCode = 401;
                            c.Response.ContentType = "text/plain";

                            //Generate Log
                            HttpRequestExtensions.GenerateLogResponse(c.HttpContext);

                            return c.Response.WriteAsync("");
                            //return Task.CompletedTask;
                        },
                        //OnChallenge = c =>
                        //{
                        //    c.HandleResponse();
                        //    return Task.CompletedTask;
                        //}
                    };
                });

            services.AddMvc(options =>
            {
                options.OutputFormatters.Add(new Formatters.CsvOutputFormatter());
                options.FormatterMappings.SetMediaTypeMappingForFormat("csv", "text/csv");

                options.OutputFormatters.Add(new Formatters.JsonLdOutputFormatter());
                //Hack only ldjson accepted
                options.FormatterMappings.SetMediaTypeMappingForFormat(
                    "json-ld",
                    "application/ldjson"
                );

                options.OutputFormatters.Add(new Formatters.RawdataOutputFormatter());
                options.FormatterMappings.SetMediaTypeMappingForFormat(
                    "rawdata",
                    "application/rawdata"
                );
            });
            //.AddJsonOptions(options =>
            //{
            //    options.JsonSerializerOptions.PropertyNameCaseInsensitive = new DefaultContractResolver();
            //});

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "Open Data Hub Tourism Api",
                        Version = "v1",
                        Description =
                            "Open Data Hub Tourism Api based on .Net Core with PostgreSQL",
                        TermsOfService = new System.Uri(
                            "https://opendatahub.readthedocs.io/en/latest/"
                        ),
                        Contact = new OpenApiContact
                        {
                            Name = "Open Data Hub Team",
                            Email = "help@opendatahub.com",
                            Url = new System.Uri("https://opendatahub.com/"),
                        },
                    }
                );
                c.MapType<LegacyBool>(() => new OpenApiSchema { Type = "boolean" });
                c.MapType<PageSize>(() => new OpenApiSchema { Type = "integer" });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //var xmlPathdatamodel = Path.Combine(AppContext.BaseDirectory, $"DataModel.xml");
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                //c.IncludeXmlComments(xmlPathdatamodel, includeControllerXmlComments: true);
                c.AddSecurityDefinition(
                    "oauth2",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.OAuth2,
                        Flows = new OpenApiOAuthFlows
                        {
                            Password = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(
                                    Configuration
                                        .GetSection("OauthServerConfig")
                                        .GetValue<string>("Authority")
                                        + "protocol/openid-connect/token"
                                ),
                            },
                            ClientCredentials = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(
                                    Configuration
                                        .GetSection("OauthServerConfig")
                                        .GetValue<string>("Authority")
                                        + "protocol/openid-connect/token"
                                ),
                            },
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                TokenUrl = new Uri(
                                    Configuration
                                        .GetSection("OauthServerConfig")
                                        .GetValue<string>("Authority")
                                        + "protocol/openid-connect/token"
                                ),
                                AuthorizationUrl = new Uri(
                                    Configuration
                                        .GetSection("OauthServerConfig")
                                        .GetValue<string>("Authority")
                                        + "protocol/openid-connect/auth"
                                ),
                                //Scopes = new Dictionary<string, string>() { { "odh-api-core-scope", "Test Scope" } }
                            }
                        },
                        BearerFormat = "JWT",
                        Scheme = "Bearer",
                    }
                );
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
                c.OperationFilter<AuthenticationRequirementsOperationFilter>();
                c.SchemaFilter<DeprecatedAttributeSchemaFilter>();
                c.SchemaFilter<EnumAttributeSchemaFilter>();
                c.SchemaFilter<ReferencedAttributeSchemaFilter>();
                c.EnableAnnotations();
                //c.AddSecurityRequirement(new OpenApiSecurityRequirement
                //{
                //    {
                //        new OpenApiSecurityScheme
                //        {
                //            Reference = new OpenApiReference
                //            {
                //                Type = ReferenceType.SecurityScheme,
                //                Id = "bearer"
                //            }
                //        }, new List<string>()
                //    }
                //});
            });
            services.AddSwaggerGenNewtonsoftSupport();

            //Use server side caching of the swagger doc
            services.Replace(
                ServiceDescriptor.Transient<ISwaggerProvider, CachingSwaggerProvider>()
            );

            //services.Configure<ForwardedHeadersOptions>(options =>
            //{
            //    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
            //});

            //services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //app.UseForwardedHeaders();

            app.UseForwardedHeaders(new ForwardedHeadersOptions()
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedProto,
                KnownNetworks = { },
                KnownProxies = { },
                //KnownProxies =
                //{
                //    // Caddy Prod
                //    IPAddress.Parse("172.31.1.28"),
                //    // Caddy Test
                //    IPAddress.Parse("172.31.23.94"),
                //}
                //KnownNetworks =
                //{
                //    // IP v4 network
                //    //new IPNetwork(IPAddress.Parse("10.244.0.0"), 16),                    
                //}
            });


            //// TODO: Move to Production
            //app.UseClientRateLimiting();
            //app.UseIpRateLimiting();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseResponseCompression();
            //app.UseHttpsRedirection();

            app.UseStaticFiles(
                new StaticFileOptions()
                {
                    OnPrepareResponse = ctx =>
                    {
                        //Activate Browser Cache of the swagger file for 1 day
                        if (ctx.File.Name.ToLower() == "swagger.json")
                        {
                            ctx.Context.Response.Headers.Append(
                                "Cache-Control",
                                "public,max-age=86400"
                            );
                        }
                        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                        ctx.Context.Response.Headers.Append(
                            "Access-Control-Allow-Headers",
                            "Origin, X-Requested-With, Content-Type, Accept"
                        );
                    },
                }
            );

            app.UseRouting();

            //app.UseCookiePolicy();

            //Important! Register Cors Policy before Using Authentication and Authorization
            app.UseCors("CorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add(
                    (swagger, httpReq) =>
                    {
                        swagger.Servers = new List<OpenApiServer>
                        {
                            new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" },
                        };
                    }
                );
            });

            app.ApplicationServices.SaveSwaggerJson(
                Configuration.GetSection("ApiConfig").GetValue<string>("Url"),
                Configuration.GetSection("JsonConfig").GetValue<string>("Jsondir")
            );
            //app.UseSaveSwaggerJson(Configuration);

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Open Data Hub Tourism API V1");
                //c.SwaggerEndpoint("/json/swagger.json", "Open Data Hub Tourism API V1");
                c.RoutePrefix = "swagger";
                c.OAuthClientSecret("");
                c.OAuthRealm("noi");
                c.EnableDeepLinking();
            });

            //if (env.IsDevelopment())
            //{
            //    app.UseRateLimiting();
            //}

            app.UseRateLimiting();

            app.UseKeycloakAuthorizationService();

            ////LOG EVERY REQUEST WITH HEADERs
            app.UseODHCustomHttpRequestConfig(Configuration);

            //REWRITE, REDIRECT RULES
            //var rwoptions = new RewriteOptions()
            //    .AddRedirect("api/(.*)", "v1/$1");
            //.AddRedirectToHttpsPermanent();
            //.AddRedirect("redirect-rule/(.*)", "redirected/$1")
            //.AddRewrite(@"^rewrite-rule/(\d+)/(\d+)", "rewritten?var1=$1&var2=$2",
            //skipRemainingRules: true)

            app.UseRewriter(
                new RewriteOptions().AddRedirect("api/(.*)", "v1/$1")
            //.AddRewrite(@"^(?=/api)", "/v1", skipRemainingRules: true)
            );

            //Not needed at moment
            //app.UseHttpContext();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseHealthChecks(
                "/self",
                new HealthCheckOptions { Predicate = r => r.Name.Contains("self") }
            );

            app.UseHealthChecks(
                "/ready",
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("services") }
            );
        }
    }
}
