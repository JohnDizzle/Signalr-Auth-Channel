using AuthChannel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Diagnostics;

namespace AuthChannel.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        readonly ITokenAcquisition _tokenAcquisition;
        public HomeController(IConfiguration configuration, ITokenAcquisition tokenAcquisition, ILogger<HomeController> logger, GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
            _configuration = configuration;
            _tokenAcquisition = tokenAcquisition;

        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _graphServiceClient.Me.Request().GetAsync();
                ViewData["GraphApiResult"] = Json(user!);
                return View();
            }
            catch (Exception)
            {
                throw;
            }
            
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult ErrorWithMessage(string message, string debug)
        {
            return View("Index");
        }
    }
}
