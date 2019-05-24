namespace SAJApi.Models
{
  public class TarabariCarRequestModel
  {
    public int tarabariCarRequestId { get; set; }

    public string sendDate { get; set; }

    public string sendTime { get; set; }

    public string userPurpose { get; set; }

    public string carTypeGeneralNameTitle { get; set; }

    public int generalCarTypeId { get; set; }

    public string missionTypeName { get; set; }

    public int missionTypeId { get; set; }

    public string missionStartDate { get; set; }

    public string missionStartTime { get; set; }

    public string missionEndDate { get; set; }

    public string missionEndTime { get; set; }

    public bool moveFromHome { get; set; }

    public string requesterAddress { get; set; }

    public string requesterPhone { get; set; }

    public string requetersName { get; set; }

    public string permissiverNameDescription { get; set; }

    public string destinations { get; set; }

    public string userFullName { get; set; }

    public int creatorUserId { get; set; }

    public int attachmentsHeaderId { get; set; }

    public int userName { get; set; }

    public int carRequestForMissionId { get; set; }
  }
}
