using Microsoft.EntityFrameworkCore;
using ShopDesk.Api.Auth;
using ShopDesk.Api.Data;
using ShopDesk.Api.Models;

namespace ShopDesk.Api.Endpoints;

public static class ApiEndpoints
{
    public static void MapShopDesk(this WebApplication app)
    {
        app.MapGet("/api/health", () => Results.Ok(new { ok = true }));
        app.MapPost("/api/auth/login", Login);

        var clients = app.MapGroup("/api/clients").RequireAuthorization();
        clients.MapGet("", ListClients);
        clients.MapPost("", CreateClient);

        var jobs = app.MapGroup("/api/jobs").RequireAuthorization();
        jobs.MapGet("", ListJobs);
        jobs.MapPost("", CreateJob);
        jobs.MapGet("/{id:guid}", GetJob);
        jobs.MapPatch("/{id:guid}", UpdateJob);

        app.Map("/api/{**catchall}", () => Results.Json(new { error = "Not found." }, statusCode: StatusCodes.Status404NotFound));
    }

    private static async Task<IResult> Login(LoginRequest? body, ShopDbContext db, JwtTokenService tokens)
    {
        var email = body?.Email?.Trim().ToLowerInvariant() ?? "";
        var password = body?.Password ?? "";
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UserAccount>();
        if (user is null
            || hasher.VerifyHashedPassword(user, user.PasswordHash, password) == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
            return Error("Email or password is wrong.", StatusCodes.Status401Unauthorized);

        return Results.Ok(new { token = tokens.Create(user), email = user.Email });
    }

    private static async Task<IResult> ListClients(ShopDbContext db)
    {
        var clients = await db.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ClientResponse(c.Id, c.Name, c.Contact))
            .ToListAsync();
        return Results.Ok(clients);
    }

    private static async Task<IResult> CreateClient(CreateClientRequest? body, ShopDbContext db)
    {
        var name = body?.Name?.Trim() ?? "";
        var contact = body?.Contact?.Trim() ?? "";
        if (name.Length == 0)
            return Error("Name is required.");
        if (name.Length > 200)
            return Error("Name is too long.");
        if (contact.Length == 0)
            return Error("Contact is required.");
        if (contact.Length > 200)
            return Error("Contact is too long.");

        var client = new Client { Id = Guid.NewGuid(), Name = name, Contact = contact };
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return Results.Created($"/api/clients/{client.Id}", new ClientResponse(client.Id, client.Name, client.Contact));
    }

    private static async Task<IResult> ListJobs(string? status, string? q, ShopDbContext db)
    {
        var query = db.Jobs.AsNoTracking().Include(j => j.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalized = JobStatus.Normalize(status);
            if (normalized is null)
                return Error("Status must be new, in_progress, review, or done.");
            query = query.Where(j => j.Status == normalized);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = "%" + EscapeLike(q.Trim()) + "%";
            query = query.Where(j =>
                EF.Functions.ILike(j.Title, pattern, "\\")
                || EF.Functions.ILike(j.Notes, pattern, "\\")
                || EF.Functions.ILike(j.Client.Name, pattern, "\\"));
        }

        var jobs = await query
            .OrderBy(j => j.DueDate)
            .ThenBy(j => j.Title)
            .ToListAsync();

        return Results.Ok(jobs.Select(ToJob).ToList());
    }

    private static async Task<IResult> GetJob(Guid id, ShopDbContext db)
    {
        var job = await db.Jobs.AsNoTracking().Include(j => j.Client).FirstOrDefaultAsync(j => j.Id == id);
        return job is null ? Error("Job not found.", StatusCodes.Status404NotFound) : Results.Ok(ToJob(job));
    }

    private static async Task<IResult> CreateJob(CreateJobRequest? body, ShopDbContext db)
    {
        if (body is null)
            return Error("Title is required.");

        var title = body.Title?.Trim() ?? "";
        if (title.Length == 0)
            return Error("Title is required.");
        if (title.Length > 200)
            return Error("Title is too long.");
        if (body.DueDate is null)
            return Error("Due date is required.");

        var status = string.IsNullOrWhiteSpace(body.Status) ? JobStatus.New : JobStatus.Normalize(body.Status);
        if (status is null)
            return Error("Status must be new, in_progress, review, or done.");

        var notes = body.Notes?.Trim() ?? "";
        if (notes.Length > 4000)
            return Error("Note is too long.");

        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == body.ClientId);
        if (client is null)
            return Error("Client not found.");

        var job = new Job
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            Title = title,
            Status = status,
            DueDate = body.DueDate.Value,
            Notes = notes
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync();
        job.Client = client;
        return Results.Created($"/api/jobs/{job.Id}", ToJob(job));
    }

    private static async Task<IResult> UpdateJob(Guid id, UpdateJobRequest? body, ShopDbContext db)
    {
        if (body is null || (body.Status is null && body.Notes is null && body.Title is null && body.DueDate is null))
            return Error("Nothing to update.");

        var job = await db.Jobs.Include(j => j.Client).FirstOrDefaultAsync(j => j.Id == id);
        if (job is null)
            return Error("Job not found.", StatusCodes.Status404NotFound);

        if (body.Status is not null)
        {
            var status = JobStatus.Normalize(body.Status);
            if (status is null)
                return Error("Status must be new, in_progress, review, or done.");
            job.Status = status;
        }

        if (body.Title is not null)
        {
            var title = body.Title.Trim();
            if (title.Length == 0)
                return Error("Title is required.");
            if (title.Length > 200)
                return Error("Title is too long.");
            job.Title = title;
        }

        if (body.Notes is not null)
        {
            var notes = body.Notes.Trim();
            if (notes.Length > 4000)
                return Error("Note is too long.");
            job.Notes = notes;
        }

        if (body.DueDate is not null)
            job.DueDate = body.DueDate.Value;

        await db.SaveChangesAsync();
        return Results.Ok(ToJob(job));
    }

    private static JobResponse ToJob(Job job) =>
        new(job.Id, job.ClientId, job.Client.Name, job.Title, job.Status, job.DueDate, job.Notes);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private static IResult Error(string message, int status = StatusCodes.Status400BadRequest) =>
        Results.Json(new { error = message }, statusCode: status);
}

public record LoginRequest(string? Email, string? Password);
public record CreateClientRequest(string? Name, string? Contact);
public record ClientResponse(Guid Id, string Name, string Contact);
public record CreateJobRequest(Guid ClientId, string? Title, DateOnly? DueDate, string? Notes, string? Status);
public record UpdateJobRequest(string? Status, string? Notes, string? Title, DateOnly? DueDate);
public record JobResponse(Guid Id, Guid ClientId, string ClientName, string Title, string Status, DateOnly DueDate, string Notes);
