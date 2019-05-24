using Newtonsoft.Json;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Shared.Business.Common;
using SepandAsa.Shared.Business.PowerBi;
using SepandAsa.Shared.Business.Utilities;
using SepandAsa.Shared.Domain.PowerBi;
using SepandAsa.UtilityClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class BiDashboardController : ApiController
  {
    [Route("api/BiDashboard/GetDashboards")]
    [HttpGet]
    public string GetDashboards()
    {
      try
      {
        List<treeModel> treeModelList = 
            new biDashTree(Utils.CreateListFromTable<biDash>(((IEnumerable<CatalogInfo.CatalogRow>) (CatalogManager.Instance.GetAllData() as CatalogInfo).Catalog).Where(r =>
            {
                if ((r.Type == 1 || r.Type == 13) && !string.IsNullOrEmpty(r.Name))
                    return r.Name != "System Resources";
                return false;
            }).OrderBy(r => r.Type).CopyToDataTable()).ToList()).MakeBiTreeModel();
        return JsonConvert.SerializeObject((object) new SimpleResult()
        {
          result = "200",
          message = (object) treeModelList
        });
      }
      catch (Exception ex)
      {
        return JsonConvert.SerializeObject((object) new SimpleResult()
        {
          result = "501.1",
          message = (object) ex.Message
        });
      }
    }

    [Route("api/BiDashboard/GetSelectDateRange/{dashId}")]
    [HttpGet]
    public string GetSelectDateRange(string dashId)
    {
      string str = "1395/01/01";
      string curDate = SepandServer.CurDate;
      int diffToDateInDays = PersianDateTime.Parse(str).GetDiffToDateInDays(PersianDateTime.Parse(curDate));
      selectDateModel selectDateModel = new selectDateModel()
      {
        startDate = str,
        endDate = curDate,
        daysDiff = diffToDateInDays
      };
      return JsonConvert.SerializeObject((object) new SimpleResult()
      {
        result = "200",
        message = (object) selectDateModel
      });
    }

    public BiDashboardController()
    {
      
    }
  }
}
