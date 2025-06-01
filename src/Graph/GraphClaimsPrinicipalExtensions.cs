﻿using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Security.Claims;
//https://github.com/aspnet-snippets-sample/tree/main/SnippetsApp
namespace AuthChannel.Graph
{
    public static class GraphClaimTypes
    {
        public const string DisplayName = "graph_name";
        public const string Email = "graph_email";
        public const string Photo = "graph_photo";
        public const string TimeZone = "graph_timezone";
        public const string TimeFormat = "graph_timeformat";
        public const string DateFormat = "graph_dateformat";
    }

    // Helper methods to access Graph user data stored in
    // the claims principal
    public static class GraphClaimsPrincipalExtensions
    {
        public static string? GetUserGraphDisplayName(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.DisplayName);
        }

        public static string? GetUserGraphEmail(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.Email);
        }

        public static string? GetUserGraphPhoto(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.Photo);
        }

        public static string? GetUserGraphTimeZone(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.TimeZone);
        }

        public static string? GetUserGraphTimeFormat(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.TimeFormat);
        }

        public static string? GetUserGraphDateFormat(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(GraphClaimTypes.DateFormat);
        }

        public static bool IsPersonalAccount(this ClaimsPrincipal claimsPrincipal)
        {
            // GetDomainHint returns "consumers" for personal Microsoft accounts
            return claimsPrincipal.GetDomainHint() == "consumers";
        }

        public static void AddUserGraphInfo(this ClaimsPrincipal claimsPrincipal, User user)
        {
            var identity = claimsPrincipal.Identity as ClaimsIdentity;

            identity?.AddClaim(
                new Claim(GraphClaimTypes.DisplayName, user.DisplayName));
            identity?.AddClaim(
                new Claim(GraphClaimTypes.Email,
                    // Non-personal accounts store email in the Mail property
                    // They can have a user principal name but no email address
                    // Only personal accounts should assume UPN = email
                    claimsPrincipal.IsPersonalAccount() ? user.UserPrincipalName : user.Mail! ?? user.UserPrincipalName));
            //identity.AddClaim(
            //    new Claim(GraphClaimTypes.TimeZone,
            //        user.MailboxSettings.TimeZone ?? "UTC"));
            //identity.AddClaim(
            //    new Claim(GraphClaimTypes.TimeFormat, user.MailboxSettings.TimeFormat ?? "h:mm tt"));
            //identity.AddClaim(
            //    new Claim(GraphClaimTypes.DateFormat, user.MailboxSettings.DateFormat ?? "M/d/yyyy"));
        }

        public static void AddUserGraphPhoto(this ClaimsPrincipal claimsPrincipal, Stream photoStream)
        {
            var identity = claimsPrincipal.Identity as ClaimsIdentity;

            if (photoStream == null)
            {
                // Add the default profile photo
                identity?.AddClaim(
                    new Claim(GraphClaimTypes.Photo, "/img/no-profile-photo.png"));
                return;
            }

            // Copy the photo stream to a memory stream
            // to get the bytes out of it
            var memoryStream = new MemoryStream();
            photoStream.CopyTo(memoryStream);
            var photoBytes = memoryStream.ToArray();

            // Generate a date URI for the photo
            var photoUrl = $"data:image/png;base64,{Convert.ToBase64String(photoBytes)}";

            identity?.AddClaim(
                new Claim(GraphClaimTypes.Photo, photoUrl));
        }
    }
}
