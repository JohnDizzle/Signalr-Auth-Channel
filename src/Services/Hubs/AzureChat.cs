using AuthChannel.Data.Contacts;
using AuthChannel.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Immutable;
using System.Data;

namespace AuthChannel.Services.Hubs
{

    [Authorize]
    public class AzureChat : Hub
    {
        private readonly IMessageHandler _messageHandler;
        private readonly SignalRService _signalRService;
        private readonly IHubCommander _commander;
        private readonly Microsoft.Graph.GraphServiceClient _graphServiceClient;
        private readonly HttpClient? _httpClient;
        private readonly IAzureDataTableSessionStorage _azureDataTableSessionStorage;
        private readonly ILogger<AzureChat> _logger;
        private static readonly object ConnectedIdsLock = new();
        private static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        static HashSet<KeyValuePair<string, string>> ConnectedIds = new HashSet<KeyValuePair<string, string>>();


        public AzureChat(IMessageHandler messageHandler, SignalRService rService, IHubCommander hubCommander, Microsoft.Graph.GraphServiceClient graphServiceClient, HttpClient httpClient, IAzureDataTableSessionStorage azureDataTable, ILogger<AzureChat> logger)
        {
            _messageHandler = messageHandler;
            _signalRService = rService;
            _commander = (IHubCommander)hubCommander;
            _graphServiceClient = graphServiceClient;
            _httpClient = httpClient;
            _azureDataTableSessionStorage = azureDataTable;
            _logger = logger; 
        }
        

        public override async Task OnConnectedAsync()
        {
            var sender = Context.UserIdentifier;
        
            lock (ConnectedIdsLock)
            {
                if (Context.UserIdentifier != null)
                    ConnectedIds.Add(new KeyValuePair<string, string>(Context.ConnectionId, Context.UserIdentifier));
            }
            await Clients!.All.SendAsync("showLoginUsers", ConnectedIds.Select(x => x.Value).ToArray());

            // Gather user session from database
            var userSessions = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetLatestSessionsAsync(sender!), _logger, $"Get Sessions");

            //  Push the latest session information to the commander.
            await AddSessionsToCommander(userSessions!);

            //  Send to latest session list to user.
            await Clients!.Caller.SendAsync("updateSessions", userSessions);

            var onConnectedMessage = sender + " joined the chat room";
            var message = new Message("Public", DateTime.Now, onConnectedMessage, "Sent");

            await Clients!.All.SendAsync("sendNotify", onConnectedMessage);
            // send badge to all users
            await BroadcastUserGroupCounts();

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var sender = Context.UserIdentifier;
            
            lock (ConnectedIdsLock)
            {
                if (Context.UserIdentifier != null)
                    ConnectedIds.Remove(new KeyValuePair<string, string>(Context.ConnectionId, Context.UserIdentifier));
            }
           
            await Clients!.All.SendAsync("showLoginUsers", ConnectedIds.Select(x => x.Value).ToArray());
       
            // Remove the user from the ActiveGroups in the commander
            var listofActive = _commander.ActiveGroups;

            foreach (var group in listofActive)
            {
                if (group.Key.Item1 == Context.ConnectionId)
                {
                    await _commander.DeleteAsync(new Tuple<string, string>(group.Key.Item1, group.Key.Item2));
                }
            }

            var onDisconnectedMessage = sender + " left the chat room";
            var message = new Message("Public", DateTime.Now, onDisconnectedMessage, "Sent");
            await Clients!.All.SendAsync("sendNotify", onDisconnectedMessage);
            // send badge to all users
            await BroadcastUserGroupCounts();
            await base.OnDisconnectedAsync(exception);

        }

        public async Task AddSessionsToCommander(KeyValuePair<string, Session>[] userSessions)
        {

            await _commander.DeleteAllSessionsByUser(Context.ConnectionId);

            foreach (var session in userSessions)
            {
                await _commander.WriteAsync(new SessionDbEntity(Context.ConnectionId, session.Key, session.Value.SessionId, DateTimeOffset.Now));
                await AddToGroup(session.Key);
            }

            // add public 
            await _commander.WriteAsync(new SessionDbEntity(Context.ConnectionId, "Public", "Public", DateTimeOffset.Now));

        }

