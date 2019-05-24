using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class RequestKalaController : ApiController
  {
    [Route("api/RequestKala/GetAllByUserName/{token}")]
    [HttpGet]
    public SimpleResult GetAllByUserName(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        AgGridModel userReqquests = RequestKalaManager.GetUserReqquests(UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult()
        {
          result = "200",
          message = (object) userReqquests
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetAttachs/{requestKalaId}/{token}")]
    [HttpGet]
    public SimpleResult GetAttachs(int requestKalaId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        long headerId;
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = ArchivesManager.GroupAttachmentsList(RequestKalaManager.GetAttachments(requestKalaId, out headerId));
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
          result = "500.35",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetOrderDescs/{token}")]
    [HttpGet]
    public SimpleResult GetOrderDescs(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        var orderDescs = RequestKalaManager.GetOrderDescs().ToList();
        return new SimpleResult()
        {
          result = "200",
          message = orderDescs
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetUnits/{token}")]
    [HttpGet]
    public SimpleResult GetUnits(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        List<KeyValueModel> units = RequestKalaManager.GetUnits();
        return new SimpleResult()
        {
          result = "200",
          message = (object) units
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.34",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetStocks/{token}")]
    [HttpGet]
    public SimpleResult GetStocks(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<KeyValueModel> stocks = RequestKalaManager.GetStocks();
        return new SimpleResult()
        {
          result = "200",
          message = (object) stocks
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.34",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetTarhs/{token}")]
    [HttpGet]
    public SimpleResult GetTarhs(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<KeyValueModel> tarhs = RequestKalaManager.GetTarhs();
        return new SimpleResult()
        {
          result = "200",
          message = (object) tarhs
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.34",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetProjects/{token}")]
    [HttpGet]
    public SimpleResult GetProjects(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<KeyValueModel> projects = RequestKalaManager.GetProjects();
        return new SimpleResult()
        {
          result = "200",
          message = (object) projects
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.34",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetSubProjects/{token}")]
    [HttpGet]
    public SimpleResult GetSubProjects(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<KeyValueModel> subProjects = RequestKalaManager.GetSubProjects();
        return new SimpleResult()
        {
          result = "200",
          message = (object) subProjects
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.34",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetHeaderByRequestKalaId/{requestId}/{token}")]
    [HttpGet]
    public SimpleResult GetHeaderByRequestKalaId(int requestId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        RequestKalaModel headerByRequestKalaId = RequestKalaManager.GetHeaderByRequestKalaId(requestId);
        return new SimpleResult()
        {
          result = "200",
          message = (object) headerByRequestKalaId
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/RequestKala/GetItemsByRequestKalaId/{requestId}/{token}")]
    [HttpGet]
    public SimpleResult GetItemsByRequestKalaId(int requestId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        AgGridModel itemsByRequestKalaId = RequestKalaManager.GetItemsByRequestKalaId(requestId);
        return new SimpleResult()
        {
          result = "200",
          message = (object) itemsByRequestKalaId
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Post([FromBody] SaveRequestKalaModel saveModel)
    {
      try
      {
        Utils.AutoLoginWithToken(saveModel.username, this.Request.GetClientIpAddress());
        string str = RequestKalaManager.SaveRequest(saveModel, false);
        return new SimpleResult()
        {
          result = "200",
          message = (object) str
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Put(int id, [FromBody] SaveRequestKalaModel saveModel)
    {
      try
      {
        Utils.AutoLoginWithToken(saveModel.username, this.Request.GetClientIpAddress());
        string str = RequestKalaManager.SaveRequest(saveModel, true);
        return new SimpleResult()
        {
          result = "200",
          message = (object) str
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Delete(int id, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        object obj = RequestKalaManager.DeleteById(id);
        return new SimpleResult()
        {
          result = "200",
          message = obj
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.33",
          message = (object) ex.Message
        };
      }
    }

    public RequestKalaController()
    {
      
    }
  }
}
