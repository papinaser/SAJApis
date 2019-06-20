using System.Collections.Generic;
using SAJApi.Models;
using SepandAsa.Bazargani.Common.Business;
using SepandAsa.Bazargani.Common.Business.PurchaseSourceManagement;
using SepandAsa.Bazargani.Common.Domain;
using SepandAsa.Bazargani.Common.Domain.PurchaseSourceManagement;
using SepandAsa.Shared.Business.BaseInfo;

namespace SAJApi.Custom
{
  public class TankhahManager
  {
    internal static AgGridModel GetAllPurchaseSource(string userName)
    {
      return Utils.GetAgGridModel((ManbaKharidsManager.Instance.GetAllData() as ManbaKharidsInfo).ManbaKharids, true, "");
    }

    internal static List<KeyValueModel> GetManbaTypes(string userName)
    {
      PurchaseSourceTypeInfo all = PurchaseSourceType.GetAll();
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      using (IEnumerator<PurchaseSourceTypeInfo.PurchaseSourceTypeRow> enumerator = all.PurchaseSourceType.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          PurchaseSourceTypeInfo.PurchaseSourceTypeRow current = enumerator.Current;
          keyValueModelList.Add(new KeyValueModel
          {
            value = current.PurchaseSourceTypeId.ToString(),
            label = current.PurchaseSourceTypeName
          });
        }
      }
      return keyValueModelList;
    }

    internal static manbaKharidModel GetManbaForEdit(
      int manbaKharidId,
      string userName)
    {
      return Utils.CreateItemFromRow<manbaKharidModel>((ManbaKharidsManager
          .Instance.GetDataById(manbaKharidId) as ManbaKharidsInfo).ManbaKharids[0]);
    }

    internal string SaveManba(ManbaKharidFormModel model, string userName)
    {
      model.data.resisterUserName = userName;
      model.data.companyId = Company.Instance.CurrentCompany.CompanyId;
      model.data.subSystemId = 39;
      ManbaKharidsInfo manbaKharidsInfo;
      if (model.isEdit)
      {
        manbaKharidsInfo = ManbaKharidsManager.Instance.GetDataById(model.data.manbaKharidId) as ManbaKharidsInfo;
      }
      else
      {
        manbaKharidsInfo = ManbaKharidsManager.Instance.MakeNewData() as ManbaKharidsInfo;
        model.data.manbaKharidId = -1;
      }
      Utils.SetRowFromItem(manbaKharidsInfo.ManbaKharids[0], model.data);
      ManbaKharidsManager.Instance.SaveInfo(manbaKharidsInfo);
      return "ذخیره سازی منبع با موفقیت انجام شد";
    }

    internal static List<KeyValueModel> GetStates(string userName)
    {
      StateInfo allState = State.Instance.GetAllState();
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      using (IEnumerator<StateInfo.StateRow> enumerator = allState.State.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          StateInfo.StateRow current = enumerator.Current;
          keyValueModelList.Add(new KeyValueModel
          {
            value = current.StateId.ToString(),
            label = current.StateName
          });
        }
      }
      return keyValueModelList;
    }

    internal static IEnumerable<CityModel> GetCities(string userName)
    {
      return Utils.CreateListFromTable<CityModel>(City.Instance.GetAllCity().City);
    }

    internal string DeleteManba(int id, string userName)
    {
      ManbaKharidsManager.Instance.DeleteById(id);
      return "منبع خرید با موفقیت جذف شد";
    }
  }
}
