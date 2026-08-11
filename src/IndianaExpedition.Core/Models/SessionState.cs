using System.Runtime.Serialization;
using IndianaExpedition.Core.Constants;

namespace IndianaExpedition.Core.Models
{
    [DataContract]
    public sealed class SessionState
    {
        [DataMember(Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 1)]
        public string LastActiveUrl { get; set; }

        public static SessionState CreateDefault()
        {
            return new SessionState { SchemaVersion = BrowserDefaults.DataSchemaVersion };
        }

        public SessionState Clone()
        {
            return new SessionState
            {
                SchemaVersion = SchemaVersion,
                LastActiveUrl = LastActiveUrl
            };
        }
    }
}