        public async Task AddToGroup(string groupName)
        {
            try
            {
                await Groups!.AddToGroupAsync(Context.ConnectionId, groupName);
            }
            catch (Exception)
            {
                _logger.LogError("Error adding to group: " + groupName);
                
            }
            
        }

        public async Task RemoveFromGroup(string groupName)
        {
            try
            {
                await Groups!.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }
            catch (Exception)
            {
                _logger.LogError("Error adding to group: " + groupName);
            }
        }


        public async Task<string> BroadcastMessage(string messageContent)
        {
            var sender = Context.UserIdentifier;
            var message = new Message(sender!, DateTime.Now, messageContent, "Sent");
            var sequenceId = await _messageHandler.AddNewMessageAsync("Public", message);

            await _commander.WriteAsync(new SessionDbEntity(Context.ConnectionId, "Public", "Public", DateTimeOffset.Now));

            await Clients!.Others.SendAsync("displayUserMessage", "Public", sequenceId, sender, messageContent);
            
            return sequenceId;
        }

        public Task<Dictionary<string, int>> GetUserGroupCounts()
        {
            var groups = _commander.ActiveGroups
                .GroupBy(x => x.Value) // x.Value is sessionId
                .ToDictionary(
                    g => g.Key, // sessionId
                    g => g.Count() // number of users in this session
                );
            return Task.FromResult(groups);
        }

        public async Task BroadcastUserGroupCounts()
        {
            // Get the user group counts per room.
            var groupCounts = await GetUserGroupCounts(); 
            await Clients!.All.SendAsync("updateUserCount", groupCounts);

        }
        public async Task<string> GetOrCreateSession(string receiver)
        {
            // validate existing and andOR create new session if not exists.

            var sender = Context.UserIdentifier;
            var session = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetOrCreateSessionAsync(sender!, receiver), _logger, $"Creating a Session"); 
            var isSession = await SafeExecutor.ExecuteAsync(async ()=> await _azureDataTableSessionStorage.GetSessionBySessionId(session!.SessionId), _logger, $"Get Session By SessionId");    

            var isListedCommander = _commander.ActiveGroups.ToDictionary().Where(w => w.Key.Item2 == receiver).Where(x => x.Value == session?.SessionId).ToDictionary();

