using Azure.Data.Tables;
using AuthChannel.Services.Hubs;
using AuthChannel.Services;
using AuthChannel.Data.Models;
using AuthChannel.Data.Contacts;

namespace AuthChannel.Data.SessionHandler.AzureTableSessionStorage;

//https://github.dev/microsoft/TeamCloud/blob/c9f7bd085798468a3edd8fb3dcf089c8a5ab5759/src/TeamCloud.API/Services/OneTimeTokenService.cs#L45#L81
public class AzureDataTableSession : IAzureDataTableSessionStorage
{
    private TableServiceClient? _tableServiceClient { get; set; }
    private TableClient? _tableClient { get; set; }
    private TableEntity? _tableEntity { get; set; }
    private readonly IConfiguration? _configuration;
    private readonly SignalRService _signalRService;
    private readonly IHubCommander? _commander;
    public AzureDataTableSession(IConfiguration configuration, SignalRService signalR, IHubCommander commander)
    {
        _configuration = configuration;
        _signalRService = signalR;
        _commander = commander;
        _ = InitializeTables().ConfigureAwait(false);
    }
    public async Task<Task> InitializeTables()
    {

        _tableServiceClient = new(_configuration?.GetRequiredSection("AzureStorage").Value!);
        _tableClient = _tableServiceClient?.GetTableClient("SessionTable");
        await _tableClient!.CreateIfNotExistsAsync().ConfigureAwait(false);
        return Task.CompletedTask;

    }

    internal Task<Session?> SetPrivateSession(string userName, string partnerName)
    {
        var sessionExists = _tableClient?.GetEntityIfExists<SQL_SESSION_ENTITY>(userName, partnerName);

        if (sessionExists!.HasValue)
        {
            return Task.FromResult<Session?>(sessionExists.Value!.ToSession());
        }

        return Task.FromResult<Session?>(null);

    }

    public async Task<bool> IsUserActive(string roomNameUserName)
    {

        var answer = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.PartitionKey == roomNameUserName).AsPages().ToBlockingEnumerable();

        if (answer!.Count() > 0)
        {
            return await Task.FromResult(true);
        }

        return await Task.FromResult(false);

    }
    public async Task<bool> DeleteUserSession(string userName, string partnerName)
    {

        var answer = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.PartitionKey == userName && x.RowKey == partnerName);

        var batchIt = new List<TableTransactionAction>();

        await foreach (var item in answer!)
        {
            batchIt.Add(new TableTransactionAction(TableTransactionActionType.Delete, item));
        }

        var response = await _tableClient!.SubmitTransactionAsync(batchIt).ConfigureAwait(false);

        if ((response.GetRawResponse() as Azure.Response).Status == 202)
        {
            return true;
        }

        return false;

    }
    public async Task<Session> GetOrCreateSessionAsync(string userName, string partnerName)
    {
        userName = userName.ToLower();
        partnerName = partnerName.ToLower();

        // Check if a private session exists between userName and partnerName
        var isJointRoom = await SetPrivateSession(userName, partnerName);

        if (isJointRoom is null)
        {
            // Check if a private session exists between partnerName and userName
            var isPartnerJoint = await SetPrivateSession(partnerName, userName);

            if (isPartnerJoint is not null)
            {
                // Create a listing for userName if partnerName already has a session
                await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, new Session(isPartnerJoint.SessionId)));
                return isPartnerJoint;
            }
            else
            {
                // Validate if both userName and partnerName are authenticated users
                var userNameIsAuth = _commander?.MsalCurrentUsers!.FirstOrDefault(x => x.UserPrincipalName == userName);
                var partnerNameIsAuth = _commander?.MsalCurrentUsers!.FirstOrDefault(y => y.UserPrincipalName == partnerName);

                if (userNameIsAuth is not null && partnerNameIsAuth is not null)
                {
                    // Create a new session for both users
                    var session = new Session(Guid.NewGuid().ToString());
                    await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, session));
                    await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(partnerName, userName, session));
                    return session;
                }
            }
        }

        // Check if a session exists regardless of who created it
        var sql = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.RowKey == partnerName);

        if (sql?.ToBlockingEnumerable().Any() == true)
        {
            var sessionExists = _tableClient?.GetEntityIfExists<SQL_SESSION_ENTITY>(userName, partnerName);

            if (sessionExists!.HasValue)
            {
                return sessionExists.Value!.ToSession();
            }
            else
            {
                var sessionExistsByPartner = _tableClient?.GetEntityIfExists<SQL_SESSION_ENTITY>(partnerName, userName);
                if (sessionExistsByPartner!.HasValue)
                {
                    return sessionExistsByPartner.Value!.ToSession();
                }
                else
                {
                    var existing = sql.ToBlockingEnumerable().FirstOrDefault();
                    if (existing is not null)
                    {
                        await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, existing.ToSession()));
                    }
                    return existing!.ToSession();
                }
            }
        }
        else
        {
            // Create a new session if no existing session is found
            var newSession = new Session(Guid.NewGuid().ToString());
            await _tableClient!.AddEntityAsync(new SQL_SESSION_ENTITY(userName, partnerName, newSession));
            return newSession;
        }
    }

    public async Task<List<SQL_SESSION_ENTITY>> GetSessionBySessionId(string sessionId)
    {

        var answer = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(x => x.SessionId == sessionId).AsPages();

        var sessions = new List<SQL_SESSION_ENTITY>();

        await foreach (var entity in answer!)
        {
            sessions.Add(entity.Values.FirstOrDefault()!);
        }

        return sessions;

    }
    public async Task<KeyValuePair<string, Session>[]> GetLatestSessionsAsync(string userName)
    {

        var result = _tableClient?.QueryAsync<SQL_SESSION_ENTITY>(s => s.PartitionKey == userName).ConfigureAwait(false);

        var sessions = new SortedDictionary<string, Session>();

        await foreach (var entity in result!)
        {
            sessions.Add(entity.RowKey, entity.ToSession());
        }

        return sessions.ToArray();
    }


}

