namespace SAJApi.Models
{
  public class SaveRequestKalaModel
  {
    public string username { get; set; }

    public RequestKalaModel header { get; set; }

    public RequestKalaItemModel[] items { get; set; }
  }
}
