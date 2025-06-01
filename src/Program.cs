using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AuthChannel.Graph;
using AuthChannel.Services;
using AuthChannel.Services.Hubs;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net;
using System.Net.Http.Headers;
using AuthChannel.Data.MessageHandler.AzureTableMessageStorage;
using AuthChannel.Data.SessionHandler.AzureTableSessionStorage;
using AuthChannel.Data.Contacts;



var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ') ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ');

if (builder.Environment.IsProduction())
{
    var vault = builder.Configuration.GetSection("KeyVaultName").ToString();
    var secretClient = new SecretClient(new(vault), new DefaultAzureCredential());
    builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

}

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);

        options.Prompt = "select_account";

        options.Events.OnTokenValidated = async context =>
        {
            var tokenAcquisition = context.HttpContext.RequestServices
                .GetRequiredService<ITokenAcquisition>();

            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(async (request) =>
                {
                    var token = await tokenAcquisition
                        .GetAccessTokenForUserAsync(GraphConstants.DefaultScopes, user: context.Principal);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                })
            );
            try
            {
                var user = await graphClient.Me.Request()
                .Select(u => new
                {
                    u.DisplayName,
                    u.Mail,
                    u.UserPrincipalName,
                    //u.MailboxSettings
                })
                .GetAsync();
                // Get user information from Graph

                context!.Principal!.AddUserGraphInfo(user);

                if (context!.Principal!.IsPersonalAccount())
                {
                    // Personal accounts do not support getting their
                    // photo via Graph
                    // Support is there in the beta API
                    context!.Principal!.AddUserGraphPhoto(null);
                    return;
                }

            }
            catch (Exception e)
            {
                throw;
            }

            // Get the user's photo
            // If the user doesn't have a photo, this throws
            try
            {
                var photo = await graphClient.Me
                    .Photos["48x48"]
                    .Content
                    .Request()
                    .GetAsync();

                context!.Principal!.AddUserGraphPhoto(photo);
            }
            catch (ServiceException ex)
            {
                if (ex.IsMatch("ErrorItemNotFound"))
                {
                    context.Principal!.AddUserGraphPhoto(null);
                }
                else
                {
                    throw;
                }
            }
        };

        options.Events.OnAuthenticationFailed = context =>
        {
            var error = WebUtility.UrlEncode(context.Exception.Message);
            context.Response
                .Redirect($"/Home/ErrorWithMessage?message=Authentication+error&debug={error}");
            context.HandleResponse();

            return Task.FromResult(0);
        };

        options.Events.OnRemoteFailure = context =>
        {
            if (context.Failure is OpenIdConnectProtocolException)
            {
                var error = WebUtility.UrlEncode(context.Failure.Message);
                context.Response
                    .Redirect($"/Home/ErrorWithMessage?message=Sign+in+error&debug={error}");
                context.HandleResponse();
            }

            return Task.FromResult(0);
        };
    })
                .EnableTokenAcquisitionToCallDownstreamApi(options =>
                {
                    builder.Configuration.Bind("AzureAd", options);
                }, GraphConstants.DefaultScopes)
                // Add a GraphServiceClient via dependency injection
                .AddMicrosoftGraph(options =>
                {
                    options.Scopes = string.Join(' ', GraphConstants.DefaultScopes);
                })
                // Use in-memory token cache
                // See https://github.com/AzureAD/microsoft-identity-web/wiki/token-cache-serialization
                .AddDistributedTokenCaches();


builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI();


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();
builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromMilliseconds(120000);
    options.KeepAliveInterval = TimeSpan.FromMilliseconds(840000);
});


builder.Services.AddSingleton<IMessageHandler, AzureDataTableMessages>();
builder.Services.AddSingleton<IAzureDataTableSessionStorage, AzureDataTableSession>();

builder.Services.AddSingleton<SignalRService>()
   .AddHostedService(sp =>
   {
       var service = sp.GetService<SignalRService>();
       if (service == null)
       {
           throw new InvalidOperationException("SignalRService is not properly registered.");
       }
       return service;
   })
   .AddSingleton<IHubContextStore>(sp =>
   {
       var service = sp.GetService<SignalRService>();
       if (service == null)
       {
           throw new InvalidOperationException("SignalRService is not properly registered.");
       }
       return service;
   });


builder.Services.AddSingleton<IHubCommander, HubCommander>();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseFileServer();
app.UseStaticFiles();
app.UseCookiePolicy(); //new 
app.UseAuthorization();
app.UseAuthentication();
app.UseSession();
app.UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapRazorPages();
    endpoints.MapHub<AzureChat>("/chat");
});

//index fall back 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// app.MapGet("/", (IConfiguration config) =>
//     string.Join(
//         Environment.NewLine,
//         "SecretName (Name in Key Vault: 'SecretName')",
//         @"Obtained from configuration with config[""SecretName""]",
//         $"Value: {config["SecretName"]}",
//         "",
//         "Section:SecretName (Name in Key Vault: 'Section--SecretName')",
//         @"Obtained from configuration with config[""Section:SecretName""]",
//         $"Value: {config["Section:SecretName"]}",
//         "",
//         "Section:SecretName (Name in Key Vault: 'Section--SecretName')",
//         @"Obtained from configuration with config.GetSection(""Section"")[""SecretName""]",
//         $"Value: {config.GetSection("Section")["SecretName"]}"));


app.Run();
