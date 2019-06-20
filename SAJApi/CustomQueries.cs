using System.Data;
using SepandAsa.Bazargani.Common.DataAccess.PurchaseSourceManagement;
using SepandAsa.Bazargani.Common.Domain.PurchaseSourceManagement;
using SepandAsa.Shared.DataAccess;

namespace SAJApi
{
    public static class CustomQueries
    {
        public static ManbaKharidsInfo GetManbaKharidByPage(int pageNumber, int pageCount)
        {

            using(new DbConnectionScope())
            {
                ManbaKharidsInfo ds = new ManbaKharidsInfo();
                using (var result = new ManbaKharidsDB().GetExexuteReader(
                    $"SELECT ManbaKharidId,MabaKharidTypeId,ManbaKharidTypeName,PurchaseSourceName,Address FROM viewManbaKharids ORDER BY ManbaKharidId DESC OFFSET {pageCount * (pageNumber - 1)} ROWS FETCH NEXT {pageCount} ROWS ONLY",
                    CommandType.Text, null))
                {
                    while (result.Read())
                    {                        
                        var row = ds.ManbaKharids.NewManbaKharidsRow();
                        row.ManbaKharidId = result.GetInt32(0);
                        row.MabaKharidTypeId = result.GetInt32(1);
                        row.ManbaKharidTypeName = result.GetString(2);
                        row.PurchaseSourceName = result.GetString(3);
                        row.Address = result.GetString(4);
                        ds.ManbaKharids.AddManbaKharidsRow(row);
                    }
                }

                return ds;

            }
        }
    }
}