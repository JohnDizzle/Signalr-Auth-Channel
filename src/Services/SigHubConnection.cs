using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Azure;
using Microsoft.Graph;
using Microsoft.Graph.TermStore;
using System.Runtime.CompilerServices;

namespace FireCore.Services
{
    public class HubTemporay : Hub {
        
        public HubTemporay() {
        
        }
        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("Received");
        }

    }
    public interface ISigHubConnection {
        HubConnection CreateHubConnection(string hubEndpoint, string userId);
        List<string> Messages { get; set; }

        HubConnection HubMessenger { get; set;  }
        Task InitializeUserHub(string user); 
    }
    public sealed class SigHubConnection :  ISigHubConnection
    {

        //public string MessageHubEndpoint = "https://localhost:7065/message/negotiate";
        ////"https://energy.service.signalr.net/?hub=message;AccessKey=HEJiDy8KLt4s8ixioCIWTCczqHAVzzZ9WXpJUQzzpyc=;Version=1.0;";
        //string Target = "Target";
        //string DefaultUser = "TestUser";
        private const string EnableDetailedErrors = "EnableDetailedErrors";
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHubContextStore _store;
        private readonly bool _enableDetailedErrors;
        public HubConnection HubMessenger { get; set; }
        public SigHubConnection(IConfiguration configuration, ILoggerFactory loggerFactory, IHubContextStore store)
        {
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _store = store;
            _enableDetailedErrors = configuration.GetValue(EnableDetailedErrors, false);


        }


        public async Task InitializeUserHub(string user) {

            var hub = await _store.MessageHubContext.NegotiateAsync(new()
            {
                UserId = user,
                EnableDetailedErrors = _enableDetailedErrors
            });
               
            try
            {
                var hubConnection = CreateHubConnection(MessageHubEndpoint, user);
                await hubConnection.StartAsync();
                
            }
            catch (Exception)
            {
                throw ;
            }

        }
        public List<string> Messages { get; set; } = new List<string>();
        string MessageHubEndpoint => "https://localhost:7065/chatcore";
        string Target => "Target";

        string DefaultUser => "TextUser";

        //public async Task<HubConnection> CreateHubConnection(string hubEndpoint, string userId)
        //{

        //    try
        //    {

        //        var url = hubEndpoint.TrimEnd('/') + $"?user={userId}";

        //        using HubConnection connection = new HubConnection(hubEndpoint.TrimEnd('/'), $"?user={userId}");
        //        connection.TransportConnectTimeout = connection.DeadlockErrorTimeout = TimeSpan.FromSeconds(10.0);

        //        IHubProxy hubProxy = connection.CreateHubProxy("MessageHub");
        //        hubProxy.On(Target, (string message) => {
        //            Messages.Add(message);
        //            Console.WriteLine($"{userId}: gets message from service: '{message}'");
        //        });

        //        await connection.Start(new LongPollingTransport());


        //        return connection;


        //        //connection.On(Target, (string message) =>
        //        //{
        //        //    Console.WriteLine($"{userId}: gets message from service: '{message}'");
        //        //});


        //        //connection.Closed += ex =>
        //        //{
        //        //    Console.Write($"The connection of '{userId}' is closed.");
        //        //    //If you expect non-null exception, you need to turn on 'EnableDetailedErrors' option during client negotiation.
        //        //    if (ex != null)
        //        //    {
        //        //        Console.Write($" Exception: {ex}");
        //        //    }
        //        //    Console.WriteLine();
        //        //    return Task.CompletedTask;
        //        //};
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        Console.WriteLine(ex.StackTrace);
        //        return null;
        //    }


        //}

        public HubConnection CreateHubConnection(string hubEndpoint, string userId)
        {
            HubConnection connection;
            try
            {

                //var url =  hubEndpoint.TrimEnd('/') + $"?user={userId.Replace(" ", "-")}";
                connection = new HubConnectionBuilder().WithUrl(hubEndpoint).Build();
                connection.On(Target, (string message) =>
                {
                    Messages.Add(message);
                    Console.WriteLine($"{userId}: gets message from service: '{message}'");
                });

                connection.Closed += async ex =>
                {
                    Console.Write($"The connection of '{userId}' is closed.");
                    //If you expect non-null exception, you need to turn on 'EnableDetailedErrors' option during client negotiation.
                    if (ex != null)
                    {
                        Console.Write($" Exception: {ex}");
                    }
                    
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await connection.StartAsync();

                    Console.WriteLine();
                    
                };
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }

            return connection;
        }

    }
    }

