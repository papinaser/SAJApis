using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using SAJApi.Models;
using SepandAsa.RepairManagment.Business;
using SepandAsa.Shared.Business.Attachments;
using SepandAsa.Shared.Domain.Attachments;

namespace SAJApi.Custom
{
  public class ArchivesManager : IDisposable
  {
    public void Dispose()
    {
    }

    internal static IEnumerable<AttachmentInfoModel> GetAttachmentsByHeadeId(
      long attachmentHeaderId)
    {
      return Utils.CreateListFromTable<AttachmentInfoModel>(Attachments.GetAttachments(attachmentHeaderId).Attachments);
    }

    internal static IEnumerable<AttachmentGroupModel> GroupAttachmentsList(
      IEnumerable<AttachmentInfoModel> list)
    {
      foreach (AttachmentInfoModel attachmentInfoModel in list.Where(r => string.IsNullOrEmpty(r.groupName)))
      {
        attachmentInfoModel.groupName = "بدون گروه";
        attachmentInfoModel.attachmentGroupId = 0;
      }
      List<AttachmentGroupModel> attachmentGroupModelList = new List<AttachmentGroupModel>();
      foreach (IGrouping<string, AttachmentInfoModel> source in list.GroupBy(r => r.groupName))
        attachmentGroupModelList.Add(new AttachmentGroupModel
        {
          groupName = source.Key,
          list = source.ToList()
        });
      return attachmentGroupModelList;
    }

    internal static long GetDetailsLogAttachsHeaderId(long tblId, long roykardId, long mrId)
    {
      string str1 = string.Format("tbl{0}tbl{1}", 
          ModelRelation.Instance.GetModelRelationById(mrId).ModelRelation[0].SetTypeItemId,roykardId);
      string str2 = str1 + "Id";
      DataTable rowFromDataTable = ClsDinamicallyTable.Instance.GetSelectedRowFromDataTable(str1, string.Format("{0} = {1} ", str2, tblId));
      rowFromDataTable.TableName = str1;
      if (rowFromDataTable == null || rowFromDataTable.Rows.Count <= 0 || rowFromDataTable.Rows[0] == null)
        throw new ApplicationException("اطلاعات رکورد جرئیات یافت نشد");
      if (rowFromDataTable.Rows[0]["AttachmentsHeaderId"] == DBNull.Value)
      {
        AttachmentsInfo attachmentsHeader = Attachments.CreateAttachmentsHeader((InfoTypes) 31, tblId.ToString() + rowFromDataTable.Rows[0]["RoykardConfigId"]);
        if (attachmentsHeader.AttachmentsHeader.Count<= 0)
          throw new ApplicationException(" اطلاعات ضمائم ساخته نشد ");
        if (!attachmentsHeader.AttachmentsHeader[0].IsNull("AttachmentsHeaderId"))
        {
          rowFromDataTable.Rows[0]["AttachmentsHeaderId"] = attachmentsHeader.AttachmentsHeader[0].AttachmentsHeaderId;
          (ClsDinamicallyTable.Instance)
              .UpdateDynamicDataTable(rowFromDataTable);
        }
      }
      return long.Parse(rowFromDataTable.Rows[0]["AttachmentsHeaderId"].ToString());
    }

    internal static IEnumerable<AttachmentInfoModel> GetMasterLogAttachs(
      long masterLogId,
      out long headerId)
    {
      headerId = MasterLog.Instance.GetMasterLogAttachmentId(masterLogId);
      return GetAttachmentsByHeadeId(headerId);
    }

    internal static IEnumerable<AttachInfoGroupModel> GetGroupsForDataEntry(
      long attachmentHeaderId)
    {
      AttachmentGroupInfo allAttachmentGroup = AttachmentGroup.Instance.GetAllAttachmentGroup();
      if (allAttachmentGroup.AttachmentGroup.Count== 0)
        throw new ApplicationException("گروهی برای ثبت ضمائم ثبت نشده است");
      return Utils.CreateListFromTable<AttachInfoGroupModel>(allAttachmentGroup.AttachmentGroup);
    }

    internal static string GetUrlForDownload(long attachmentId, string token)
    {
      AttachmentsInfo attachmentById = Attachments.GetAttachmentById(attachmentId);
      byte[] attachmentData = attachmentById.Attachments[0].AttachmentData;
      string ext = attachmentById.Attachments[0].AttachmentExtension.Replace(".", "");
      return Utils.CreateTempFileForRead(attachmentData, "atc" + attachmentId, ext, token, true);
    }

    internal static long SaveDataEntry(AttachmentInfoDataEntryModel model)
    {
      AttachmentsInfo attachmentsInfo;
      AttachmentsInfo.AttachmentsRow attachmentsRow;
      if (model.isNew)
      {
        attachmentsInfo = new AttachmentsInfo();
        attachmentsRow = attachmentsInfo.Attachments.NewAttachmentsRow();
      }
      else
      {
        attachmentsInfo = Attachments.GetAttachmentById(model.attachInfo.attachmentId);
        attachmentsRow = attachmentsInfo.Attachments[0];
      }
      attachmentsRow.AttachmentGroupId=model.attachInfo.attachmentGroupId.Value;
      attachmentsRow.AttachmentDescription=model.attachInfo.attachmentDescription;
      attachmentsRow.AttachmentExtension=model.attachInfo.attachmentExtension;
      attachmentsRow.AttachmentsHeaderId=model.attachInfo.attachmentHeaderId;
      attachmentsRow.AttachmentTitle=model.attachInfo.attachmentTitle;
      attachmentsRow.IsScan=false;
      attachmentsRow.IsTemplate=false;
      attachmentsRow.IsGlobal=model.attachInfo.isGlobal;
      if (!string.IsNullOrEmpty(model.attachInfo.attachData))
      {
        string tempFileForRead = Utils.
            CreateTempFileForRead(model.attachInfo.attachData, "attach", 
                model.attachInfo.attachmentExtension.Replace(".", ""), model.token);
        attachmentsRow.AttachmentData=File.ReadAllBytes(tempFileForRead);
      }
      if (model.isNew)
      {
        attachmentsInfo.Attachments.AddAttachmentsRow(attachmentsRow);
        Attachments.CreateAttachment(attachmentsInfo);
        return attachmentsInfo.Attachments[0].AttachmentId;
      }
      Attachments.UpdateAttachment(attachmentsInfo);
      return model.attachInfo.attachmentId;
    }

    internal static void DeleteAttachment(long attachId)
    {
      Attachments.DeleteAttachments(Attachments.GetAttachmentById(attachId));
    }
  }
}
