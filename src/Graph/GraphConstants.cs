﻿namespace AuthChannel.Graph
{
    //https://github.com/aspnet-snippets-sample/tree/main/SnippetsApp
    public static class GraphConstants
    {
        // Defines the permission scopes used by the app
        public readonly static string[] DefaultScopes =
        {
            UserRead,
            UserReadWrite,
            "email",
            "profile",
            "offline_access",
            "openid"

        };
        // User.Read User.ReadWrite email profile offline_access openid
        // Default page size for collections
        public const int PageSize = 25;

        // User
        public const string UserRead = "User.Read";
        public const string UserReadBasicAll = "User.ReadBasic.All";
        public const string UserReadAll = "User.Read.All";
        public const string UserReadWrite = "User.ReadWrite";
        public const string UserReadWriteAll = "User.ReadWrite.All";

        // Group
        public const string GroupReadWriteAll = "Group.ReadWrite.All";

        // Teams
        public const string ChannelCreate = "Channel.Create";
        public const string ChannelMessageSend = "ChannelMessage.Send";
        public const string ChannelSettingsReadWriteAll = "ChannelSettings.ReadWrite.All";
        public const string TeamsAppInstallationReadWriteForTeam = "TeamsAppInstallation.ReadWriteForTeam";
        public const string TeamCreate = "Team.Create";
        public const string TeamSettingsReadWriteAll = "TeamSettings.ReadWrite.All";

        // Mailbox settings
        public const string MailboxSettingsRead = "MailboxSettings.Read";

        // Mail
        public const string MailRead = "Mail.Read";
        public const string MailReadWrite = "Mail.ReadWrite";
        public const string MailSend = "Mail.Send";

        // Calendar
        public const string CalendarReadWrite = "Calendars.ReadWrite";

        // Files
        public const string FilesReadWrite = "Files.ReadWrite";
        public const string FilesReadWriteAll = "Files.ReadWrite.All";

        // Errors
        public const string ItemNotFound = "ErrorItemNotFound";
        public const string RequestDenied = "Authorization_RequestDenied";
        public const string RequestResourceNotFound = "Request_ResourceNotFound";
        public const string ResourceNotFound = "ResourceNotFound";
    }
}
