using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Security;
using SepandAsa.Shared.Business.Utilities;

namespace SAJApi.Controllers
{
    [EnableCors("http://192.168.1.8:888,http://localhost:8100,http://91.98.153.26:3000,http://localhost:3000,http://172.20.0.245:888", "*", "*")]
    public class UserController : ApiController
  {
    [Route("api/User/GetCurrentDate")]
    [HttpGet]
    public SimpleResult GetCurrentDate()
    {
      string curDate = SepandServer.CurDate;
      return new SimpleResult
      {
        result = "200",
        message = curDate
      };
    }

    [Route("api/User/HasUserPermission/{actions}/{token}")]
    [HttpGet]
    public SimpleResult HasUserPermission(string actions, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        string[] strArray = actions.Split(',');
        List<bool> boolList = new List<bool>();
        for (int index = 0; index < strArray.Length; ++index)
          boolList.Add(UserAccount.Instance.UserHasPermissionFor(39, strArray[index]));
        return new SimpleResult
        {
          message = boolList,
          result = "200"
        };
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.9"
        };
      }
    }   
  }
}
