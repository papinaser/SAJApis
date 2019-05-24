using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Security;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class PurchaseSourceController : ApiController
  {
    [Route("api/PurchaseSource/GetByUser/{token}")]
    [HttpGet]
    public SimpleResult GetByUser(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        AgGridModel allPurchaseSource = TankhahManager.GetAllPurchaseSource(UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult()
        {
          result = "200",
          message = (object) allPurchaseSource
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.37",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/PurchaseSource/GetManbaTypes/{token}")]
    [HttpGet]
    public SimpleResult GetManbaTypes(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        List<KeyValueModel> manbaTypes = TankhahManager.GetManbaTypes(UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult()
        {
          result = "200",
          message = (object) manbaTypes
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.38",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/PurchaseSource/GetStates/{token}")]
    [HttpGet]
    public SimpleResult GetStates(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        List<KeyValueModel> states = TankhahManager.GetStates(UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult()
        {
          result = "200",
          message = (object) states
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.43",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/PurchaseSource/GetCities/{token}")]
    [HttpGet]
    public SimpleResult GetCities(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        IEnumerable<CityModel> cities = TankhahManager.GetCities(UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult()
        {
          result = "200",
          message = (object) cities
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.44",
          message = (object) ex.Message
        };
      }
    }

    [Route("api/PurchaseSource/GetForEdit/{manbaKharidId}/{token}")]
    [HttpGet]
    public SimpleResult GetForEdit(int manbaKharidId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        manbaKharidModel manbaForEdit = TankhahManager.GetManbaForEdit(manbaKharidId, UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult()
        {
          result = "200",
          message = (object) manbaForEdit
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        return new SimpleResult()
        {
          result = "500.39",
          message = (object) ex.Message
        };
      }
    }

    public SimpleResult Post(ManbaKharidFormModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, this.Request.GetClientIpAddress());
        string str = new TankhahManager().SaveManba(model, UserAccount.Instance.CurrentUser.UserName);
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
          result = "500.40",
          message = (object) ex.Message
        };
      }
    }

    public void Put(int id, [FromBody] string value)
    {
    }

    public SimpleResult Delete(int id, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        string str = new TankhahManager().DeleteManba(id, UserAccount.Instance.CurrentUser.UserName);
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
          result = "500.41",
          message = (object) ex.Message
        };
      }
    }  
  }
}
