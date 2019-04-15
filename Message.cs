using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroupMeAnalytics
{
    public class Message
    {
        public List<object> attachments { get; set; }
        public string avatar_url { get; set; }
        public int created_at { get; set; }
        public List<object> favorited_by { get; set; }
        public string group_id { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string sender_id { get; set; }
        public string sender_type { get; set; }
        public string source_guid { get; set; }
        public bool system { get; set; }
        public string text { get; set; }
        public string user_id { get; set; }
        public string platform { get; set; }
    }

    public class MessageResponse
    {
        public int count { get; set; }
        public List<Message> messages { get; set; }
    }

    public class MessageMetaData
    {
        public int code { get; set; }
    }

    public class Messages
    {
        public MessageResponse response { get; set; }
        public MessageMetaData meta { get; set; }
    }
}
