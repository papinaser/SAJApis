using SAJApi.Custom;
using SAJApi.Models;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class ArchivesController : ApiController
  {
    [Route("api/Archives/GetGroupsForDataEntry/{attachmentHeaderId}/{token}")]
    [HttpGet]
    public SimpleResult GetGroupsForDataEntry(long attachmentHeaderId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<AttachInfoGroupModel> groupsForDataEntry = ArchivesManager.GetGroupsForDataEntry(attachmentHeaderId);
        return new SimpleResult()
        {
          result = "200",
          message = (object) groupsForDataEntry
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.29",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/Archives/GetDetailRowAttachs/{tblId}/{roykardId}/{mrId}/{token}")]
    [HttpGet]
    public SimpleResult GetDetailRowAttachs(
      long tblId,
      long roykardId,
      long mrId,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        long logAttachsHeaderId = ArchivesManager.GetDetailsLogAttachsHeaderId(tblId, roykardId, mrId);
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(ArchivesManager.GetAttachmentsByHeadeId(logAttachsHeaderId));
        return new SimpleResult()
        {
          result = "200",
          message = (object) new DataRowAttachmentsModel()
          {
            headerId = logAttachsHeaderId,
            list = attachmentGroupModels
          }
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.32",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/Archives/GetMasterLogAttachs/{masterLogId}/{token}")]
    [HttpGet]
    public SimpleResult GetMasterLogAttachs(long masterLogId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        long headerId;
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(ArchivesManager.GetMasterLogAttachs(masterLogId, out headerId));
        return new SimpleResult()
        {
          result = "200",
          message = (object) new DataRowAttachmentsModel()
          {
            headerId = headerId,
            list = attachmentGroupModels
          }
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.31",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/Archives/GetByHeaderId/{attachmentHeaderId}/{token}")]
    [HttpGet]
    public SimpleResult GetByHeaderId(long attachmentHeaderId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(ArchivesManager.GetAttachmentsByHeadeId(attachmentHeaderId));
        return new SimpleResult()
        {
          result = "200",
          message = (object) attachmentGroupModels
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.27",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/Archives/GetUrlForDownload/{attachmentId}/{token}")]
    [HttpGet]
    public SimpleResult GetUrlForDownload(long attachmentId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        string urlForDownload = ArchivesManager.GetUrlForDownload(attachmentId, token);
        return new SimpleResult()
        {
          result = "200",
          message = (object) urlForDownload
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.28",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Post([FromBody] AttachmentInfoDataEntryModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, this.Request.GetClientIpAddress());
        long num = ArchivesManager.SaveDataEntry(model);
        return new SimpleResult()
        {
          result = "200",
          message = (object) num
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.30",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Delete(long attachId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        ArchivesManager.DeleteAttachment(attachId);
        return new SimpleResult()
        {
          result = "200",
          message = (object) "حذف با موفقیت انجام شد"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.30",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Put(int attachId, [FromBody] AttachmentInfoDataEntryModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, this.Request.GetClientIpAddress());
        ArchivesManager.SaveDataEntry(model);
        return new SimpleResult()
        {
          result = "200",
          message = (object) "بروزرسانی با موفقیت انجام شد"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.30",
          message = (object) ex.Message
        };
      }
    }

    public ArchivesController()
    {
      
    }
  }
}
