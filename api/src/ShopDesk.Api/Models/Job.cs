namespace ShopDesk.Api.Models;

public class Job
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Status { get; set; } = JobStatus.New;
    public DateOnly DueDate { get; set; }
    public string Notes { get; set; } = "";
}
