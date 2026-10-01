namespace ShopDesk.Api.Models;

public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Contact { get; set; } = "";
    public List<Job> Jobs { get; set; } = [];
}
