using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Security;
using SepandAsa.Shared.Business.Utilities;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class UserController : ApiController
  {
    [Route("api/User/GetCurrentDate")]
    [HttpGet]
    public SimpleResult GetCurrentDate()
    {
      string curDate = SepandServer.CurDate;
      return new SimpleResult()
      {
        result = "200",
        message = (object) curDate
      };
    }

    [Route("api/User/HasUserPermission/{actions}/{token}")]
    [HttpGet]
    public SimpleResult HasUserPermission(string actions, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        string[] strArray = actions.Split(',');
        List<bool> boolList = new List<bool>();
        for (int index = 0; index < strArray.Length; ++index)
          boolList.Add(UserAccount.Instance.UserHasPermissionFor(39, strArray[index]));
        return new SimpleResult()
        {
          message = (object) boolList,
          result = "200"
        };
      }
      catch (Exception ex)
      {
        return new SimpleResult()
        {
          message = (object) ex.Message,
          result = "500.9"
        };
      }
    }   
  }
}
