
using Microsoft.Azure.SignalR.Management;

namespace AuthChannel.Services;
public interface IHubContextStore
{
    ServiceHubContext? MessageHubContext { get; set; }

}
public class SignalRService : IHostedService, IHubContextStore, IDisposable
{

    private const string MessageHub = "AzureChat";
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public ServiceHubContext? MessageHubContext { get; set; }

    public SignalRService(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        using var serviceManager = new ServiceManagerBuilder()
            .WithOptions(o => o.ConnectionString = _configuration["Azure:SignalR:ConnectionString"])
            .WithLoggerFactory(_loggerFactory)
            .WithNewtonsoftJson()
            .BuildServiceManager();

        MessageHubContext = await serviceManager.CreateHubContextAsync(MessageHub, cancellationToken);

    }


    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(Dispose(MessageHubContext!));
    }

    private static Task Dispose(ServiceHubContext hubContext)
    {
        if (hubContext == null)
        {
            return Task.CompletedTask;
        }
        return hubContext.DisposeAsync();
    }

    public void Dispose()
    {
        MessageHubContext?.Dispose();
    }
}