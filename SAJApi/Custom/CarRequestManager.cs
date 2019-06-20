using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using SAJApi.Models;
using SepandAsa.Shared.Business.Attachments;
using SepandAsa.Shared.Business.Security;
using SepandAsa.Shared.Business.Utilities;
using SepandAsa.Shared.DataAccess;
using SepandAsa.Shared.Domain;
using SepandAsa.Shared.Domain.Attachments;
using SepandAsa.Transportation.Light.Business;
using SepandAsa.Transportation.Light.Business.Car.BaseInfo;
using SepandAsa.Transportation.Light.Business.Car.LightDispenceOrg;
using SepandAsa.Transportation.Light.Business.Common;
using SepandAsa.Transportation.Light.Business.Common.BaseInfo;
using SepandAsa.Transportation.Light.Business.Web;
using SepandAsa.Transportation.Light.DataAccess.Car.LightDispenceOrg;
using SepandAsa.Transportation.Light.DataAccess.Web;
using SepandAsa.Transportation.Light.Domain;
using SepandAsa.Transportation.Light.Domain.Web;
using SepandAsa.Utility;
using SepandAsa.UtilityClasses;

namespace SAJApi.Custom
{
  public class CarRequestManager : IDisposable
  {

    internal AgGridModel GetAllCarRequestForMissionByPersonelNo(long requesterPersonelNo)
    {
      if (!WebRequestManager.Instance.HavePermitForCarRequest(requesterPersonelNo))
        throw new ApplicationException("شما مجوز ارسال درخواست خودرو را ندارید");
      WebConfigsInfo.SalaryWagePersonelsRow personelById = WebRequestManager.Instance.GetPersonelById(requesterPersonelNo);
      if (personelById == null)
        throw new ApplicationException("اطلاعات پرسنل مورد نظر یافت نشد");
      return Utils.GetAgGridModel(CarRequestForMissionManager.
          Instance.GetByRequesterPersonelId(personelById.SalaryWagePersonelId).
          CarRequestForMission, false, "");
    }

       
    internal List<KeyValueModel> GetGeneralCarTypes(int userId)
    {
        return ((IEnumerable<CarTypeGeneralNamesInfo.CarTypeGeneralNamesRow>) (CarTypesInGeneralManager.Instance
                .GetAllData() as CarTypeGeneralNamesInfo).CarTypeGeneralNames)
            .Select(
                ct => new KeyValueModel
                {
                    value = ct.CarTypeGeneralNameId.ToString(),
                    label = ct.CarTypeGeneralNameTitle
                }).ToList();
    }

    public List<KeyValueModel> GetAllCarTypes()
    {
      CarTypeInfo allData = CarTypeManager.Instance.GetAllData() as CarTypeInfo;
      IEnumerable<string> strings = allData.CarType.Select(r => r.Name).Distinct();
      List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
      foreach (string str in strings)
      {
        string dn = str;
        CarTypeInfo.CarTypeRow carTypeRow = allData.CarType.First(r => r.Name == dn);
        keyValueModelList.Add(new KeyValueModel
        {
          value = carTypeRow.CarTypeId.ToString(),
          label = carTypeRow.CarTypeName
        });
      }
      return keyValueModelList;
    }

    public List<KeyValueModel> GetAllLocationForMission()
    {
      return (LocationManager.Instance.GetAllData() as LocationInfo)
          .Location.Select(r => new KeyValueModel
      {
          value = r.LocationId.ToString(),
          label = r.Name
      }).ToList();
    }

    public void AddRequestItems(CarRequestForMissionInfo newReq, IEnumerable<string> destinations)
    {
      newReq.CarRequestForMissionItems.Clear();
      string[] strArray = destinations as string[] ?? destinations.ToArray();
      int num = strArray.Count();
      for (int index = 0; index < strArray.Length; ++index)
      {
        LocationInfo locationInfo = LocationManager.Instance
            .Search(string.Format("Name=N'{0}'", strArray[index])) as LocationInfo;
        if (locationInfo.Location.Count== 0)
          throw new ApplicationException("مقصد انتخاب شده معتبر نمی باشد " + strArray[index]);
        if (index == 0)
        {
          AddNewPath(newReq, DefaultsManager.Instance
              .Defaults.MissionCityId, locationInfo.Location[0].LocationId);
        }
        else
        {
          long locationId1 = locationInfo.Location[0].LocationId;
          long locationId2 = ((LocationInfo) LocationManager.Instance
                  .Search(string.Format("Name=N'{0}'", strArray[index - 1])))
              .Location[0].LocationId;
          AddNewPath(newReq, locationId2, locationId1);
          if (index + 1 != num)
          {
            long slId = locationId1;
            long locationId3 = (LocationManager.Instance
                .Search(string.Format("Name=N'{0}'", strArray[index + 1])) as LocationInfo).Location[0].LocationId;
            AddNewPath(newReq, slId, locationId3);
          }
        }
        if (index + 1 == num)
          AddNewPath(newReq, locationInfo.Location[0].LocationId, 
              DefaultsManager.Instance.Defaults.MissionCityId);
      }
    }

