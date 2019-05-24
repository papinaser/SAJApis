using System.Collections.Generic;

namespace SAJApi.Models
{
  public class DataRowAttachmentsModel
  {
    public long headerId { get; set; }

    public IEnumerable<AttachmentGroupModel> list { get; set; }
  }
}
