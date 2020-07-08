using AspNetCoreRateLimit;
using AutoSchedule.Common;
using AutoSchedule.Dtos.Data;
using AutoSchedule.Models;
using ExcuteInterface;
using FreeSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewLife.Caching;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace AutoSchedule
{
    public class Startup
    {
       
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            string SqlLiteConn = string.Empty;
          
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SqlLiteConn =  Configuration.GetConnectionString("SqlLiteLinux");
            }
            else
            {
                SqlLiteConn = Configuration.GetConnectionString("SqlLiteWin");
            }

            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
            #region 限流
            services.AddOptions();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            #endregion
            services.AddDbContext<SqlLiteContext>(options => options.UseSqlite(SqlLiteConn), ServiceLifetime.Scoped);
            services.AddHttpClient();
            services.AddExtendService(configure =>
            {
                configure.UseAutoTaskJob();
                configure.UseIOCJobFactory();
                configure.UseISchedulerFactory();
                configure.UseQuartzStartup();
            });
            services.AddSingleton<FreeSqlFactory>();
            services.AddSingleton<JobLogger>();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddMvc().AddNewtonsoftJson(s => s.SerializerSettings.ContractResolver = new DefaultContractResolver());
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        }

        private void getService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var quartzStartup = (QuartzStartup)  GetContext.getInstance().ServiceProvider.GetService(typeof(QuartzStartup));
                quartzStartup.rds.Clear();
            }
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,IHostApplicationLifetime lifetime)
        {
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIpRateLimiting();

            app.UseStaticFiles();
            lifetime.ApplicationStarted.Register(UnRegService);
            GetContext.getInstance().ServiceProvider = app.ApplicationServices;
            getService();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
        public HttpContext requestServices ;
        private void  UnRegService()
        {
            Console.WriteLine("网站启动成功,请勿关闭此窗口!");
            Console.WriteLine($"请访问:localhost:{Configuration.GetSection("urls").Value.Split(':')[2]} 进行下一步配置!");
            Console.WriteLine($"祝您生活愉快");
        }
    }
}