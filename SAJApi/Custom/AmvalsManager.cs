// Decompiled with JetBrains decompiler
// Type: SAJApi.Custom.AmvalsManager
// Assembly: SAJApi, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: E75874B1-C49B-4EF7-8C32-B89971E1CBDC
// Assembly location: C:\Users\papinaser\Downloads\SAJApi.dll

using System;
using System.Data;
using SepandAsa.Bazargani.Amval.Business;
using Stimulsoft.Report;

namespace SAJApi.Custom
{
  public class AmvalsManager
  {
    internal static StiReport GetStiReport(string userName, int reportTypeId)
    {
      string str = string.Format("JamdarPersonelNo=N'{0}' OR KeeperPersonelNo=N'{0}' OR TahvilGirandehPersonelNo=N'{0}'", Utils.GetUserFullInfo(userName).EmployeeId);
      AmvalSearchManager amvalSearchManager = new AmvalSearchManager(true);
      amvalSearchManager.Search(str);
      DataView searchDataView = amvalSearchManager.GetSearchDataView();
      if (searchDataView.Table.Rows.Count == 0)
        throw new ApplicationException("اموال در اختیار شما ثبت نگردیده است");
      StiReport report = new AmvalBazarganiSubSystem().GetReport(reportTypeId == 1 ? "rptWebAmvalKolli" : "rptWebAmvalKolliTafkikSakhteman", null, searchDataView);
      if (report == null)
        throw new ApplicationException("گزارش مورد نظر در بانک یافت نشد");
      report.Render();
      return report;
    }
  }
}
