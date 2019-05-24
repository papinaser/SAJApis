namespace SAJApi.Models
{
  public class RequestKalaItemModel
  {
    public int RequestKalaItemId { get; set; }

    public int RequestKalaId { get; set; }

    public string ItemDescription { get; set; }

    public float ItemAmount { get; set; }

    public string UnitName { get; set; }

    public int ItemUnitId { get; set; }

    public bool IsTarh { get; set; }

    public int StockId { get; set; }

    public int TarhId { get; set; }

    public int ProjectId { get; set; }

    public int SubProjectId { get; set; }

    public string StockRoomNumber { get; set; }

    public string PlanCode { get; set; }

    public string BuyProjectCode { get; set; }

    public string SubProjectNumber { get; set; }
  }
}
