using AuthChannel.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Graph;
using Microsoft.Identity.Web;


namespace AuthChannel.Controllers
{
    [Authorize]
    [ApiController]
    public class NegotiateController : ControllerBase
    {
        private const string EnableDetailedErrors = "EnableDetailedErrors";
        private readonly ServiceHubContext? _messageHubContext;
        private readonly ServiceHubContext? _chatHubContext;
        private readonly bool _enableDetailedErrors;
        private readonly GraphServiceClient? _graphServiceClient;

        public NegotiateController(IHubContextStore store, IConfiguration configuration, GraphServiceClient graphServiceClient)
        {
            _messageHubContext = store.MessageHubContext;
            _enableDetailedErrors = configuration.GetValue(EnableDetailedErrors, false);
            _graphServiceClient = graphServiceClient;
        }

        [HttpPost("message/negotiate")]
        public Task<ActionResult> MessageHubNegotiate()
        {
            var user = User.GetDisplayName();
            return NegotiateBase(user!, _messageHubContext!);
        }

        [HttpGet("users/all")]
        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        public async Task<ActionResult> GetallMsalUsers()
        {
            try
            {
                var users = await _graphServiceClient!.Users.Request().GetAsync();
                return new JsonResult(users.Select(x => x.UserPrincipalName).ToArray());
            }
            catch (Exception)
            {
                return new BadRequestResult();
            }


        }
        
        private async Task<ActionResult> NegotiateBase(string user, ServiceHubContext serviceHubContext)
        {
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("User ID is null or empty.");
            }

            NegotiationResponse? negotiateResponse = await serviceHubContext.NegotiateAsync(new()
            {
                UserId = user,
                EnableDetailedErrors = _enableDetailedErrors
            });

            return new JsonResult(new Dictionary<string, string>()
            {
                { "url", negotiateResponse.Url! },
                { "accessToken", negotiateResponse.AccessToken! }
            });
        }
    }
}