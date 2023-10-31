using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portal.Model;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Azure.Identity;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Reflection;

namespace Portal
{
    public class Startup
    {
        private string _AuthServerAuthority;
        private string _AuthServerApiName;

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            // Configuration = configuration;
            DefaultAzureCredentialOptions credentialOptions = new DefaultAzureCredentialOptions();
            credentialOptions.ExcludeManagedIdentityCredential = false;
            var appConfigPath = configuration["AppConfigPath"];

#if (DEBUG)
            String strPath = Assembly.GetExecutingAssembly().Location;

            if (strPath.IndexOf("Development", StringComparison.OrdinalIgnoreCase) > -1)
            {
                appConfigPath = configuration["Development_AppConfigPath"];
                credentialOptions.ManagedIdentityClientId = configuration["Development_ManagedIdentityClientId"];
            }

            if (strPath.IndexOf("Main", StringComparison.OrdinalIgnoreCase) > -1)
            {
                appConfigPath = configuration["Main_AppConfigPath"];
                credentialOptions.ManagedIdentityClientId = configuration["Main_ManagedIdentityClientId"];
            }

            if (strPath.IndexOf("Release", StringComparison.OrdinalIgnoreCase) > -1)
            {
                appConfigPath = configuration["Release_AppConfigPath"];
                credentialOptions.ManagedIdentityClientId = configuration["Release_ManagedIdentityClientId"];
            }
#endif
            DefaultAzureCredential credential = new DefaultAzureCredential(credentialOptions);

            //var builder = new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            ////.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) test
            ////.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false)
            ////.AddEnvironmentVariables() 
            //.AddAzureAppConfiguration(options =>
            //{
            //    options.Connect(new Uri(appConfigPath), credential)
            //    //.Select(keyFilter: "Portal:*", "Common:*")
            //    .Select(keyFilter: "Portal:*")
            //    .UseFeatureFlags();
            //});

            var builder = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false);

            Configuration = builder.Build();
            _AuthServerAuthority = Configuration["Portal:AuthServerAuthority"];
            _AuthServerApiName = Configuration["Portal:AuthServerApiName"];
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson();

            services.Configure<AppSettings>(Configuration.GetSection("Portal"));
            //services.AddAzureAppConfiguration();

            //// services.AddApplicationInsightsTelemetry();
            //services.AddApplicationInsightsTelemetry(Configuration["Portal:ApplicationInsightsKey"]);

            services.AddAuthentication("Bearer")
                    .AddIdentityServerAuthentication(options =>
                    {
                        options.Authority = _AuthServerAuthority;
                        options.RequireHttpsMetadata = false;
                        options.ApiName = _AuthServerApiName;
                    });

            services.AddAuthorization(options =>
            {
                //options.AddPolicy("ApiAccess", policy =>
                //   policy.RequireAssertion(context =>
                //       context.User.HasClaim(c => c.Type == "idp")));
                //options.AddPolicy("ApiAccess", policy => policy.RequireClaim("scope", "GptwClientLoginScope"));
            });

            services.AddControllersWithViews();
            services.AddRazorPages();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");

            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                spa.Options.StartupTimeout = System.TimeSpan.FromSeconds(999);
                spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                {
                    OnPrepareResponse = context =>
                    {
                        //string foo = "123";
                        //Debug.WriteLine("USESPA: " + context.File.Name);
                        // never cache index.html
                        if (context.File.Name == "index.html")
                        {
                            context.Context.Response.Headers.Add("Cache-Control", "no-cache, no-store");
                            context.Context.Response.Headers.Add("Expires", "-1");
                        }
                    }
                };
                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });

        }

    }
}