    public long AddRequesAttachment(
      CarRequestForMissionInfo newReq,
      byte[] attachedFile,
      long reqId)
    {
      if (attachedFile == null)
        return 0;
      AttachmentsInfo attachmentsHeader = 
          Attachments
              .CreateAttachmentsHeader((InfoTypes) 19, reqId.ToString());
      AttachmentsInfo.AttachmentsDetailRow attachmentsDetailRow = attachmentsHeader.AttachmentsDetail
          .NewAttachmentsDetailRow();
      attachmentsDetailRow.AttachmentExtension=".jpg";
      attachmentsDetailRow.IsScan=FilesUtil.IsImageFile(attachedFile);
      attachmentsDetailRow.AttachmentData=attachedFile;
      attachmentsHeader.AttachmentsDetail.AddAttachmentsDetailRow(attachmentsDetailRow);
      attachmentsDetailRow.OrginalFileCapacity=(int)attachmentsDetailRow.AttachmentId;
      AttachmentsInfo.AttachmentsRow attachmentsRow = attachmentsHeader.Attachments.NewAttachmentsRow();
      attachmentsRow.AttachmentId=attachmentsDetailRow.AttachmentId;
      attachmentsRow.IsScan=attachmentsDetailRow.IsScan;
      attachmentsRow.AttachmentsHeaderId=attachmentsHeader.AttachmentsHeader[0].AttachmentsHeaderId;
      attachmentsRow.AttachmentDescription="فایل تائیدبه درخواست خودرو";
      attachmentsRow.AttachmentTitle="تائیدیه درخواست خودرو";
      attachmentsRow.SubSystemId=LightTransportationSubSystem.Instance.Id;
      attachmentsRow.UserId=UserAccount.Instance.CurrentUser.UserId;
      attachmentsRow.LastAttachmentDataChangeDate=DateTime.Now;
      attachmentsHeader.Attachments.AddAttachmentsRow(attachmentsRow);
      Attachments.CreateAttachment(attachmentsHeader);
      return attachmentsHeader.AttachmentsHeader[0].AttachmentsHeaderId;
    }

    public void AddNewPath(CarRequestForMissionInfo carRequest, long slId, long dlId)
    {
      CarRequestForMissionInfo.CarRequestForMissionItemsRow forMissionItemsRow = 
          carRequest.CarRequestForMissionItems.NewCarRequestForMissionItemsRow();
      forMissionItemsRow.CarRequestForMissionId=carRequest.CarRequestForMission[0].CarRequestForMissionId;
      forMissionItemsRow.ItemId=carRequest.CarRequestForMissionItems.Count() + 1;
      forMissionItemsRow.SourceLocationId=slId;
      forMissionItemsRow.DestinationLocationId=dlId;
      carRequest.CarRequestForMissionItems.AddCarRequestForMissionItemsRow(forMissionItemsRow);
    }

    public CarRequestForMissionInfo ConvertModelToTable(
      CarRequestModel model)
    {
      CarRequestForMissionInfo requestForMissionInfo = CarRequestForMissionManager.Instance.MakeNewData() as CarRequestForMissionInfo;
      DataSetHelper.CopyRowInfo(((DataTable) JsonConvert.DeserializeObject(JsonConvert.SerializeObject(model), typeof (DataTable))).Rows[0], requestForMissionInfo.CarRequestForMission[0]);
      return requestForMissionInfo;
    }

    private void SetDefaults(long personelNo, CarRequestForMissionInfo result)
    {
      result.CarRequestForMission[0].RequestDate=PersianDateTime.Now.ToString("yyyy/mm/dd");
      result.CarRequestForMission[0].RequestTime=PersianDateTime.Now.ToString((TimeFormat) 1);
      result.CarRequestForMission[0].CarRequestTypeId=(int)CarRequestForMissionTypes.ByWeb;
      WebConfigsInfo.SalaryWagePersonelsDataTable pesonelByPersonelNo = GetPesonelByPersonelNo(personelNo);
      result.CarRequestForMission[0].PermissiverName=
          pesonelByPersonelNo[0].FirstName+ " " + pesonelByPersonelNo[0].LastName;
      result.CarRequestForMission[0].UnitId=pesonelByPersonelNo[0].OrganizationUnitId;
      result.CarRequestForMission[0].UnitName=pesonelByPersonelNo[0].OrganizationUnitName;
    }

