﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventFlow;
using EventFlow.AspNetCore.Extensions;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.EntityFramework;
using EventFlow.Extensions;
using EventStore.Module;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Vehicle.ReadStore;
using VehicleTracker.Infrastructure;

namespace Vehicle.Service
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            var middlewareConfig = new ServiceConfiguration().Create(new Dictionary<string, string>()
            {
                { nameof(ServiceConfiguration.EventDbConnection), _configuration.GetValue<string>(Identifiers.EventDbConnection) },
                { nameof(ServiceConfiguration.DbConnection), _configuration.GetValue<string>(Identifiers.DbConnection) }
            });
            services.AddSingleton(middlewareConfig)
                .AddSwaggerGen(c => c.SwaggerDoc("v1", new Info {Title = "VehicleModel API", Version = "v1"}));

            return EventFlowOptions.New
                .UseServiceCollection(services)
                .AddAspNetCoreMetadataProviders()
                .UseConsoleLog()
                .AddDefaults(typeof(VehicleReadModel).Assembly)
                .RegisterModule<Vehicle.ReadStore.Module.VehicleReadStoreModule>()
                .RegisterModule<EventSourcingModule>()
                .CreateServiceProvider();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {            
            // initialize InfoDbContext
            using (var scope = app.ApplicationServices.CreateScope())
            {
                //var efentflow = scope.ServiceProvider.GetService<EventFlow.ReadStores.IReadModel>();
                var dbContext = scope.ServiceProvider.GetService<IDbContextProvider<Vehicle.ReadStore.VehicleContext>>();
                dbContext.CreateContext();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VehicleModel API V1");
                });
                app.UseMvc();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
