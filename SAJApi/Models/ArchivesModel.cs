using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SAJApi.Models
{
    public class AttachmentGroupModel
    {
        public string groupName { get; set; }
        public IEnumerable<AttachmentInfoModel> list { get; set; }
    }
    public class AttachInfoGroupModel
    {
        [JsonProperty(PropertyName = "groupId")]
        public int attachmentGroupId { get; set; }

        public string groupName { get; set; }
    }
    public class AttachmentInfoDataEntryModel
    {
        public AttachmentInfoModel attachInfo { get; set; }

        public bool isNew { get; set; }

        public string token { get; set; }
    }
    public class AttachmentInfoModel
    {
        public long attachmentHeaderId { get; set; }

        public long attachmentId { get; set; }

        [JsonProperty(PropertyName = "ext")]
        public string attachmentExtension { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string attachmentTitle { get; set; }

        [JsonProperty(PropertyName = "desc")]
        public string attachmentDescription { get; set; }

        [JsonProperty(PropertyName = "groupId")]
        public int? attachmentGroupId { get; set; }

        public string groupName { get; set; }

        public bool isTemplate { get; set; }

        public bool isGlobal { get; set; }

        public string attachData { get; set; }
    }
}