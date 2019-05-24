using SAJApi.Custom;
using SepandAsa.Shared.Business.Security;
using System;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class AmvalsController : ApiController
  {
    [Route("api/Amvals/GetUserAmvals/{typeId}/{token}")]
    [HttpGet]
    public IHttpActionResult GetUserAmvals(int typeId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        return (IHttpActionResult) new FileResult(Utils.ConvertStiToPdf
            (AmvalsManager.GetStiReport(UserAccount.Instance.CurrentUser.UserName, typeId), Utils.GetTempFileName(token, "salary", "pdf")), (string) null);
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex.Message, ex);
        throw new ApplicationException(ex.Message);
      }
    }

    public void Post([FromBody] string value)
    {
    }

    public void Put(int id, [FromBody] string value)
    {
    }

    public void Delete(int id)
    {
    }
    
  }
}
