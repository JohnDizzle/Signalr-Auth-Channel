using AuthChannel.Services;
using Microsoft.AspNetCore.SignalR;

namespace AuthChannel.Services;

/// <summary> -- WorkerBroadcastManagementSignalR -- Using SignalR Management --	
/// Background service that periodically broadcasts a notification message to all connected SignalR clients.
/// Utilizes dependency injection to obtain the SignalR hub context and sends messages every 10 seconds.
/// Logs each broadcast operation for monitoring purposes.
/// </summary>

public class WorkerBroadcastManagementSignalR : BackgroundService
{
	private readonly ILogger<WorkerBroadcastManagementSignalR> _logger;
	IServiceScopeFactory _serviceScope;
	public WorkerBroadcastManagementSignalR(ILogger<WorkerBroadcastManagementSignalR> logger, IServiceScopeFactory serviceScopeFactory)
	{
		_logger = logger;
		_serviceScope = serviceScopeFactory;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var scope = _serviceScope.CreateScope();
		var sigMsgContext = scope.ServiceProvider.GetService<SignalRService>();
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_logger.IsEnabled(LogLevel.Information))
			{
				await sigMsgContext!.MessageHubContext!.Clients!.All.SendAsync("sendNotify", "Message from worker " + DateTime.Now.ToString(), stoppingToken);

				_logger.LogInformation("WorkerBroadcastManagementSignalR running at: {time}", DateTimeOffset.Now);
			}
			await Task.Delay(10000, stoppingToken);
		}
	}
}
