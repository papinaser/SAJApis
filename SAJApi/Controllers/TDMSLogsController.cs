// Decompiled with JetBrains decompiler
// Type: SAJApi.Controllers.TDMSLogsController
// Assembly: SAJApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: E75874B1-C49B-4EF7-8C32-B89971E1CBDC
// Assembly location: C:\Users\papinaser\Downloads\SAJApi.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.RepairManagment.Business;
using SepandAsa.RepairManagment.Domain;
using SepandAsa.Shared.Business.Reports;
using SepandAsa.Shared.Domain.Reports;

namespace SAJApi.Controllers
{
    [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://localhost:3000,http://172.20.0.245:888", "*", "*")]
    public class TDMSLogsController : ApiController
  {
    [Route("api/TDMSLogs/GetAttachments/{mrId}/{token}")]
    [HttpGet]
    public SimpleResult GetAttachments(string mrId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        IEnumerable<AttachmentGroupModel> attachmentGroupModels = new List<AttachmentGroupModel>();
        long headerId;
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
          attachmentGroupModels = tdmsMojodiats.GetAttachments(mrId, out headerId);
        DataRowAttachmentsModel attachmentsModel = new DataRowAttachmentsModel
        {
          headerId = headerId,
          list = attachmentGroupModels
        };
        return new SimpleResult
        {
          result = "200",
          message = attachmentsModel
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.19",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/GetLogFormReports/{mrId}/{rcId}/{setTypeItemId}/{token}")]
    [HttpGet]
    public SimpleResult GetLogFormReports(
      string mrId,
      string rcId,
      string setTypeItemId,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        string str = string.Format("frmSearchLogInLevelstbl{0}tbl{1}", setTypeItemId, rcId);
        ReportInfo allReports = new ReportMaintenance(RepairManagmentSubSystem.Instance, str).GetAllReports(false);
        List<KeyValueModel> keyValueModelList = new List<KeyValueModel>();
        foreach (ReportInfo.ReportRow reportRow in allReports.Report)
        {
            keyValueModelList.Add(new KeyValueModel
            {
                value = reportRow.ReportId.ToString(),
                label = reportRow.ReportTitle
            });
        }       
        return new SimpleResult
        {
          result = "200",
          message = keyValueModelList
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.19",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/GetDetailLogReport")]
    [HttpPost]
    public FileResult GetDetailLogReport([FromBody] GetDetailLogReportModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
          return new FileResult(tdmsMojodiats.GetDetailLogReport(model.modelRelationId, 
              model.roykardId, model.setTypeItemId, model.reportId, model.tblIds, model.token),
              "application/pdf");
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        new HttpRequestMessage().CreateResponse(HttpStatusCode.ExpectationFailed, new SimpleResult
        {
            result = "500.20",
            message = ex.Message
        });        
        return null;
      }
    }

    [Route("api/TDMSLogs/UploadExcelLog")]
    [HttpPost]
    public SimpleResult UploadExcelLog([FromBody] UploadLogExcelModel model)
    {
      try
      {
        Utils.AutoLoginWithToken(model.token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          tdmsMojodiats.UploadExcelLog(model);
          if (tdmsMojodiats.lstErrs.Count == 0)
          {
            if (tdmsMojodiats.countInserted > 0)
              return new SimpleResult
              {
                result = "200",
                message = string.Format("تعداد {0} لاگ با موفقیت ذخیره شد", tdmsMojodiats.countInserted)
              };
            return new SimpleResult
            {
              result = "200",
              message = "لاگ جدید با موفقیت ذخیره شد"
            };
          }
          return new SimpleResult
          {
            result = "500.15",
            message = string.Join(Environment.NewLine, tdmsMojodiats.lstErrs)
          };
        }
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.16",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/InitForEntryLog/{mrId}/{rcId}/{mlId}/{token}")]
    [HttpGet]
    public SimpleResult InitForEntryLog(
      string mrId,
      string rcId,
      string mlId,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          tdmsLogModel tdmsLogModel = tdmsMojodiats.InitForEntryLog(long.Parse(tdmsMojodiats.Decrypt(mrId)), long.Parse(rcId), long.Parse(mlId));
          return new SimpleResult
          {
            result = "200",
            message = tdmsLogModel
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          result = "500.22",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/LookupDataSource/{mrId}/{rcId}/{paramName}/{token}")]
    [HttpGet]
    public SimpleResult LookupDataSource(
      string mrId,
      string rcId,
      string paramName,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          IEnumerable<ListItemModel> listItemModels = tdmsMojodiats.LookupDataSource(long.Parse(tdmsMojodiats.Decrypt(mrId)), long.Parse(rcId), paramName);
          return new SimpleResult
          {
            result = "200",
            message = listItemModels
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          result = "500.24",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/SaveLogDataEntry")]
    [HttpPost]
    public SimpleResult SaveLogDataEntry([FromBody] SaveDataEntryModel saveModel)
    {
      try
      {
        Utils.AutoLoginWithToken(saveModel.token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          long num = tdmsMojodiats.SaveLogDataEntry(saveModel);
          return new SimpleResult
          {
            result = "200",
            message = num
          };
        }
      }
      catch (Exception ex)
      {
        Utils.log.Error(ex.Message, ex);
        return new SimpleResult
        {
          result = "500.25",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/ValidateNewLog/{mrId}/{rcId}/{token}")]
    [HttpGet]
    public SimpleResult ValidateNewLog(string mrId, string rcId, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        string fileFullPathAppData1 = Utils.GetFileFullPathAppData(rcId + "FormTemplate.xlsm");
        if (!File.Exists(fileFullPathAppData1))
          throw new ApplicationException("فایل ورود اطلاعات برای این فرم لاگ تعریف نشده");
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          string logForm = tdmsMojodiats.GetLogForm(token);
          string fileFullPathAppData2 = Utils.GetFileFullPathAppData(logForm);
          File.Copy(fileFullPathAppData1, fileFullPathAppData2, true);
          return new SimpleResult
          {
            result = "200",
            message = logForm
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          result = "500.13",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/DeleteLog/{masterLog}/{token}")]
    [HttpPost]
    public SimpleResult DeleteLog(string masterLog, string token)
    {
      long num = long.Parse(masterLog);
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        MasterLogInfo masterLogById = MasterLog.Instance.GetMasterLogById(num);
        MasterLog.Instance.SurveyAndDeleteMasterLogRow(masterLogById.MasterLog[0], true, true);
        return new SimpleResult
        {
          result = "200",
          message = "حذف با موفقیت انجام شد"
        };
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          result = "500.14",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/SaveEditLog/{mlId}/{mrId}/{rcId}/{fileName}/{token}")]
    [HttpPost]
    public SimpleResult SaveEditLog(
      string mlId,
      string mrId,
      string rcId,
      string fileName,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        long num = long.Parse(mlId);
        MasterLogInfo masterLogById = MasterLog.Instance.GetMasterLogById(num);
        MasterLog.Instance.SurveyAndDeleteMasterLogRow(masterLogById.MasterLog[0], false, false);
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          fileName = Utils.GetFileFullPathAppData(fileName);
          tdmsMojodiats.ImportLog(long.Parse(tdmsMojodiats.Decrypt(mrId)), long.Parse(rcId), fileName, 0L, masterLogById);
          if (tdmsMojodiats.lstErrs.Count == 0)
          {
            if (tdmsMojodiats.countInserted > 0)
              return new SimpleResult
              {
                result = "200",
                message = string.Format("تعداد {0} لاگ با موفقیت ویرایش شد", tdmsMojodiats.countInserted)
              };
            return new SimpleResult
            {
              result = "200",
              message = string.Format("لاگ {0} با موفقیت ویرایش شد", num)
            };
          }
          return new SimpleResult
          {
            result = "500.15",
            message = string.Join(Environment.NewLine, tdmsMojodiats.lstErrs)
          };
        }
      }
      catch (Exception ex)
      {
        string str = ex.InnerException != null ? ex.InnerException.Message : "";
        return new SimpleResult
        {
          result = "500.16",
          message = ex.Message + "-" + ex.StackTrace + "-" + str
        };
      }
    }

    [Route("api/TDMSLogs/ValidateEditLog/{mlId}/{token}")]
    [HttpGet]
    public SimpleResult ValidateEditLog(string mlId, string token)
    {
      long num = long.Parse(mlId);
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        MasterLogInfo masterLogById = MasterLog.Instance.GetMasterLogById(num);
        string fileFullPathAppData1 = Utils.GetFileFullPathAppData(masterLogById.MasterLog[0].RoykardConfigId + "FormTemplate.xlsm");
        if (!File.Exists(fileFullPathAppData1))
          throw new ApplicationException("فایل ورود اطلاعات برای این فرم لاگ تعریف نشده");
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          string logForm = tdmsMojodiats.GetLogForm(token);
          string fileFullPathAppData2 = Utils.GetFileFullPathAppData(logForm);
          File.Copy(fileFullPathAppData1, fileFullPathAppData2, true);
          ExcelExport.Instance.Export(masterLogById.MasterLog[0].RoykardConfigId, masterLogById.MasterLog[0].RoykardConfigId, masterLogById.MasterLog[0].MasterLogId, fileFullPathAppData2);
          Console.WriteLine("****** Finished Export************");
          return new SimpleResult
          {
            result = "200",
            message = logForm
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          result = "500.17",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/SaveNewLog/{mrId}/{rcId}/{fileName}/{token}")]
    [HttpPost]
    public SimpleResult SaveNewLog(
      string mrId,
      string rcId,
      string fileName,
      string token)
    {
      try
      {
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
          fileName = Utils.GetFileFullPathAppData(fileName);
          tdmsMojodiats.ImportLog(long.Parse(tdmsMojodiats.Decrypt(mrId)), long.Parse(rcId), fileName, 0L, null);
          if (tdmsMojodiats.lstErrs.Count == 0)
          {
            if (tdmsMojodiats.countInserted > 0)
              return new SimpleResult
              {
                result = "200",
                message = string.Format("تعداد {0} لاگ با موفقیت ذخیره شد", tdmsMojodiats.countInserted)
              };
            return new SimpleResult
            {
              result = "200",
              message = "لاگ جدید با موفقیت ذخیره شد"
            };
          }
          return new SimpleResult
          {
            result = "500.15",
            message = string.Join(Environment.NewLine, tdmsMojodiats.lstErrs)
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          result = "500.16",
          message = ex.Message
        };
      }
    }

    [Route("api/TDMSLogs/InitialLogList/{mrId}/{rcId}/{viewType}/{setTypeItem}/{filterYear}/{token}")]
    [HttpGet]
    public SimpleResult InitialLogList(
      string mrId,
      string rcId,
      string viewType,
      string setTypeItem,
      string filterYear,
      string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          AgGridModel agGridModel = tdmsMojodiats.InitialLogList(mrId, rcId, viewType, setTypeItem, filterYear);
          return new SimpleResult
          {
            message = agGridModel,
            result = "200"
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.6"
        };
      }
    }

    [Route("api/TDMSLogs/GetRoykardConfigs/{id}")]
    [HttpGet]
    public string GetRoykardConfigs(string id)
    {
      SimpleResult simpleResult = null;
      try
      {
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          List<KeyValueModel> roykardConfigs = tdmsMojodiats.GetRoykardConfigs(id, false);
          simpleResult = new SimpleResult
          {
            message = roykardConfigs,
            result = "200"
          };
        }
      }
      catch (Exception ex)
      {
        simpleResult = new SimpleResult
        {
          message = ex.Message,
          result = "500.5"
        };
      }
      return JsonConvert.SerializeObject(simpleResult);
    }

    [Route("api/TDMSLogs/GetRoykardConfigsForReport/{mrId}")]
    [HttpGet]
    public string GetRoykardConfigsForReport(string mrId)
    {
      SimpleResult simpleResult = null;
      try
      {
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          List<KeyValueModel> roykardConfigs = tdmsMojodiats.GetRoykardConfigs(mrId, true);
          simpleResult = new SimpleResult
          {
            message = roykardConfigs,
            result = "200"
          };
        }
      }
      catch (Exception ex)
      {
        simpleResult = new SimpleResult
        {
          message = ex.Message,
          result = "500.5"
        };
      }
      return JsonConvert.SerializeObject(simpleResult);
    }

    [Route("api/TDMSLogs/GetRoyardConfigLogTypes/{id}")]
    [HttpGet]
    public SimpleResult GetRoyardConfigLogTypes(string id)
    {
      try
      {
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          List<KeyValueModel> royardConfigLogTypes = tdmsMojodiats.GetRoyardConfigLogTypes(id);
          return new SimpleResult
          {
            message = royardConfigLogTypes,
            result = "200"
          };
        }
      }
      catch (Exception ex)
      {
        return new SimpleResult
        {
          message = ex.Message,
          result = "500.5"
        };
      }
    }

    [Route("api/TDMSLogs/GetMojodiatsTreeNodes/{id}/{token}")]
    [HttpGet]
    public string GetMojodiatsTreeNodes(string id, string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, Request.GetClientIpAddress());
        using (TDMSMojodiats tdmsMojodiats = new TDMSMojodiats())
        {
          ModelRelationInfo mojodiats = tdmsMojodiats.GetMojodiats(id);
          List<treeModel> treeModelList = new List<treeModel>();
          using (IEnumerator<ModelRelationInfo.ModelRelationRow> enumerator =
               mojodiats.ModelRelation.GetEnumerator())
          {
            while (enumerator.MoveNext())
            {
              ModelRelationInfo.ModelRelationRow current = enumerator.Current;
              treeModelList.Add(new treeModel
              {
                id = "child-" + tdmsMojodiats.Encrypt(current.ModelRelationId.ToString()),
                title = current.SetItemTypeName + " " + current.ModelRelationName,
                hasChildren = true
              });
            }
          }
          return JsonConvert.SerializeObject(new SimpleResult
          {
              result = "200",
              message = treeModelList
          });
        }
      }
      catch (Exception ex)
      {
        return JsonConvert.SerializeObject(new SimpleResult
        {
            result = "501.2",
            message = ex.Message
        });
      }
    }

    private string Encrypt(string clearText)
    {
      byte[] bytes = Encoding.Unicode.GetBytes(clearText);
      StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
      foreach (byte num in bytes)
        stringBuilder.AppendFormat("{0:X2}", num);
      return stringBuilder.ToString();
    }

    private string Decrypt(string cipherText)
    {
      cipherText = cipherText.Replace("child-", "");
      Encoding unicode = Encoding.Unicode;
      int length = cipherText.Length;
      byte[] bytes = new byte[length / 2];
      for (int startIndex = 0; startIndex < length; startIndex += 2)
        bytes[startIndex / 2] = Convert.ToByte(cipherText.Substring(startIndex, 2), 16);
      return unicode.GetString(bytes);
    }

    
  }
}
