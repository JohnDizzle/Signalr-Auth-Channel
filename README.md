# Authchannel Message Board 🗣️

**Auth Channel** is a modern, open-source Blazor chat/message board application designed for secure, enterprise-ready collaboration. It leverages Azure services for authentication, real-time messaging, and file sharing.

✨ ## Features

 **Single-Tenant Authorization**  
  Uses [MSAL](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) for secure, single-tenant authentication with Azure Active Directory.

✔️ **Real-Time Messaging**  
  Built on [Azure SignalR Service](https://docs.microsoft.com/en-us/azure/azure-signalr/signalr-overview) for scalable, low-latency chat.

✔️ **Room Ownership**  
  Users can create chat rooms and become their owners. Room owners manage the existance of there rooms.

✔️ **Private Messaging**  
  All messages are authorized per user/session. Private chats are supported with strict access control.

✔️ **File Sharing with Expiry**  
  Upload files to Azure Storage. Shared files are accessible via secure links that expire after 1 hour.

✔️ **Dependency Injection & Azure Common**  
  Built with .NET 9 and C# 13, using best practices for dependency injection and Azure SDKs.

✔️ **Session Management**  
  User sessions and message history are stored in Azure Table Storage for reliability and scalability.

🛞 ## Architecture 🛞

📃 **Frontend:** Blazor (.NET 9), Signalr Client
📃 **Backend:** ASP.NET Core, SignalR Management & Hub, Azure SDKs
📃 **Authentication:** Azure AD (MSAL, single-tenant)
📃 **Storage:** Azure Table Storage (sessions/messages), Azure Blob Storage (file uploads) - ** In Memory Storage is available for debugging **
📃 **Real-Time:** Azure SignalR Service

🔡 ## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Azure Subscription (for SignalR, Storage, and AD)
- [Node.js](https://nodejs.org/) (for frontend tooling, if needed)

### Configuration

1. **Azure AD App Registration**  
   Register a single-tenant app in Azure AD.  
   Update `appsettings.json` with your Azure AD and SignalR/Storage connection details.

2. **Azure Resources**  
   - Create an Azure SignalR Service instance.
   - Create an Azure Storage Account (Table + Blob).
   - Configure CORS and access policies as needed.

3. **Local Development**  
   - Run the application using `dotnet run` or your preferred IDE.
	 - Ensure you have the necessary Azure resources set up and configured in `appsettings.json`

4. **Environment Variables**  
   - `Azure:SignalR:ConnectionString`
   - `AzureAD:ClientSecret`
   - `AzureStorage`
   - `ConnectionStrings:AzureStorage`
   - `KeyVaultName`
   - *local development* you may add these to the secrets.json use the cli.
      1. dotnet user-secrets init .
      2. dotnet user-secrets set "Section:Key" "value".
      
		 **suggestion: use key vault for sensitive data**
  	
   - `Azure` - configuration in appsettings.json 
      ```
      {"AzureAd": {
	    "Instance": "https://login.microsoftonline.com/",
	    "Domain": "{your EntraId domain}",
	    "TenantId": "{you TenantId}",
	    "ClientId": "{your App Registration ClientId}",
	    "CallbackPath": "/signin-oidc",
	    "SkipUnrecognizedRequests": true,
	    "TokenDecryptionCertificates": [
	      {
	        "SourceType": "KeyVault",
	        "KeyVaultUrl": "{your keyvalut url}",
	        "KeyVaultCertificateName": "{you certification name}"
	      }
	    ],
	    "SendX5C": "true",
	    "Scopes": "[ User.Read, User.ReadWrite, email, profile, offline_access, openid ]",
	    "identityProviders": {
	      "azureActiveDirectory": {
	        "enabled": true,
	        "login": {
	          "loginParameters": [
	            "response_type=code id_token",
	            "scope=openid offline_access profile https://graph.microsoft.com/User.Read"
	          ]
	        }
	      }
	    }
	  },
	  "Logging": {
	    "LogLevel": {
	      "Default": "Information",
	      "Microsoft.AspNetCore": "Warning"
	    }
	  },
	  "AllowedHosts": "*",
	  "MicrosoftGraph": {
	    "BaseUrl": "https://graph.microsoft.com/v1.0",
	    "Scopes": "User.Read User.ReadWrite email profile offline_access openid"
	  },
	  "Azure": {
	    "SignalR": {
	      "Enabled": "true"
	    }
	  }
	}```
  
	
### Usage

- **Sign in** with your Azure AD account.
- **Create or join rooms**. Room creators become owners.
- **Send messages** in public or private rooms.
- **Share files** (valid for 1 hour).
- **Manage sessions** and view message history.

## Security

- All endpoints require authentication.
- Private messages and files are only accessible to authorized users.
- File links expire after 1 hour for added security.

## Contributing

Contributions are welcome! Please open issues or submit pull requests.

## License

[MIT License](/LICENSE.txt)

## Message Board
[Main View ](/MessageBoard.png)
---

**Note:** This project is intended for single-tenant (enterprise) use. For multi-tenant scenarios, additional configuration is required.
