using System;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.Bazargani.Common.Business.PurchaseSourceManagement;
using SepandAsa.Bazargani.Common.Domain.PurchaseSourceManagement;

namespace SAJApi.Controllers
{
    [EnableCors("http://192.168.1.8:888,http://localhost:8100,http://91.98.153.26:3000,http://localhost:3000,http://172.20.0.245:888", "*", "*")]
    public class manbaKharidController : ApiController
    {
        // GET: api/manbaKharid
        public string Get()
        {
            try
            {
                var count = ManbaKharidsManager.Instance.GetDataCount();
                var result = new SimpleResult {message = count, result = "200"};
                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                var result = new SimpleResult { message = ex.Message, result = "500" };
                return JsonConvert.SerializeObject(result);
            }
        }

        //GET: api/manbaKharid/5
        public string Get(int pageNumber, int pageCount)
        {
            try
            {
                var ds = CustomQueries.GetManbaKharidByPage(pageNumber, pageCount);
                var result = Utils.CreateListFromTable<manbaKharidModel>(ds.ManbaKharids);
                return JsonConvert.SerializeObject(new SimpleResult { message = result, result = "200" });
            }
            catch (Exception ex)
            {
                var result = new SimpleResult { message = ex.Message, result = "500" };
                return JsonConvert.SerializeObject(result);
            }
        }

        // POST: api/manbaKharid
        public void Post([FromBody]string value)
        {
            var manbakharid = JsonConvert.DeserializeObject<manbaKharidModel>(value);
            ManbaKharidsInfo ds = new ManbaKharidsInfo();
            var newRow = ds.ManbaKharids.NewManbaKharidsRow();
            Utils.SetRowFromItem(newRow, manbakharid);
            ds.ManbaKharids.AddManbaKharidsRow(newRow);
            ManbaKharidsManager.Instance.SaveInfo(ds);           
        }

        // PUT: api/manbaKharid/5
        public void Put(int id, [FromBody]string value)
        {
            var manbakharid = JsonConvert.DeserializeObject<manbaKharidModel>(value);
            ManbaKharidsInfo ds = ManbaKharidsManager.Instance.GetDataById(id) as ManbaKharidsInfo;
            Utils.SetRowFromItem(ds.ManbaKharids[0], manbakharid);
            ManbaKharidsManager.Instance.SaveInfo(ds);
        }

        // DELETE: api/manbaKharid/5
        public void Delete(int id)
        {            
            ManbaKharidsManager.Instance.DeleteById(id);
        }
    }
}