    private WebConfigsInfo.SalaryWagePersonelsDataTable GetPesonelByPersonelNo(
      long personelNo)
    {
      using (new DbConnectionScope())
        return new SalaryWagePersonelsDB().GetByPersonelNo((int) personelNo);
    }

    private void VerifyWebRequestForSave(CarRequestForMissionInfo requestInfo)
    {
      if (requestInfo.CarRequestForMission.Count== 0)
        throw new ApplicationException("اطلاعات هدر درخواست ثبت نشده است");
      if (requestInfo.CarRequestForMissionItems.Count== 0)
        throw new ApplicationException("اطلاعات مسیرهای درخواست ثبت نشده است");
    }

    public void SaveRequest(long personelNo, CarRequestForMissionInfo requestInfo)
    {
      VerifyWebRequestForSave(requestInfo);
      SetDefaults(personelNo, requestInfo);
      CarRequestForMissionManager.Instance.SaveInfo(requestInfo);
    }

    public int GetRequestState(long carRequestId)
    {
      using (new DbConnectionScope())
        return new CarRequestForMissionDB().GetRequestState(carRequestId);
    }

    public void VerifyCanPersonelRegisterNewCarRequest(long personelNo)
    {
      WebConfigsInfo.SalaryWagePersonelsRow personelById = GetPersonelById(personelNo);
      if (personelById == null)
        throw new ApplicationException("پرسنل باکدپرسنلی مورد نظریافت نشد");
      if (personelById.IsHasCarRequestPermitNull() || !personelById.HasCarRequestPermit)
        throw new ApplicationException("مجوز ثبت درخواست برای پرسنل مورد نظر صادرنشده است");
      WebConfigsInfo webRequestConfig = WebConfigsManager.Instance.GetWebRequestConfig(SepandServer.CurDate);
      if (webRequestConfig == null || webRequestConfig.WebRequestConfigs.Count== 0
                                   || !DateTimeUtil.TimesHaveConflict
                                       (webRequestConfig.WebRequestConfigs[0].CarReqestStartTime, 
                                       webRequestConfig.WebRequestConfigs[0].CarRequestEndTime, 
                                       SepandServer.CurTime, SepandServer.CurTime))
        throw new ApplicationException("لطفا در زمان تعیین شده نسبت به ثبت درخواست اقدام نمایید");
    }

