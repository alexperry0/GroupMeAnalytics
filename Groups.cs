using System.Collections.Generic;

namespace GroupMeAnalytics {

    public class GroupMember
    {
        public string user_id { get; set; }
        public string nickname { get; set; }
        public string image_url { get; set; }
        public string id { get; set; }
        public bool muted { get; set; }
        public bool autokicked { get; set; }
        public List<string> roles { get; set; }
        public string name { get; set; }
    }

    public class GroupPreview
    {
        public string nickname { get; set; }
        public string text { get; set; }
        public string image_url { get; set; }
        public List<object> attachments { get; set; }
    }

    public class GroupMessagesMetaData
    {
        public int count { get; set; }
        public string last_message_id { get; set; }
        public int last_message_created_at { get; set; }
        public GroupPreview preview { get; set; }
    }

    public class GroupResponse
    {
        public string id { get; set; }
        public string group_id { get; set; }
        public string name { get; set; }
        public string phone_number { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string image_url { get; set; }
        public string creator_user_id { get; set; }
        public int created_at { get; set; }
        public int updated_at { get; set; }
        public bool office_mode { get; set; }
        public object share_url { get; set; }
        public object share_qr_code_url { get; set; }
        public List<GroupMember> members { get; set; }
        public GroupMessagesMetaData messages { get; set; }
        public int max_members { get; set; }
    }

    public class GroupMetaData
    {
        public int code { get; set; }
    }

    public class Groups
    {
        public List<GroupResponse> response { get; set; }
        public GroupMetaData meta { get; set; }
    }
}
