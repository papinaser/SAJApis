//using SAJApi.Models;
//using SepandAsa.Shared.Business.Common;
//using SepandAsa.Shared.Domain.Attachments;
//using SepandAsa.Transportation.Light.Business.Car.BaseInfo;
//using SepandAsa.Transportation.Light.Business.Common;
//using SepandAsa.Transportation.Light.Business.Common.BaseInfo;
//using SepandAsa.Transportation.Light.Business.Web;
//using SepandAsa.Transportation.Light.Domain;
//using SepandAsa.Transportation.Light.Domain.Web;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;

//namespace SAJApi.Custom
//{
//  public class TarabariRequestManager
//  {
//    internal static AgGridModel GetUserRequests(int userId)
//    {
//      return Utils.GetAgGridModel((DataTable) ((TarabariCarRequestsManager) TarabariCarRequestsManager.Instance).GetByUserId(userId).get_TarabariCarRequests(), true, "");
//    }

//    internal static List<KeyValueModel> GetGeneralCarTypes(int userId)
//    {
//      return ((IEnumerable<CarTypeGeneralNamesInfo.CarTypeGeneralNamesRow>) (((BusinessManager) CarTypesInGeneralManager.Instance).GetAllData() as CarTypeGeneralNamesInfo).get_CarTypeGeneralNames()).Select<CarTypeGeneralNamesInfo.CarTypeGeneralNamesRow, KeyValueModel>((Func<CarTypeGeneralNamesInfo.CarTypeGeneralNamesRow, KeyValueModel>) (ct => new KeyValueModel()
//      {
//        key = ct.get_CarTypeGeneralNameId().ToString(),
//        value = ct.get_CarTypeGeneralNameTitle()
//      })).ToList<KeyValueModel>();
//    }

//    internal static TarabariCarRequestModel GetByRequestId(int requestId)
//    {
//      return Utils.CreateItemFromRow<TarabariCarRequestModel>((DataRow) (requestId != 0 ? ((BusinessManager) TarabariCarRequestsManager.Instance).GetDataById((object) requestId) as TarabariCarRequestsInfo : ((BusinessManager) TarabariCarRequestsManager.Instance).MakeNewData() as TarabariCarRequestsInfo).get_TarabariCarRequests()[0]);
//    }

//    internal static List<KeyValueModel> GetAllLocationForMission(int userId)
//    {
//      return ((IEnumerable<LocationInfo.LocationRow>) (((BusinessManager) LocationManager.Instance).GetAllData() as LocationInfo).get_Location()).Select<LocationInfo.LocationRow, KeyValueModel>((Func<LocationInfo.LocationRow, KeyValueModel>) (r => new KeyValueModel()
//      {
//        key = r.get_LocationId().ToString(),
//        value = r.Name
//      })).ToList<KeyValueModel>();
//    }

//    internal static IEnumerable<AttachmentInfoModel> GetAttachments(
//      int requestId,
//      out long headerId)
//    {
//      TarabariCarRequestsInfo dataById = ((BusinessManager) TarabariCarRequestsManager.Instance).GetDataById((object) requestId) as TarabariCarRequestsInfo;
//      if (dataById.get_TarabariCarRequests()[0].IsAttachmentsHeaderIdNull())
//      {
//        AttachmentsInfo attachmentsHeader = SepandAsa.Shared.Business.Attachments.Attachments.CreateAttachmentsHeader((InfoTypes) 1, "1");
//        dataById.get_TarabariCarRequests()[0].set_AttachmentsHeaderId(attachmentsHeader.AttachmentsHeader[0].AttachmentsHeaderId);
//        ((BusinessManager) TarabariCarRequestsManager.Instance).Update((DataSet) dataById);
//      }
//      headerId = dataById.get_TarabariCarRequests()[0].AttachmentsHeaderId;
//      return Utils.CreateListFromTable<AttachmentInfoModel>((DataTable) SepandAsa.Shared.Business.Attachments.Attachments.GetAttachments(headerId).Attachments);
//    }
//  }
//}