    public CarRequestForMissionInfo ValidateNewRequestAndSave(
      CarRequestForMissionInfo dsRequestInfo,
      int requesterPersonelNo)
    {
      if (dsRequestInfo == null || dsRequestInfo.Tables.Count <= 0)
        throw new ApplicationException(" دیتاست حاوی جداول هدر و مسیرهای درخواست نیست ");
      if (dsRequestInfo.Tables["CarRequestForMission"].Rows.Count <= 0)
        throw new ApplicationException(" اطلاعات هدر درخواست منتقل نشده ");
      CarRequestForMissionInfo requestForMissionInfo = CarRequestForMissionManager.Instance.MakeNewData() as CarRequestForMissionInfo;
      requestForMissionInfo.CarRequestForMissionItems.Clear();
      long requestForMissionId = requestForMissionInfo.CarRequestForMission[0].CarRequestForMissionId;
      DataSetHelper.CopyRowInfo(dsRequestInfo.Tables["CarRequestForMission"].Rows[0],
          requestForMissionInfo.CarRequestForMission[0]);
      if (dsRequestInfo.Tables["CarRequestForMission"].Rows[0].IsNull("CarTypeName") 
          || string.IsNullOrEmpty(dsRequestInfo.Tables["CarRequestForMission"].Rows[0]["CarTypeName"].ToString()))
        throw new ApplicationException("نوع خودور در ستون CarTypeName مشخص نشده است");
      object obj = dsRequestInfo.Tables["CarRequestForMission"].Rows[0]["CarTypeName"];
      DataRow[] dataRowArray = (CarTypeManager.Instance.GetAllData() as CarTypeInfo).CarType.Select("CarTypeName LIKE '" + obj + "*'");
      if (!dataRowArray.Any())
        throw new ApplicationException("نوع خودروی انتخاب شده در بانک حمل و نقل نامعتبر است");
      requestForMissionInfo.CarRequestForMission[0]["CarTypeId"] = dataRowArray[0]["CarTypeId"];
      if (requestForMissionInfo.CarRequestForMission[0].IsMissionTypeIdNull() || 
          requestForMissionInfo.CarRequestForMission[0].MissionTypeId!= (int)MissionTypes.Inside 
          && requestForMissionInfo.CarRequestForMission[0].MissionTypeId != (int)MissionTypes.Outside)
                throw new ApplicationException("نوع ماموریت مشخص نشده است");
      requestForMissionInfo.CarRequestForMission[0].CarRequestForMissionId=requestForMissionId;
      if (requestForMissionInfo.CarRequestForMission.Count> 0)
      {
        WebConfigsInfo.SalaryWagePersonelsRow personelById = GetPersonelById(requesterPersonelNo);
        if (personelById == null)
          throw new ApplicationException("کدپرسنلی موردنظرثبت نشده است");
        if (personelById.IsOrganizationUnitIdNull())
          throw new ApplicationException("واحد سازمانی پرسنل مشخص نشده است");
        requestForMissionInfo.CarRequestForMission[0].UnitId=personelById.OrganizationUnitId;
        requestForMissionInfo.CarRequestForMission[0].RequesterPersonelId=personelById.SalaryWagePersonelId;
        requestForMissionInfo.CarRequestForMission[0].CarRequestTypeId=(int)CarRequestForMissionTypes.ByWeb;
        int num1 = 1;
        if (dsRequestInfo.Tables["CarRequestForMissionItems"].Rows.Count <= 0)
          throw new ApplicationException(" اطلاعات مسیرهای درخواست  منتقل نشده ");
        foreach (DataRow row in (InternalDataCollectionBase) dsRequestInfo.Tables["CarRequestForMissionItems"].Rows)
        {
          if (row.RowState != DataRowState.Deleted)
          {
            CarRequestForMissionInfo.CarRequestForMissionItemsRow forMissionItemsRow1 = requestForMissionInfo.CarRequestForMissionItems.NewCarRequestForMissionItemsRow();
            DataSetHelper.CopyRowInfo(row, forMissionItemsRow1);
            forMissionItemsRow1.CarRequestForMissionId=requestForMissionInfo.CarRequestForMission[0].CarRequestForMissionId;
            CarRequestForMissionInfo.CarRequestForMissionItemsRow forMissionItemsRow2 = forMissionItemsRow1;
            int num2;
            forMissionItemsRow1.ItemId=num2 = num1++;
            long num3 = num2;
            forMissionItemsRow2.RequestItemId=num3;
            forMissionItemsRow1.SourceLocationId=(long) row["SourceLocationId"];
            forMissionItemsRow1.DestinationLocationId=(long) row["DestinationLocationId"];
            requestForMissionInfo.CarRequestForMissionItems.AddCarRequestForMissionItemsRow(forMissionItemsRow1);
          }
        }
        CarRequestForMissionManager.Instance.SaveInfo(requestForMissionInfo);
      }
      return requestForMissionInfo;
    }

    public WebConfigsInfo.SalaryWagePersonelsRow GetPersonelById(long personelNo)
    {
      using (new DbConnectionScope())
      {
        WebConfigsInfo.SalaryWagePersonelsDataTable byPersonelNo = new SalaryWagePersonelsDB().GetByPersonelNo((int) personelNo);
        if (byPersonelNo.Count> 0)
          return byPersonelNo[0];
        return null;
      }
    }

    public DataTable GetCarRequestStateByRequestId(long carRequestId)
    {
      if ((CarRequestForMissionManager.Instance.GetDataById(carRequestId) as CarRequestForMissionInfo).CarRequestForMission[0].CarRequestTypeId != (int)CarRequestForMissionTypes.ByWeb)
        throw new ApplicationException("درخواست مورد نظر شما توسط وب ثبت نشده است");
      CarRequestForMissionInfo.viewCarRequestStateFullRow requestState = CarRequestForMissionManager.Instance.GetRequestState(carRequestId);
      CarRequestForMissionInfo.viewCarRequestStateFullDataTable requestStateFull = 
          new CarRequestForMissionInfo().viewCarRequestStateFull;
      requestStateFull.ImportRow(requestState);
      return requestStateFull;
    }

    public void Dispose()
    {
    }
  }
}
