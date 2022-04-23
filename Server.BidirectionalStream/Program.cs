using System.Net;

using Microsoft.AspNetCore.Server.Kestrel.Core;

using Prometheus;

using Serilog;
using Serilog.OpenTelemetry;
using Serilog.OpenTelemetry.LoggiaConsoleSink;

using Server.BidirectionalStream.Interceptors;
using Server.BidirectionalStream.SerilogEnricher;
using Server.BidirectionalStream.Services;
using Server.BidirectionalStream.ServicesInterfaces;

WebApplicationOptions options = new()
{
    Args = args
};

var builder = WebApplication.CreateBuilder(options);

var app = ConfigureHost(builder).Build();

var appTask = ConfigApp(app).RunAsync();

await appTask;

throw new ApplicationException("Application was successfully crashed!");

/* Config Host & Services */
static WebApplicationBuilder ConfigureHost(WebApplicationBuilder builder)
{
    /* Seq & Loggia */
    builder.Host.UseSerilog((ctx, lc) => lc
        .Enrich.WithCaller()
        .Enrich.WithResource(
            ("server", Environment.MachineName),
            ("app", AppDomain.CurrentDomain.FriendlyName))
        .WriteTo.OpenTelemetry(new LoggiaConsoleSink())
        .ReadFrom.Configuration(ctx.Configuration)
    );

    builder.WebHost.ConfigureKestrel((_, opt) =>
    {
        var host = builder.Configuration.GetValue<string>("host");
        var port = builder.Configuration.GetValue<int>("port");

        opt.Limits.MinRequestBodyDataRate = null;

        opt.Listen(IPAddress.Parse(host), port, listenOptions =>
        {
            Log.Debug("{AppName} has started at [{StartTime}] (UTC)",
                AppDomain.CurrentDomain.FriendlyName,
                DateTime.UtcNow.ToString("F"));

            listenOptions.Protocols = HttpProtocols.Http2;
        });
    });

    //Service collection
    builder.Services.AddSingleton<ICacheAsync, CacheService>();
    builder.Services.AddGrpc(options =>
    {
        // https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/configuration.md
        options.Interceptors.Add<RequestLoggerInterceptor>();
        options.IgnoreUnknownServices =
            false; // it's default, should be changed after debug mode and compression should be enabled
        options.MaxReceiveMessageSize = 4194304;
        options.MaxSendMessageSize = 4194304;
        options.EnableDetailedErrors = true;
    });

    /* Prometheus */
    builder.Services.AddSingleton<PrometheusService>();

    return builder;
}

/* Config App */
static WebApplication ConfigApp(WebApplication app)
{
    using (var serviceScope = app.Services.GetService<IServiceScopeFactory>()?.CreateScope())
    {
        if (serviceScope != null)
        {
            /* Prometheus */
            var prometheus = serviceScope.ServiceProvider.GetRequiredService<PrometheusService>();
            prometheus.Init();
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        Log.ForContext("Mode", app.Environment.EnvironmentName);
        Log.Debug("App activated in [{Environment}] mode", app.Environment.EnvironmentName);
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseRouting();
    app.UseHttpMetrics();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGrpcService<GrpcService>();
        endpoints.MapGet("/",
            () =>
                "This is grpc service, you should use http2 for interaction");
    });

    return app;
}