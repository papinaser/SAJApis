namespace SAJApi.Models
{
  public class ManbaKharidFormModel
  {
    public int selectedId { get; set; }

    public string token { get; set; }

    public bool isEdit { get; set; }

    public manbaKharidModel data { get; set; }
  }
}
