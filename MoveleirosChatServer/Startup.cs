using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovChat.Core.Hub;
using MovChat.Data;
using MovChat.Data.Repositories;
using MovChat.PushNotification;
using MoveleirosChatServer.Auth;
using MoveleirosChatServer.Channels;
using MoveleirosChatServer.Data;
using MoveleirosChatServer.Utils;

namespace MoveleirosChatServer
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
            services.AddMvc();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddSignalR();

            services.AddSingleton(typeof(DefaultHubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultPresenceHublifetimeManager<>));
            services.AddSingleton(typeof(IUserTracker<>), typeof(InMemoryUserTracker<>));
            services.AddScoped<LivechatContext>();
            services.AddScoped<LivechatRules>();
            services.AddScoped<UOW>();
            services.AddScoped<PushNotificationService>();
            services.AddTransient<SQLFactory>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<CustomExceptionHandlerMiddleware>();

            app.UseCors("AllowAll");

            app.UseFileServer();

            // Bug fix
            app.Use(async (context, next) => {
                context.Request.Headers.Remove("If-Modified-Since");

                await next();
            });

            // Deal with X-StoreId header
            app.Use(async (context, next) =>
            {
                var request = context.Request;
                var x = request.Path.Value;
                var query = request.Query;

                if (x.StartsWith("/mktchat") || x.StartsWith("/mktpush"))
                {
                    if (query.TryGetValue("storeId", out var storeId))
                    {
                        request.Headers.Add("storeId", storeId);
                    }

                    if (query.TryGetValue("authorization", out var authorization))
                    {
                        request.Headers.Add("Authorization", $"Bearer {authorization}");
                    }

                    if (query.TryGetValue("access_token", out var access_token))
                    {
                        request.Headers.Add("Authorization", $"Bearer {access_token}");
                    }
                }

                await next();
            });

            app.Use(async (context, next) =>
            {
                await JwtMiddleware.UseJwtMiddleware(context, next);
            });

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Livechat}/{action=Register}/{id?}");
            });

            app.UseSignalR(routes =>
            {
                routes.MapHub<MktChatHub>("/mktchat");
            });
        }
    }
}
