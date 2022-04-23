using Prometheus;

namespace Server.BidirectionalStream.Services;

public class PrometheusService
{
    private readonly ILogger<PrometheusService> _logger;

    private readonly Counter _processedJobCount = Metrics
        .CreateCounter("app_jobs_processed_total", "Number of processed jobs.");

    private readonly int _prometheusPort;

    private readonly Counter _tickTock =
        Metrics.CreateCounter("sampleapp_ticks_total", "Just keeps on ticking");

    public PrometheusService(IConfiguration configuration, ILogger<PrometheusService> logger)
    {
        Configuration = configuration;
        _logger = logger;

        _prometheusPort = Configuration.GetValue<int>("prometheus");
        var server = new MetricServer(_prometheusPort);
        server.Start();
        _processedJobCount.Inc();

        Task.Run(() =>
        {
            while (true)
            {
                _tickTock.Inc();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        });
    }

    private IConfiguration Configuration { get; }

    public void Init()
    {
        _logger.LogInformation("Prometheus server has been started on port {PrometheusPort} at {StartTime} (UTC) ",
            _prometheusPort, DateTime.UtcNow.ToString("F"));
    }
}