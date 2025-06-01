using AuthChannel.Data.Contacts;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
#nullable disable
namespace AuthChannel.Data.Models
{
    [Table("SessionDb")]
    [PrimaryKey(nameof(ConnectionId), nameof(PartnerName))]
    public class SessionDbEntity : ISessionDbEntity
    {
        public string ConnectionId { get; set; }
        public string PartnerName { get; set; }
        public string SessionId { get; set; }
        public DateTimeOffset DateTime { get; set; }

        public SessionDbEntity(string _connectionId, string _partnerName, string _sessionId, DateTimeOffset _datetime = new DateTimeOffset())
        {
            ConnectionId = _connectionId;
            PartnerName = _partnerName;
            SessionId = _sessionId;
            DateTime = _datetime;
        }
        public SessionDbEntity() { }
    }
}
