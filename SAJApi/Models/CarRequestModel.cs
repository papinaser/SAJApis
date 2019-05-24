namespace SAJApi.Models
{
  public class CarRequestModel
  {
    public long CarRequestForMissionId { get; set; }

    public string RequestDate { get; set; }

    public string RequestTime { get; set; }

    public string UsePurpose { get; set; }

    public int CarTypeId { get; set; }

    public string MissionStartDate { get; set; }

    public string MissionStartTime { get; set; }

    public string MissionEndDate { get; set; }

    public string MissionEndTime { get; set; }

    public bool MoveFromHome { get; set; }

    public string RequesterAddress { get; set; }

    public string RequesterPhone { get; set; }

    public string RequetersName { get; set; }

    public string[] Locations { get; set; }

    public string token { get; set; }
  }
}
