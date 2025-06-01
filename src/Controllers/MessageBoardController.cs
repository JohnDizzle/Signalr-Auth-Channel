using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using AuthChannel.Services;
using AuthChannel.Services.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;

namespace AuthChannel.Controllers
{
    [Authorize]
    [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
    public class MessageBoardController : Controller
    {
        #region Initiliaze;

        private readonly IHubContextStore? _contextStore;
        private readonly GraphServiceClient? _graphServiceClient;
        private readonly IHubCommander? _commander;
        private readonly ITokenAcquisition? _tokenAcquisition;
        private readonly IConfiguration? _configuration;
        private readonly IWebHostEnvironment _hostEnvironment;
        public MessageBoardController(IHubContextStore hubContextStore, GraphServiceClient graphServiceClient, IHubCommander commander, ITokenAcquisition tokenAcquisition, IConfiguration configuration, IWebHostEnvironment env)
        {
            _contextStore = hubContextStore;
            _graphServiceClient = graphServiceClient;
            _commander = commander;
            _tokenAcquisition = tokenAcquisition;
            _configuration = configuration;
            _hostEnvironment = env;
        }
        #endregion;

        public async Task<IActionResult> Index()
        {
            try
            {
                //https://stackoverflow.com/questions/72922593/asp-net-core-openidconnect-message-state-is-null-or-empty
                //https://learn.microsoft.com/en-us/answers/questions/1465232/openidconnectauthenticationhandler-message-state-i
                _ = await _tokenAcquisition!.GetAuthenticationResultForUserAsync(new List<string> { "user.Read", "email", "profile", "offline_access", "openid" }, "common", _configuration?.GetRequiredSection("AzureAd:TenantId").Value!, User, new TokenAcquisitionOptions() { ForceRefresh = true });
                var users = await _graphServiceClient!.Users.Request().GetAsync();
                _commander!.MsalCurrentUsers = users.ToList();
                return View();
            }
            catch (Exception e)
            {
                foreach (var cookie in Request.Cookies)
                { Response.Cookies.Delete(cookie.Key); }
                Console.Write(e.Message!.ToString());
            };

            return Redirect("~/");

        }
    }
}
