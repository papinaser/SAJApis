using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Cors;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Security;

namespace SAJApi.Controllers
{
    [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://localhost:3000,http://172.20.0.245:888", "*", "*")]
    public class SalaryStrapController : ApiController
  {
    [Route("api/SalaryStrap/GetCompanies/{token}")]
    [HttpGet]
    public SimpleResult GetCompanies(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        List<KeyValueModel> userCompanies = SalaryStrapManager.GetUserCompanies(UserAccount.Instance.CurrentUser.UserName);
        return new SimpleResult
        {
          result = "200",
          message = userCompanies
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.37",
          message = ex.Message
        };
      }
    }

    [Route("api/SalaryStrap/GetYears/{companyId}/{token}")]
    [HttpGet]
    public SimpleResult GetYears(int companyId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        IEnumerable<int> years = SalaryStrapManager.GetYears(UserAccount.Instance.CurrentUser.UserName, companyId);
        return new SimpleResult
        {
          result = "200",
          message = years
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.38",
          message = ex.Message
        };
      }
    }

    [Route("api/SalaryStrap/GetMonths/{companyId}/{year}/{token}")]
    [HttpGet]
    public SimpleResult GetMonths(int companyId, string year, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        List<KeyValueModel> months = SalaryStrapManager.GetMonths(UserAccount.Instance.CurrentUser.UserName, companyId, year);
        return new SimpleResult
        {
          result = "200",
          message = months
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.38",
          message = ex.Message
        };
      }
    }

    [Route("api/SalaryStrap/GetSalaryStrap/{companyId}/{year}/{month}/{token}")]
    [HttpGet]
    public IHttpActionResult GetSalaryStrap(
      int companyId,
      string year,
      string month,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        return new FileResult(Utils.ConvertStiToPdf(SalaryStrapManager.GetSalaryStrap(UserAccount.Instance.CurrentUser.UserName, companyId, year, month), Utils.GetTempFileName(token, "salary", "pdf")), null);
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        throw new ApplicationException(ex.Message);
      }
    }

    public string Get(int id)
    {
      return "value";
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
