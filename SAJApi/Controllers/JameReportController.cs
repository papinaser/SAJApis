using SAJApi.Custom;
using SAJApi.Models;
using SepandAsa.RepairManagment.Business;
using SepandAsa.Shared.Business;
using SepandAsa.Shared.Business.Common;
using SepandAsa.Shared.Business.Reports;
using SepandAsa.Shared.Business.SubSystemManagement;
using SepandAsa.Shared.Domain;
using SepandAsa.Shared.Domain.Reports;
using SepandAsa.Shared.Domain.SubSystemManagement;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SAJApi.Controllers
{
  [EnableCors("http://localhost:8100,http://91.98.153.26:3000,http://192.168.1.8:3000,http://172.20.0.245:888", "*", "*")]
  public class JameReportController : ApiController
  {
    [Route("api/JameReport/GetReportTree/{token}")]
    [HttpGet]
    public SimpleResult GetReportTree(string token)
    {
      try
      {
        Utils.AutoLoginWithToken(token, this.Request.GetClientIpAddress());
        JameReportGroupsInfo currentGroups = JameReportGroupsManager.Instance.GetCurrentGroups((ISubSystem) RepairManagmentSubSystem.Instance, false);
        List<treeModel> result = new List<treeModel>();
        using (IEnumerator<JameReportGroupsInfo.JameReportGroupsRow> enumerator = ((IEnumerable<JameReportGroupsInfo.JameReportGroupsRow>) currentGroups.JameReportGroups).Where(r =>
        {
            if (r.IsParentIdNull())
                return r.IsReportIdNull();
            return false;
        }).GetEnumerator())
        {
          while (((IEnumerator) enumerator).MoveNext())
            this.AddNodes(enumerator.Current, result, currentGroups);
        }
        return new SimpleResult()
        {
          result = "200",
          message = (object) result
        };
      }
      catch (Exception ex)
      {
        Utils.log.Error((object) ex, ex);
        return new SimpleResult()
        {
          result = "500",
          message = (object) ex.Message
        };
      }
    }

    private void AddNodes(
      JameReportGroupsInfo.JameReportGroupsRow parent,
      List<treeModel> result,
      JameReportGroupsInfo allTree)
    {
      treeModel treeModel = new treeModel()
      {
        id = parent.JameReportGroupId.ToString(),
        title = parent.GroupName
      };
      treeModel.hasChildren = parent.IsReportIdNull();
      parent.IsReportIdNull();
      IEnumerable<JameReportGroupsInfo.JameReportGroupsRow> jameReportGroupsRows = ((IEnumerable<JameReportGroupsInfo.JameReportGroupsRow>) allTree.JameReportGroups).Where<JameReportGroupsInfo.JameReportGroupsRow>((Func<JameReportGroupsInfo.JameReportGroupsRow, bool>) (r =>
      {
        if (!r.IsParentIdNull())
          return r.ParentId == parent.JameReportGroupId;
        return false;
      }));
      List<treeModel> result1 = new List<treeModel>();
      using (IEnumerator<JameReportGroupsInfo.JameReportGroupsRow> enumerator = jameReportGroupsRows.GetEnumerator())
      {
        while (((IEnumerator) enumerator).MoveNext())
          this.AddNodes(enumerator.Current, result1, allTree);
      }
      treeModel.childs = result1;
      result.Add(treeModel);
    }

    [Route("api/JameReports/VerifyGetReport/{startDate}/{endDate}/{reportId}")]
    [HttpGet]
    public SimpleResult VerifyGetReport(
      string startDate,
      string endDate,
      string reportId)
    {
      SimpleResult simpleResult = new SimpleResult();
      try
      {
        startDate = startDate.Replace("_", "/");
        endDate = endDate.Replace("_", "/");
        long num = long.Parse(reportId);
        JameReportGroupsInfo dataById = ((BusinessManager) JameReportGroupsManager.Instance).GetDataById((object) num) as JameReportGroupsInfo;
        JameReportGroupsInfo.JameReportGroupsRow jameReportGroupsRow = dataById.JameReportGroups[0];
        if (dataById.JameReportGroups.Count == 0)
        {
          simpleResult.result = "500.17";
          simpleResult.message = (object) "شناسه گزارش نامعتبر است";
        }
        else if (jameReportGroupsRow.IsReportIdNull())
        {
          simpleResult.result = "500.17";
          simpleResult.message = (object) "لطفا یک گزارش از درختواره انتخاب کنید";
        }
        SubSystemSpecialViewInfo.SubSystemSpecialViewRow systemSpecialViewRow = ((SubSystemSpecialViewManager) SubSystemSpecialViewManager.Instance).GetById(jameReportGroupsRow.SpecialViewId).SubSystemSpecialView[0];
        if (((IEnumerable<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow>) ((SubSystemSpecialViewManager) SubSystemSpecialViewManager.Instance).GetSpecialViewDefinition(systemSpecialViewRow).SubSystemSpecialViewDefinition).All<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow>((Func<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow, bool>) (r =>
        {
          if (!r.IsIsColDateNull())
            return !r.IsColDate;
          return true;
        })))
        {
          simpleResult.result = "500.17";
          simpleResult.message = (object) "فیلد تاریخ در تعاریف ویوی خاص مشخص نشده است";
        }
        if (startDate.Split('/')[0] != endDate.Split('/')[0])
        {
          simpleResult.result = "500.17";
          simpleResult.message = (object) "باید سال تاریخ شروع و پایان یکی باشند";
        }
        if (string.IsNullOrEmpty(simpleResult.result))
          simpleResult.result = "200";
      }
      catch (Exception ex)
      {
        simpleResult.result = "500.17";
        simpleResult.message = (object) ex.Message;
      }
      return simpleResult;
    }

    [Route("api/JameReports/GetReport/{startDate}/{endDate}/{reportId}/{token}")]
    [HttpGet]
    public IHttpActionResult GetReport(
      string startDate,
      string endDate,
      string reportId,
      string token)
    {
      return this.GetReportOrXml(token, startDate, endDate, reportId, false);
    }

    [Route("api/JameReports/GetReportXml/{startDate}/{endDate}/{reportId}/{token}")]
    [HttpGet]
    public IHttpActionResult GetReportXml(
      string startDate,
      string endDate,
      string reportId,
      string token)
    {
      return this.GetReportOrXml(token, startDate, endDate, reportId, true);
    }

    private IHttpActionResult GetReportOrXml(
      string token,
      string startDate,
      string endDate,
      string reportId,
      bool xml = false)
    {
      startDate = startDate.Replace("_", "/");
      endDate = endDate.Replace("_", "/");
      long num1 = long.Parse(reportId);
      JameReportGroupsInfo.JameReportGroupsRow 
          jameReportGroupsRow = (((BusinessManager) JameReportGroupsManager.Instance)
              .GetDataById((object) num1) as JameReportGroupsInfo).JameReportGroups[0];
      string str1 = "";
      if (jameReportGroupsRow != null && !jameReportGroupsRow.IsReportIdNull())
      {
        SubSystemSpecialViewInfo.SubSystemSpecialViewRow systemSpecialViewRow = ((SubSystemSpecialViewManager) SubSystemSpecialViewManager.Instance).GetById(jameReportGroupsRow.SpecialViewId).SubSystemSpecialView[0];
        ReportInfo byReportId = ((ReportMaintenance) ReportMaintenance.Instance).GetByReportId(jameReportGroupsRow.ReportId);
        SubSystemSpecialViewManager.SpecialViewSearchManager viewSearchManager = ((SubSystemSpecialViewManager) SubSystemSpecialViewManager.Instance).GetSpecialViewSearchManager(systemSpecialViewRow);
        SubSystemSpecialViewInfo specialViewDefinition = ((SubSystemSpecialViewManager) SubSystemSpecialViewManager.Instance).GetSpecialViewDefinition(systemSpecialViewRow);
        if (((IEnumerable<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow>) specialViewDefinition.SubSystemSpecialViewDefinition).Any<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow>((Func<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow, bool>) (r =>
        {
          if (!r.IsIsColDateNull())
            return r.IsColDate;
          return false;
        })) && !byReportId.Report[0].IsIsDateRangeReportNull() && byReportId.Report[0].IsDateRangeReport)
        {
          int num2 = byReportId.Report[0].IsDateRangeTypeIdNull() ? 0 : byReportId.Report[0].DateRangeTypeId;
          if (startDate.Split('/')[0] != endDate.Split('/')[0])
            throw new ApplicationException("باید سال تاریخ شروع و پایان یکی باشند");
          string newValue = this.SetJameReportFromYear(startDate.Split('/')[0], byReportId.Report[0]);
          string str2 = newValue + "/01/01";
          string oldValue = startDate.Split('/')[0];
          string str3 = startDate.Replace(oldValue, newValue);
          string str4 = endDate.Replace(oldValue, newValue);
          SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow viewDefinitionRow = ((IEnumerable<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow>) specialViewDefinition.SubSystemSpecialViewDefinition).First<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow>((Func<SubSystemSpecialViewInfo.SubSystemSpecialViewDefinitionRow, bool>) (r =>
          {
            if (!r.IsIsColDateNull())
              return r.IsColDate;
            return false;
          }));
          str1 = viewDefinitionRow.ColumName;
          string str5 = string.Format("({0}>='{1}' AND {0} <='{2}')",viewDefinitionRow.ColumName, startDate, endDate);
          if (num2 == 9)
            str5 = string.Format("{0} OR ({1}>='{2}' AND {1} <='{3}')", str5, viewDefinitionRow.ColumName, str3, (object) str4);
          else if (byReportId.Report[0].IsFilterNotNullsBeforeRangeNull() || string.IsNullOrWhiteSpace(byReportId.Report[0].FilterNotNullsBeforeRange))
          {
            if (byReportId.Report[0].ReportType == 1)
              str5 = string.Format("{0} OR ({1}>='{2}' AND {1}<='{3}')", str5, viewDefinitionRow.ColumName, str2, (object) startDate);
          }
          else
          {
            string str6 = string.Join(" IS NOT NULL OR ", byReportId.Report[0].FilterNotNullsBeforeRange.Split(',')) + " IS NOT NULL";
            str5 = string.Format("({0} OR ({1}>=N'{2}' AND {1}<=N'{3}')) AND ({4})", (object) str5, (object) viewDefinitionRow.ColumName, (object) str2, (object) startDate, (object) str6);
          }
          ((BaseSearchManager) viewSearchManager).Search(str5);
          DataView searchDataView = ((BaseSearchManager) viewSearchManager).GetSearchDataView();
          if (xml)
          {
            string fileFullPathAppData = Utils.GetFileFullPathAppData("temp" + token + ".xml");
            FileStream fileStream = new FileStream(fileFullPathAppData, FileMode.Create);
            searchDataView.Table.WriteXml((Stream) fileStream, XmlWriteMode.IgnoreSchema);
            fileStream.Close();
            return (IHttpActionResult) new FileResult(fileFullPathAppData, (string) null);
          }
          ReportInfo.ReportRow reportRow = byReportId.Report[0];
          StiReport report = new StiReport();
          report.Load(reportRow.bineryReport);
          if (!report.IsCompiled)
            report.Compile();
          if (!string.IsNullOrEmpty(startDate))
            byReportId.FilterRow.AddFilterRowRow(startDate, endDate);
          ReportInfo.FilterRowRow filterRowRow = byReportId.FilterRow.Count > 0 ? byReportId.FilterRow[0] : (ReportInfo.FilterRowRow) null;
          RepairManagmentSubSystem.Instance.CustomizeReport(byReportId.Report[0], report, (DataRow) filterRowRow, searchDataView);
          SepandAsa.Shared.Business.Reports.Report.SetCurDateTimeInReport(report);
          if (report.IsVariableExist("vStartDate"))
            report["vStartDate"]= startDate;
          if (report.IsVariableExist("vEndDate"))
            report["vEndDate"]=endDate;
          if (report.IsVariableExist("vMainYear"))
            report["vMainYear"]= int.Parse(endDate.Split('/')[0]);
          foreach (StiPage page in report.CompiledReport.Pages)
            page.Rendering+=page_Rendering;
          report.Render();
          string tempFileName = Utils.GetTempFileName(token, "rpt", "pdf");
          return (IHttpActionResult) new FileResult(Utils.ConvertStiToPdf(report, tempFileName), (string) null);
        }
      }
      return (IHttpActionResult) null;
    }

    private void page_Rendering(object sender, EventArgs e)
    {
//      StiPage stiPage = sender as StiPage;
//      stiPage.get_Watermark().set_ImageAlignment(ContentAlignment.BottomCenter);
//      stiPage.get_Watermark().set_ShowBehind(false);
//      stiPage.get_Watermark().set_ShowImageBehind(false);
//      StiBrush stiBrush = (StiBrush) new StiSolidBrush(Color.FromArgb(50, 0, 0, 0));
//      stiPage.get_Watermark().set_TextBrush(stiBrush);
    }

    private string SetJameReportFromYear(string year, ReportInfo.ReportRow reportRow)
    {
      if (reportRow.IsMultiYearCountNull())
        return year;
      return (int.Parse(year) - (reportRow.MultiYearCount - 1)).ToString();
    }

    public JameReportController()
    {
      
    }
  }
}
