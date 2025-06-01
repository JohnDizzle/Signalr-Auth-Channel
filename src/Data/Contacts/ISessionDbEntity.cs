namespace AuthChannel.Data.Contacts
{
    public interface ISessionDbEntity
    {
        public string? ConnectionId { get; set; }
        public string? PartnerName { get; set; }
        public string? SessionId { get; set; }
        public DateTimeOffset DateTime { get; set; }

    }
}
