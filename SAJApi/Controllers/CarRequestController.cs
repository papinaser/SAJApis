using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Security;
using SepandAsa.Transportation.Light.Domain;

namespace SAJApi.Controllers
{
    [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://localhost:3000,http://172.20.0.245:888", "*", "*")]
    public class CarRequestController : ApiController
  {
    [Route("api/CarRequest/GetByRequestId/{requestId}/{token}")]
    [HttpGet]
    public SimpleResult GetByRequestId(int requestId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        var byRequestId =
            new CarRequestManager().GetAllCarRequestForMissionByPersonelNo(UserAccount.Instance.CurrentUser.EmployeeId);
        return new SimpleResult
        {
          result = "200",
          message = byRequestId
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.33",
          message = ex.Message
        };
      }
    }

    [Route("api/CarRequest/GetUserRequests/{token}")]
    [HttpGet]
    public SimpleResult GetUserRequests(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        AgGridModel userRequests = new CarRequestManager().GetAllCarRequestForMissionByPersonelNo(UserAccount.Instance.CurrentUser.EmployeeId);
        return new SimpleResult
        {
          message = userRequests,
          result = "200"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.10"
        };
      }
    }

    [Route("api/CarRequest/GetAttachs/{requestId}/{token}")]
    [HttpGet]
    public SimpleResult GetAttachs(int requestId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        long headerId=0;
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = null;
            //ArchivesManager.GroupAttachmentsList(new CarRequestManager().GetAttachments(requestId, out headerId));
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
          result = "500.35",
          message = ex.Message
        };
      }
    }

    [Route("api/CarRequest/GetCarTypes/{token}")]
    [HttpGet]
    public SimpleResult GetCarTypes(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        List<KeyValueModel> generalCarTypes = new CarRequestManager().GetGeneralCarTypes(UserAccount.Instance.CurrentUser.UserId);
        return new SimpleResult
        {
          message = generalCarTypes,
          result = "200"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.10"
        };
      }
    }

    [Route("api/CarRequest/GetLocations/{token}")]
    [HttpGet]
    public SimpleResult GetLocations(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        List<KeyValueModel> locationForMission = new CarRequestManager().GetAllLocationForMission();
        return new SimpleResult
        {
          message = locationForMission,
          result = "200"
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.13"
        };
      }
    }

    public SimpleResult Post([FromBody] CarRequestModel request)
    {
      try
      {
        using (CarRequestManager carRequestManager = new CarRequestManager())
        {
          Utils.AutoLoginWithToken(request.token, Request.GetClientIpAddress());
          CarRequestForMissionInfo table = carRequestManager.ConvertModelToTable(request);
          carRequestManager.AddRequestItems(table, request.Locations);
          carRequestManager.SaveRequest(UserAccount.Instance.CurrentUser.EmployeeId, table);
          return new SimpleResult
          {
            message = "درخواست شما با شماره 111 ذخیره شد",
            result = "200"
          };
        }
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.11"
        };
      }
    }

    public void Put(int id, [FromBody] string value)
    {
    }

    public void Delete(int id)
    {
    }
  }
}
