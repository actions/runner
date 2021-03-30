using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.FileProviders;
using Runner.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Runner.Server
{
    public class Startup
    {

        // private static byte[] privatekey;
        // private static byte[] publickey;
        // private static RSAParameters parameters;
        // public static RSA Key {
        //     get {
        //         if(privatekey == null || publickey == null) {
        //             RSA rsa = RSA.Create(4096);
        //             publickey = rsa.ExportRSAPublicKey();
        //             privatekey = rsa.ExportRSAPrivateKey();
        //             parameters = rsa.ExportParameters(true);
        //             return rsa;
        //         } else {
        //             RSA rsa = RSA.Create(parameters);
        //             rsa.ImportParameters(parameters);
        //             rsa.ImportRSAPublicKey(publickey, out _);
        //             rsa.ImportRSAPrivateKey(privatekey, out _);
        //             return rsa;
        //         }
        //     }
        // }
        // public static RSA ORGRSA = Key;
        // public static RSA Key = RSA.Create();
        public static HMACSHA512 Key = new HMACSHA512();
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            using(var client = new SqLiteDb())
            {
                client.Database.EnsureCreated();
            }
        }

        public IConfiguration Configuration { get; }
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => {
                // options.InputFormatters.Add(new LogFormatter());
            }).AddNewtonsoftJson();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Runner.Server", Version = "v1" });
                // add JWT Authentication
                // var securityScheme = new OpenApiSecurityScheme
                // {
                //     Name = "JWT Authentication",
                //     Description = "Enter JWT Bearer token **_only_**",
                //     In = ParameterLocation.Header,
                //     Type = SecuritySchemeType.Http,
                //     Scheme = "bearer", // must be lower case
                //     BearerFormat = "JWT",
                //     Reference = new OpenApiReference
                //     {
                //         Id = JwtBearerDefaults.AuthenticationScheme,
                //         Type = ReferenceType.SecurityScheme
                //     }
                // };
                // c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                // c.AddSecurityRequirement(new OpenApiSecurityRequirement
                // {
                //     {securityScheme, new string[] { }}
                // });

            });
            // services.AddDbContext<InMemoryDB>(options => options.UseInMemoryDatabase("db"));
            services.AddDbContext<SqLiteDb>();
            // services.AddAuthentication(x =>
            // {
            //     x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //     x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            // })
            // .AddJwtBearer(x =>
            // {
            //     x.RequireHttpsMetadata = false;
            //     x.SaveToken = true;
            //     x.TokenValidationParameters = new TokenValidationParameters
            //     {
            //         ValidateIssuerSigningKey = true,
            //         IssuerSigningKey = new SymmetricSecurityKey(Key.Key) /* new RsaSecurityKey(Key) */,
            //         ValidateIssuer = false,
            //         ValidateAudience = false
            //     };
            // });
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
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Runner.Server v1"));
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);

            // app.UseAuthentication();
            // app.UseAuthorization();
            
            // app.UseStaticFiles(new StaticFileOptions(new Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions(){RequestPath = new Microsoft.AspNetCore.Http.PathString("/test"), FileProvider = new PhysicalFileProvider("C:/Users/Christopher/runner")}){ServeUnknownFileTypes = true});

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseStaticFiles();
        }
    }
}
