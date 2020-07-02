using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.CloudFoundry;


namespace PalTracker
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
            services.AddCloudFoundryActuators(Configuration, Steeltoe.Management.Endpoint.MediaTypeVersion.V2, Steeltoe.Management.Hypermedia.ActuatorContext.ActuatorAndCloudFoundry);
            services.AddControllers();

            var message = Configuration.GetValue<string>("WELCOME_MESSAGE");

            var port = Configuration.GetValue<string>("PORT");
            var memory = Configuration.GetValue<string>("MEMORY_LIMIT");
            var index = Configuration.GetValue<string>("CF_INSTANCE_INDEX");
            var addr = Configuration.GetValue<string>("CF_INSTANCE_ADDR");
            
           if (string.IsNullOrEmpty(message))
           {
               throw new ApplicationException("WELCOME_MESSAGE not configured.");
           }
           services.AddDbContext<TimeEntryContext>(options => options.UseMySql(Configuration));
           services.AddSingleton(sp => new WelcomeMessage(message));
           services.AddSingleton( sp => new CloudFoundryInfo(port,memory,index,addr));
           //services.AddSingleton<ITimeEntryRepository, InMemoryTimeEntryRepository>();
           services.AddScoped<ITimeEntryRepository, MySqlTimeEntryRepository>();
           services.AddScoped<IHealthContributor, TimeEntryHealthContributor>();
           services.AddSingleton<IOperationCounter<TimeEntry>, OperationCounter<TimeEntry>>();
           services.AddSingleton<Steeltoe.Management.Endpoint.Info.IInfoContributor, TimeEntryInfoContributor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
              app.UseCloudFoundryActuators(Steeltoe.Management.Endpoint.MediaTypeVersion.V2, Steeltoe.Management.Hypermedia.ActuatorContext.ActuatorAndCloudFoundry);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
