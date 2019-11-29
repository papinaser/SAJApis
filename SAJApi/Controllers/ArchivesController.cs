using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using SAJApi.Custom;
using SAJApi.Models;

namespace SAJApi.Controllers
{
    [EnableCors("http://192.168.1.8:888,http://localhost:8100,http://91.98.153.26:3000,http://localhost:3000,http://172.20.0.245:888", "*", "*")]
    public class ArchivesController : ApiController
  {
    [Route("api/Archives/GetGroupsForDataEntry/{attachmentHeaderId}/{token}")]
    [HttpGet]
    public SimpleResult GetGroupsForDataEntry(long attachmentHeaderId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        IEnumerable<AttachInfoGroupModel> groupsForDataEntry = ArchivesManager.GetGroupsForDataEntry(attachmentHeaderId);
        return new SimpleResult
        {
          result = "200",
          message = groupsForDataEntry
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.29",
          message = ex.Message
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
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        long logAttachsHeaderId = ArchivesManager.GetDetailsLogAttachsHeaderId(tblId, roykardId, mrId);
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(ArchivesManager.GetAttachmentsByHeadeId(logAttachsHeaderId));
        return new SimpleResult
        {
          result = "200",
          message = new DataRowAttachmentsModel
          {
              headerId = logAttachsHeaderId,
              list = attachmentGroupModels
          }
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.32",
          message = ex.Message
        };
      }
    }

    [Route("api/Archives/GetMasterLogAttachs/{masterLogId}/{token}")]
    [HttpGet]
    public SimpleResult GetMasterLogAttachs(long masterLogId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        long headerId;
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(ArchivesManager.GetMasterLogAttachs(masterLogId, out headerId));
        return new SimpleResult
        {
          result = "200",
          message = new DataRowAttachmentsModel
          {
              headerId = headerId,
              list = attachmentGroupModels
          }
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.31",
          message = ex.Message
        };
      }
    }

    [Route("api/Archives/GetByHeaderId/{attachmentHeaderId}/{token}")]
    [HttpGet]
    public SimpleResult GetByHeaderId(long attachmentHeaderId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(ArchivesManager.GetAttachmentsByHeadeId(attachmentHeaderId));
        return new SimpleResult
        {
          result = "200",
          message = attachmentGroupModels
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.27",
          message = ex.Message
        };
      }
    }

    [Route("api/Archives/GetUrlForDownload/{attachmentId}/{token}")]
    [HttpGet]
    public SimpleResult GetUrlForDownload(long attachmentId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        string urlForDownload = ArchivesManager.GetUrlForDownload(attachmentId, token);
        return new SimpleResult
        {
          result = "200",
          message = urlForDownload
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.28",
          message = ex.Message
        };
      }
    }

    public SimpleResult Post([FromBody] AttachmentInfoDataEntryModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, Request.GetClientIpAddress());
        long num = ArchivesManager.SaveDataEntry(model);
        return new SimpleResult
        {
          result = "200",
          message = num
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.30",
          message = ex.Message
        };
      }
    }

    public SimpleResult Delete(long attachId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        ArchivesManager.DeleteAttachment(attachId);
        return new SimpleResult
        {
          result = "200",
          message = "حذف با موفقیت انجام شد"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.30",
          message = ex.Message
        };
      }
    }

    public SimpleResult Put(int attachId, [FromBody] AttachmentInfoDataEntryModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, Request.GetClientIpAddress());
        ArchivesManager.SaveDataEntry(model);
        return new SimpleResult
        {
          result = "200",
          message = "بروزرسانی با موفقیت انجام شد"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.30",
          message = ex.Message
        };
      }
    }
  }
}
