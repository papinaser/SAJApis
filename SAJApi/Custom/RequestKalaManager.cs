using SAJApi.Models;
using SepandAsa.Bazargani.Common.Business;
using SepandAsa.Bazargani.Common.Bussiness;
using SepandAsa.Bazargani.Common.Domain;
using SepandAsa.Bazargani.Sefareshat.Business.DarkhastKala;
using SepandAsa.Bazargani.Sefareshat.Domain.DarkhastKala;
using SepandAsa.Shared.Business.Common;
using SepandAsa.Shared.Business.ParamDataType;
using SepandAsa.Shared.Business.Security;
using SepandAsa.Shared.Business.Utilities;
using SepandAsa.Shared.Domain.Attachments;
using SepandAsa.Shared.Domain.ParamDataType;
using SepandAsa.UtilityClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SAJApi.Custom
{
  public class RequestKalaManager
  {
    internal static AgGridModel GetUserReqquests(string userName)
    {
      return Utils.GetAgGridModel(RequestKalasManager.Instance.GetByUserName(userName).RequestKalas,
          true, "");
    }

    internal static AgGridModel GetItemsByRequestKalaId(int requestId)
    {
      RequestKalasInfo itemsByHeaderId = ((RequestKalasManager) RequestKalasManager.Instance).GetItemsByHeaderId(requestId);
      string columnNames = ",RequestKalaItemId,ItemDescription,ItemAmount,ItemUnitId,UnitName,StockId,SubProjectId,ProjectId,TarhId,IsTarh,StockRoomNumber,PlanCode,BuyProjectCode,SubProjectNumber";
      itemsByHeaderId.EnforceConstraints = false;
      return Utils.GetAgGridModel(itemsByHeaderId.Tables["RequestKalaItems"].Copy(), true, columnNames);
    }

    internal static RequestKalaModel GetHeaderByRequestKalaId(int requestId)
    {
      return Utils.CreateItemFromRow<RequestKalaModel>((
          (requestId != 0 ? RequestKalasManager.Instance.GetHeaderById(requestId) : RequestKalasManager.Instance.MakeNewData() as RequestKalasInfo)?.RequestKalas).Rows[0]);
    }

    internal static IEnumerable<AttachmentInfoModel> GetAttachments(
      int requestKalaId,
      out long headerId)
    {
      RequestKalasInfo headerById = ((RequestKalasManager) RequestKalasManager.Instance).GetHeaderById(requestKalaId);
      if (headerById.RequestKalas[0].IsAttachmentsHeaderIdNull())
      {
        AttachmentsInfo attachmentsHeader = SepandAsa.Shared.Business.Attachments.Attachments.CreateAttachmentsHeader((InfoTypes) 1, "1");
        headerById.RequestKalas[0].AttachmentsHeaderId=attachmentsHeader.AttachmentsHeader[0].AttachmentsHeaderId;
        ((BusinessManager) RequestKalasManager.Instance).SaveInfo((DataSet) headerById);
      }
      headerId = headerById.RequestKalas[0].AttachmentsHeaderId;
      return Utils.CreateListFromTable<AttachmentInfoModel>((DataTable) SepandAsa.Shared.Business.Attachments.Attachments.GetAttachments(headerId).Attachments);
    }

    internal static IEnumerable<KeyValueModel> GetOrderDescs()
    {
      OrderSummaryDescInfo orderSummaryDesc = OrderSummaryDesc.Instance.GetAllOrderSummaryDesc();
      return orderSummaryDesc.OrderSummaryDesc.Select(osd => new KeyValueModel
      {
          key = osd.OrderSummaryDescId.ToString(),
          value = osd.OrderSummaryDescName
      });
    }

    internal static List<KeyValueModel> GetUnits()
    {
      UnitInfo allUnit = ((Unit) Unit.Instance).GetAllUnit();
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      using (IEnumerator<UnitInfo.UnitRow> enumerator = allUnit.Unit.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          UnitInfo.UnitRow current = enumerator.Current;
          keyValueModelList.Add(new KeyValueModel()
          {
            key = current.UnitId.ToString(),
            value = current.UnitName
          });
        }
      }
      return keyValueModelList;
    }

    internal static string SaveRequest(SaveRequestKalaModel saveModel, bool isUpdate = false)
    {
      if (saveModel.header.requestKalaTypeId != 1 && saveModel.header.requestKalaTypeId != 2)
        throw new ApplicationException("نوع درخواست نامعتبر است");
      PersianDateTime persianDateTime;
      if (saveModel.header.needDate == "" || !PersianDateTime.TryParse(saveModel.header.needDate,
              out persianDateTime))
        throw new ApplicationException("نوع درخواست نامعتبر است");
      if (saveModel.header.description.Contains("--"))
        throw new ApplicationException("توضیحات عبارت نامعتبر دارد");
      RequestKalasInfo requestKalasInfo = !isUpdate ? ((BusinessManager) RequestKalasManager.Instance).MakeNewData() as RequestKalasInfo : ((RequestKalasManager) RequestKalasManager.Instance).GetHeaderById(saveModel.header.requestKalaId);
      requestKalasInfo.RequestKalas[0].EmployeeId=UserAccount.Instance.CurrentUser.EmployeeId;
      requestKalasInfo.RequestKalas[0].RequesterOrganizationId=UserAccount.Instance.CurrentUser.OrganizationUnitId;
      requestKalasInfo.RequestKalas[0].RequsterUserId=UserAccount.Instance.CurrentUser.UserId;
      requestKalasInfo.RequestKalas[0].RequestDate=SepandServer.CurDate;
      requestKalasInfo.RequestKalas[0].RequestTime=SepandServer.CurTime;
      requestKalasInfo.RequestKalas[0].Status="ثبت شده";
      Utils.SetRowFromItem(requestKalasInfo.RequestKalas[0], saveModel.header);
      if (isUpdate)
      {
                requestKalasInfo.Merge(RequestKalasManager.Instance.GetItemsByHeaderId(saveModel.header.requestKalaId));
        for (int index = 0; index < requestKalasInfo.RequestKalaItems.Count(); ++index)
          (requestKalasInfo.RequestKalaItems[index]).Delete();
      }
      foreach (RequestKalaItemModel requestKalaItemModel1 in saveModel.items)
      {
        if ((double) requestKalaItemModel1.ItemAmount <= 0.0)
          throw new ApplicationException(string.Format("مقدار برای آیتم {0} نامعتبر است",requestKalaItemModel1.ItemDescription));
        if (requestKalaItemModel1.ItemUnitId == 0)
          throw new ApplicationException(string.Format("واحد برای آیتم {0} نامعتبر است", requestKalaItemModel1.ItemDescription));
        RequestKalasInfo.RequestKalaItemsRow requestKalaItemsRow = requestKalasInfo.RequestKalaItems.NewRequestKalaItemsRow();
        RequestKalaItemModel requestKalaItemModel2 = requestKalaItemModel1;
        int requestKalaId;
        requestKalaItemsRow.RequestKalaId=requestKalaId = requestKalasInfo.RequestKalas[0].RequestKalaId;
        int num = requestKalaId;
        requestKalaItemModel2.RequestKalaId = num;
        requestKalasInfo.RequestKalaItems.AddRequestKalaItemsRow(requestKalaItemsRow);
        requestKalaItemModel1.RequestKalaItemId = requestKalaItemsRow.RequestKalaItemId;
        Utils.SetRowFromItem<RequestKalaItemModel>((DataRow) requestKalaItemsRow, requestKalaItemModel1);
      }
      RequestKalasManager.Instance.SaveInfo(requestKalasInfo);
      return "ذخیره سازی درخواست با موفقیت انجام شد";
    }

    internal static IEnumerable<KeyValueModel> GetStocks()
    {
        var stocks = StockRoom.Instance
            .GetAllStockRoom();
        return stocks.StockRoom.Select(stock => new KeyValueModel
        {
            key = stock.StockRoomId.ToString(),
            value = $"{stock.StockRoomName}({stock.StockRoomNumber})"
        });            
    }

    internal static IEnumerable<KeyValueModel> GetTarhs()
    {
        var tarhs = PlanManager.Instance.GetAllPlan();
        return tarhs.Plan.Select(plan => new KeyValueModel
        {
            key = plan.PlanId.ToString(),
            value = $"{plan.PlanName}({plan.PlanCode})"
        });      
    }

    internal static IEnumerable<KeyValueModel> GetProjects()
    {
        var projects = BuyProjectManager.Instance.GetAllBuyProject();
        return projects.BuyProject.Select(prj => new KeyValueModel
        {
            key = prj.BuyProjectId.ToString(),
            value = $"{prj.BuyProjectName}({prj.BuyProjectCode})"
        });
    }

    internal static IEnumerable<KeyValueModel> GetSubProjects()
    {
        var subs = SubProjectManager.Instance.GetAll();
        return subs.Select(sub => new KeyValueModel
        {
            key = sub.SubProjectId.ToString(),
            value = $"{sub.SubProjectName}({sub.SubProjectNumber})"
        });      
    }

    internal static object DeleteById(int id)
    {
            RequestKalasManager.Instance.DeleteById(id);
      return "حذف درخواست با موفقیت انجام شد";
    }
  }
}
