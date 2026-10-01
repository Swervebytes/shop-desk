using Microsoft.AspNetCore.Identity;
using ShopDesk.Api.Models;

namespace ShopDesk.Api.Data;

public static class DbSeeder
{
    public const string DemoEmail = "demo@shopdesk.dev";
    public const string DemoPassword = "demo-shop-desk";

    public static void Seed(ShopDbContext db)
    {
        if (db.Users.Any())
            return;

        var hasher = new PasswordHasher<UserAccount>();
        var user = new UserAccount
        {
            Id = Guid.Parse("8f0c1a2e-6b4d-4c1a-9a11-0a0b0c0d0e01"),
            Email = DemoEmail
        };
        user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        db.Users.Add(user);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var northline = Client("11111111-1111-4111-8111-111111111111", "Northline Studio", "ada@northline.example");
        var harbor = Client("22222222-2222-4222-8222-222222222222", "Harbor & Co", "sam@harbor.example");
        var lumen = Client("33333333-3333-4333-8333-333333333333", "Lumen Freight", "jules@lumen.example");
        db.Clients.AddRange(northline, harbor, lumen);

        db.Jobs.AddRange(
            Job(northline, "Rewrite the onboarding email", JobStatus.InProgress, today.AddDays(5),
                "Copy is still a draft. Waiting on the welcome offer."),
            Job(northline, "Fix invoice PDF totals", JobStatus.Review, today.AddDays(2),
                "Tax line is off by one cent on multi-page invoices."),
            Job(harbor, "Add a client export", JobStatus.New, today.AddDays(12),
                "CSV is enough. They asked for name and contact."),
            Job(harbor, "Status page for outages", JobStatus.InProgress, today.AddDays(-1),
                "List open incidents. No subscriber alerts."),
            Job(lumen, "Migrate tracking webhooks", JobStatus.Review, today.AddDays(1),
                "The old endpoint still receives retries."),
            Job(lumen, "Close the Q3 billing jobs", JobStatus.Done, today.AddDays(-4),
                "Invoices sent. Nothing left to file."));

        db.SaveChanges();
    }

    private static Client Client(string id, string name, string contact) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        Contact = contact
    };

    private static Job Job(Client client, string title, string status, DateOnly due, string notes) => new()
    {
        Id = Guid.NewGuid(),
        Client = client,
        ClientId = client.Id,
        Title = title,
        Status = status,
        DueDate = due,
        Notes = notes
    };
}