            if (await _signalRService.MessageHubContext!.ClientManager.UserExistsAsync(receiver))
            {
                var userSessions = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetLatestSessionsAsync(sender!), _logger, $"Get Sessions");
                await Clients!.User(receiver).SendAsync("updateSessions", userSessions!);
                await AddSessionsToCommander(userSessions!);
            }
            else
            {
                var userSessions = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetLatestSessionsAsync(sender!), _logger, $"Get Sessions");
                await Clients!.Caller.SendAsync("updateSessions", userSessions!);
                await AddSessionsToCommander(userSessions!);
            }

            return session.SessionId;

        }
        public async Task<string> SendUserMessage(string sessionId, string roomName, string messageContent)
        {
            roomName = roomName.ToLower();
            var sender = Context.UserIdentifier;
            var message = new Message(sender!, DateTime.Now, messageContent, "Sent");
            var sequenceId = await SafeExecutor.ExecuteAsync(async () => await _messageHandler.AddNewMessageAsync(sessionId, message), _logger, $"Get Message SequenceId"); 
            // Check if the session exists in the database
            var listOfSessions = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetSessionBySessionId(sessionId), _logger, $"Gather Sessions") ?? null;
            // if not found, create a new session and add to the group the caller's message is already on their screen. 

            if (listOfSessions is null)
            {
                await _commander.WriteAsync(new SessionDbEntity(Context.ConnectionId, roomName, sessionId, DateTimeOffset.Now));
                await AddToGroup(roomName);
                await Clients!.GroupExcept(roomName, Context.ConnectionId).SendAsync("displayUserMessage", sessionId, sequenceId, roomName, messageContent);
                return sessionId;
            }

            //  get all user that are federated
            var hubUsers = _commander.MsalCurrentUsers!.ToList();

            // this means that the Room is actually a private chat - "An Auth User", so roll 
            var twins = (from v in hubUsers where v.UserPrincipalName == roomName select v.UserPrincipalName).Distinct();

            // 1. send update to group or private chat.  2. send owner update

            if (twins.Count() > 0)
            {    // private chat
                await Clients!.User(roomName).SendAsync("displayUserMessage", sessionId, sequenceId, Context.UserIdentifier!, messageContent);
                await Clients!.User(roomName).SendAsync("sendPrivateMessage", sessionId, string.Format("You have a private message from: {0}", Context!.UserIdentifier), messageContent, sender!);
                await Clients!.Caller.SendAsync("displayResponseMessage", sessionId, sequenceId, "Sent");
            }
            else
            {
                // group message 
                await Clients!.GroupExcept(roomName, Context.ConnectionId).SendAsync("displayUserMessage", sessionId, sequenceId, sender!, messageContent);
                await Clients!.Caller.SendAsync("displayResponseMessage", sessionId, sequenceId, "Sent");
            }

            return sessionId;


        }

        public async Task<string> SendUserResponse(string sessionId, string sequenceId, string receiver, string messageStatus)
        {
            await SafeExecutor.ExecuteAsync(async () => await _messageHandler.UpdateMessageAsync(sessionId, sequenceId, messageStatus), _logger, $"Update Message ${sessionId}"); 

            await Clients!.User(receiver).SendAsync("displayResponseMessage", sessionId, sequenceId, messageStatus);

            return messageStatus;
        }


        public async Task<Task> DeleteUserSession(string userName, string partnerName)
        {
            var sender = Context.UserIdentifier;
            var items = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetLatestSessionsAsync(userName), _logger, "Get Sessions"); 
            var Removed = (from kv in items where kv.Key == partnerName select kv);
            // delete session from table 
            await _azureDataTableSessionStorage.DeleteUserSession(userName, partnerName);
            // delete from static controller
            await _commander.DeleteAsync(new Tuple<string, string>(Context.ConnectionId, partnerName));
            // remove from siganlr groups 
            await RemoveFromGroup(partnerName);
            //  get latest session information to the user.
            var userSessions = await SafeExecutor.ExecuteAsync(async () => await _azureDataTableSessionStorage.GetLatestSessionsAsync(userName), _logger, "Post Delete -> Gather Remaining Sessions."); 
            //  Send to latest session list to user (caller).
            await Clients!.Caller.SendAsync("updateSessions", userSessions);

            if (userSessions!.Count() > 0)
                //  Send to latest session list to user multiple sessions.
                await Clients!.User(userName).SendAsync("updateSessions", userSessions);

            return Task.CompletedTask;
        }

        // introuduce for currentancy 
        public static class SafeExecutor
        {
            public static async Task<T?> ExecuteAsync<T>(
                Func<Task<T>> taskFunc,
                ILogger logger,
                string operationName = "")
            {
                
                try
                {
                    await semaphore.WaitAsync();
                    var result = await taskFunc();
                    logger.LogInformation("Operation {OperationName} succeeded.", operationName);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Operation {OperationName} failed: {Message}", operationName, ex.Message);
                    return default;
                }
                finally {
                    semaphore.Release(); 
                }
            }
        }

        public async Task<List<Message>> LoadMessages(string sessionId)
        {
            return await SafeExecutor.ExecuteAsync(async ()=> await _messageHandler.LoadHistoryMessageAsync(sessionId), _logger, "LoadMessages") ?? new List<Message>();
        }

        #region Retry Logic - not used currently, but could be useful for future enhancements

        //public async Task RetrySendMessage(Func<Task> sendMessage, int maxRetries = 3, int delayMilliseconds = 500)
        //{
        //    for (int attempt = 1; attempt <= maxRetries; attempt++)
        //    {
        //        try
        //        {
        //            await sendMessage();
        //            return; // Exit if successful
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
        //            if (attempt == maxRetries)
        //                throw; // Re-throw after max retries
        //            await Task.Delay(delayMilliseconds * attempt); // Exponential backoff
        //        }
        //    }
        //}
        //public async Task SendMessageWithRetry(string messageContent, int maxRetries = 3, int delayMilliseconds = 500)
        //{
        //    await RetrySendMessage(async () =>
        //    {
        //        var sender = Context.UserIdentifier;
        //        var message = new Message(sender!, DateTime.Now, messageContent, "Sent");
        //        var sequenceId = await _messageHandler.AddNewMessageAsync("Public", message);

        //        await Clients!.Others.SendAsync("displayUserMessage", "Public", sequenceId, sender, messageContent);
        //    }, maxRetries, delayMilliseconds);
        //}

        #endregion
    }
}
