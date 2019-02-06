﻿using EhTagClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EhTagApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            RepositoryClient.Username = Configuration.GetValue<string>("GitHub:Username");
            RepositoryClient.Password = Configuration.GetValue<string>("GitHub:Password");
            RepositoryClient.Email = Configuration.GetValue<string>("GitHub:Email");
            RepositoryClient.Init();

            var db = new Database();
            db.Load();
            services.AddSingleton(db);

            services.AddHttpsRedirection(options => options.RedirectStatusCode = 301);

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter(new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()));
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseMvc();
        }
    }
}
