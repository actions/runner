using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.FileProviders;
using Runner.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System;
using System.Linq;
using System.IO;
using System.IO.Pipes;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Quartz;
using Microsoft.Data.Sqlite;
using System.Reflection;
using Microsoft.AspNetCore.Rewrite;
using System.Net;
using Microsoft.Net.Http.Headers;

namespace Runner.Server
{
    public class Startup
    {
        public static RSAParameters AccessTokenParameter;
        public static string KeyId;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        private class CleanUpArtifactsAndCache : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                var b = new DbContextOptionsBuilder<SqLiteDb>();
                b.UseInMemoryDatabase("Agents");
                var artifactspath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
                var cachepath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "cache");
                var db = new SqLiteDb(b.Options);
                foreach(var rec in db.ArtifactRecords) {
                    File.Delete(Path.Combine(artifactspath, rec.StoreName));
                }
                foreach(var cache in db.Caches) {
                    File.Delete(Path.Combine(cachepath, cache.Storage));
                }
                return Task.CompletedTask;
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<IPolicyEvaluator, AgentAuthenticationPolicyEvaluator>();

            services.AddControllers(options => {
                // options.InputFormatters.Add(new LogFormatter());
            }).AddNewtonsoftJson();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Runner.Server", Version = "v1" });
                // add JWT Authentication
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // must be lower case
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {securityScheme, new string[] { }}
                });

            });
#if EF_MIGRATION
            // By default we use an InMemoryDatabase, which is incompatible with sqlite migrations
            var sqlitecon = "Data Source=Agents.db;";
#else
            var sqlitecon = Configuration.GetConnectionString("sqlite");
#endif
            bool useSqlite = sqlitecon?.Length > 0;
            if(!useSqlite) {
                services.AddHostedService<CleanUpArtifactsAndCache>();
            }
            Action<DbContextOptionsBuilder> optionsAction = conf => {
                if(useSqlite) {
                    conf.UseSqlite(sqlitecon);
                } else {
                    conf.UseInMemoryDatabase("Agents");
                }
            };
            services.AddDbContext<SqLiteDb>(optionsAction, ServiceLifetime.Scoped, ServiceLifetime.Singleton);
            
            if(useSqlite) {
                var b = new DbContextOptionsBuilder<SqLiteDb>();
                optionsAction(b);
                new SqLiteDb(b.Options).Database.Migrate();
                try {
                    using(var schema = Assembly.GetExecutingAssembly().GetManifestResourceStream("quartz/tables_sqlite.sql"))
                    using(var reader = new StreamReader(schema))
                    using (var connection = new SqliteConnection(sqlitecon))
                    {
                        connection.Open();
                        string sql = reader.ReadToEnd();
                        var command = new SqliteCommand(sql, connection);
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                } catch(SqliteException) {
                } catch(Exception ex) when (!(ex is SqliteException sqlex)) {
                    Console.WriteLine($"Failed to initialize sqlite for cron schedules: {ex.Message}\n{ex.StackTrace}");
                }
            }
            services.AddQuartz(c => {
                if(useSqlite) {
                    c.UsePersistentStore(configure => {
                        configure.UseProperties = true;
                        configure.UseMicrosoftSQLite(sqlitecon);
                        configure.UseJsonSerializer();
                    });
                } else {
                    c.UseInMemoryStore();
                }
                c.UseMicrosoftDependencyInjectionJobFactory();
            });
            services.AddQuartzHostedService();

            var sessionCookieLifetime = Configuration.GetValue("SessionCookieLifetimeMinutes", 60);
            services.AddSingleton<IAuthorizationHandler, DevModeOrAuthenticatedUser>();

            var rsa = RSA.Create();
            AccessTokenParameter = rsa.ExportParameters(true);
            KeyId = Guid.NewGuid().ToString();
            var auth = services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(setup => {
                setup.ExpireTimeSpan = TimeSpan.FromMinutes(sessionCookieLifetime);
                setup.Events.OnValidatePrincipal = context => {
                    var httpContext = context.HttpContext;
                    httpContext.Items["Properties"] = context.Properties;
                    httpContext.Features.Set(context.Properties);
                    return Task.CompletedTask;
                };
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters() {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidIssuer = "http://githubactionsserver",
                    ValidAudience = "http://githubactionsserver",
                };
            });

            // auth.AddNegotiate(opts => {
            //     // opts.Events.OnChallenge += async (Microsoft.AspNetCore.Authentication.Negotiate.ChallengeContext chp) => {
                    
            //     //     await Task.CompletedTask;
            //     //     // chp.HandleResponse();
            //     //     // chp.Properties.
            //     // };
            //     // //opts.Events.OnAuthenticated
            // });

            services.AddAuthorization(options => {

                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new DevModeOrAuthenticatedUserRequirement()).Build();
                options.AddPolicy("AgentManagement", policy => policy.RequireClaim("Agent", "management"));
                options.AddPolicy("AgentManagementRead", policy => policy.RequireClaim("Agent", "management", "oauth"));
                options.AddPolicy("Agent", policy => policy.RequireClaim("Agent", "oauth"));
                options.AddPolicy("AgentJob", policy => policy.RequireClaim("Agent", "oauth", "job"));
            });
            if(Configuration["Authority"] != null && Configuration["ClientId"] != null  && Configuration["ClientSecret"] != null) {
                auth.AddOpenIdConnect(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters() {
                        ValidateActor = false,
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateIssuerSigningKey = false,
                        ValidateLifetime = false,
                        ValidateTokenReplay = false,
                        SignatureValidator = delegate (string token, TokenValidationParameters parameters)
                        {
                            var jwt = new JwtSecurityToken(token);
                            return jwt;
                        }
                    };
                    // options.Events.OnTokenValidated = async ctx => {
                    //     ctx.Principal
                    // };
                    // options.SecurityTokenValidator = new Validator();
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = Configuration["Authority"];
                    options.SignedOutRedirectUri = "https://localhost";
                    options.ClientId = Configuration["ClientId"];
                    options.ClientSecret = Configuration["ClientSecret"];
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.RequireHttpsMetadata = false;
                    options.Scope.Add("openid");
                });
            }

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                                builder =>
                                {
                                    builder.AllowAnyOrigin();
                                    builder.AllowAnyHeader();
                                    builder.AllowAnyMethod();
                                });
            });
            services.AddMemoryCache();
            services.Configure<KestrelServerOptions>(options => {
                options.Limits.MaxRequestBodySize = int.MaxValue;
            });
        }

        public class AgentAuthenticationPolicyEvaluator : IPolicyEvaluator
        {
            private IConfiguration configuration;
            private PolicyEvaluator policyEvaluator;
            private bool byPassAuth;

            public AgentAuthenticationPolicyEvaluator(IConfiguration configuration, IAuthorizationService authorization)
            {
                this.configuration = configuration;
                this.policyEvaluator = new PolicyEvaluator(authorization);
                this.byPassAuth = configuration.GetSection("Runner.Server")?.GetValue<bool>("byPassAuth") ?? false;
            }

            public async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
            {
                if(policy.Requirements.FirstOrDefault() is ClaimsAuthorizationRequirement claimreq && claimreq.ClaimType == "Agent" && claimreq.AllowedValues?.FirstOrDefault() == "management") {
                    var token = configuration.GetSection("Runner.Server")?.GetValue<String>("RUNNER_TOKEN") ?? "";
                    if(string.IsNullOrEmpty(token)) {
                        var authenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), JwtBearerDefaults.AuthenticationScheme);
                        return AuthenticateResult.Success(authenticationTicket);
                    }
                    string authheader = context.Request.Headers.Authorization;
                    var basicauthprefix = "Basic ";
                    if(!string.IsNullOrEmpty(authheader) && authheader.StartsWith(basicauthprefix, StringComparison.OrdinalIgnoreCase)) {
                        authheader = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(authheader.Substring(basicauthprefix.Length)));
                        var nameandtoken = authheader.Split(":", 2);
                        if(nameandtoken?.Length == 2 && nameandtoken[1] == token) {
                            var authenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), JwtBearerDefaults.AuthenticationScheme);
                            return AuthenticateResult.Success(authenticationTicket);
                        }
                    }
                }
                var authResult = await policyEvaluator.AuthenticateAsync(policy, context);
                if(!authResult.Succeeded && byPassAuth) {
                    var authenticationTicket = new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), JwtBearerDefaults.AuthenticationScheme);
                    return AuthenticateResult.Success(authenticationTicket);
                }
                return authResult;
            }

            public async Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object resource)
            {
                if(policy.Requirements.FirstOrDefault() is ClaimsAuthorizationRequirement claimreq && claimreq.ClaimType == "Agent" && claimreq.AllowedValues?.Contains("management") == true) {
                    var token = configuration.GetSection("Runner.Server")?.GetValue<String>("RUNNER_TOKEN") ?? "";
                    if(string.IsNullOrEmpty(token)) {
                        return PolicyAuthorizationResult.Success();
                    }
                    string authheader = context.Request.Headers.Authorization;
                    var basicauthprefix = "Basic ";
                    if(!string.IsNullOrEmpty(authheader) && authheader.StartsWith(basicauthprefix, StringComparison.OrdinalIgnoreCase)) {
                        authheader = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(authheader.Substring(basicauthprefix.Length)));
                        var nameandtoken = authheader.Split(":", 2);
                        if(nameandtoken?.Length == 2 && nameandtoken[1] == token) {
                            return PolicyAuthorizationResult.Success();
                        }
                    }
                }
                var authResult = await policyEvaluator.AuthorizeAsync(policy, authenticationResult, context, resource);
                if(!authResult.Succeeded && byPassAuth) {
                    return PolicyAuthorizationResult.Success();
                }
                return authResult;
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            app.UseHttpLogging();
            var pipe = Environment.GetEnvironmentVariable("RUNNER_CLIENT_PIPE");
            if(pipe != null) {
                lifetime.ApplicationStarted.Register(() => {
                    var addr = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
                    using (PipeStream pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipe)) {
                        using (StreamWriter sr = new StreamWriter(pipeClient))
                        {
                            sr.WriteLine(addr);
                        }
                    }
                });
            }
            var shutdownPipe = Environment.GetEnvironmentVariable("RUNNER_CLIENT_PIPE_IN");
            if(shutdownPipe != null) {
                Task.Run(() => {
                    using (PipeStream pipeClient = new AnonymousPipeClientStream(PipeDirection.In, shutdownPipe)) {
                        using (StreamReader rd = new StreamReader(pipeClient)) {
                            var line = rd.ReadLine();
                            if(line == "shutdown") {
                                lifetime.StopApplication();
                            }
                        }
                    }
                });
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Runner.Server v1"));
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWebSockets();
    
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var defaultWebUIView = Configuration.GetSection("Runner.Server")?.GetValue<string>("DefaultWebUIView");
            if(!string.IsNullOrEmpty(defaultWebUIView)) {
                var rewriteOptions = new RewriteOptions();
                rewriteOptions.Rules.Add(new AllWorlflowsRedirect(defaultWebUIView));
                app.UseRewriter(rewriteOptions);
            }

            DefaultFilesOptions options = new DefaultFilesOptions();
            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(options);
            app.UseStaticFiles();
        }
    }

    internal class AllWorlflowsRedirect : IRule
    {
        public AllWorlflowsRedirect(string defaultWebUIView) {
            DefaultWebUIView = defaultWebUIView;
        }

        public string DefaultWebUIView { get; }

        public void ApplyRule(RewriteContext context)
        {
            var request = context.HttpContext.Request;            
            if (request.Path.Value == null || request.Query.ContainsKey("view") || request.Path != "/")
            {
                return;
            }

            var response = context.HttpContext.Response;
            response.StatusCode = (int) HttpStatusCode.Moved;
            context.Result = RuleResult.EndResponse;
            response.Headers[HeaderNames.Location] = "/" + request.QueryString.Add("view", DefaultWebUIView);
        }
    }
}
