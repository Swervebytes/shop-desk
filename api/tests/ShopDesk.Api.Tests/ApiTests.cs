using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopDesk.Api.Data;
using Testcontainers.PostgreSql;

namespace ShopDesk.Api.Tests;

[Collection("api")]
public class ApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ApiFixture _fixture;

    public ApiTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_is_public()
    {
        using var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_a_wrong_password()
    {
        using var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "demo@shopdesk.dev",
            password = "not-the-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("wrong", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Jobs_require_a_signed_in_user()
    {
        using var client = _fixture.Factory.CreateClient();
        var jobs = await client.GetAsync("/api/jobs");
        var clients = await client.GetAsync("/api/clients");
        var create = await client.PostAsJsonAsync("/api/jobs", new { title = "Nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, jobs.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, clients.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Seeded_password_is_stored_as_a_hash()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "demo@shopdesk.dev");

        Assert.NotEqual("demo-shop-desk", user.PasswordHash);
        Assert.DoesNotContain("demo-shop-desk", user.PasswordHash, StringComparison.Ordinal);
        Assert.True(user.PasswordHash.Length > 40);
    }

    [Fact]
    public async Task Seed_loads_three_clients_and_six_jobs()
    {
        using var client = await SignedInClient();
        var clients = await client.GetFromJsonAsync<List<ClientDto>>("/api/clients", Json);
        var jobs = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs", Json);

        Assert.NotNull(clients);
        Assert.Contains(clients, c => c.Name == "Northline Studio" && c.Contact == "ada@northline.example");
        Assert.Contains(clients, c => c.Name == "Harbor & Co" && c.Contact == "sam@harbor.example");
        Assert.Contains(clients, c => c.Name == "Lumen Freight" && c.Contact == "jules@lumen.example");
        Assert.True(clients.Count >= 3);

        Assert.NotNull(jobs);
        string[] seeded =
        [
            "Rewrite the onboarding email",
            "Fix invoice PDF totals",
            "Add a client export",
            "Status page for outages",
            "Migrate tracking webhooks",
            "Close the Q3 billing jobs"
        ];
        foreach (var title in seeded)
            Assert.Contains(jobs, job => job.Title == title);

        var done = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs?status=done", Json);
        Assert.NotNull(done);
        Assert.Contains(done, job => job.Title == "Close the Q3 billing jobs");
        Assert.DoesNotContain(done, job => job.Title == "Rewrite the onboarding email");

        var byClient = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs?q=lumen", Json);
        Assert.NotNull(byClient);
        Assert.Contains(byClient, job => job.Title == "Migrate tracking webhooks");
        Assert.DoesNotContain(byClient, job => job.ClientName == "Harbor & Co");
    }

    [Fact]
    public async Task Create_job_move_it_to_done_and_find_it_by_search()
    {
        using var client = await SignedInClient();
        var clients = await client.GetFromJsonAsync<List<ClientDto>>("/api/clients", Json);
        Assert.NotNull(clients);
        var clientId = clients[0].Id;
        var title = "Lobby window banner " + Guid.NewGuid().ToString("N")[..8];

        var createdResponse = await client.PostAsJsonAsync("/api/jobs", new
        {
            clientId,
            title,
            dueDate = "2026-10-20",
            notes = "Needs a proof from the printer."
        });
        var createdBody = await createdResponse.Content.ReadAsStringAsync();
        Assert.True(createdResponse.StatusCode == HttpStatusCode.Created, createdBody);
        var created = JsonSerializer.Deserialize<JobDto>(createdBody, Json);
        Assert.NotNull(created);
        Assert.Equal(title, created.Title);
        Assert.Equal("new", created.Status);

        var patched = await client.PatchAsJsonAsync($"/api/jobs/{created.Id}", new
        {
            status = "done",
            notes = "Shipped. The client signed off."
        });
        var patchedBody = await patched.Content.ReadAsStringAsync();
        Assert.True(patched.IsSuccessStatusCode, patchedBody);
        var updated = JsonSerializer.Deserialize<JobDto>(patchedBody, Json);
        Assert.NotNull(updated);
        Assert.Equal("done", updated.Status);
        Assert.Equal("Shipped. The client signed off.", updated.Notes);

        var found = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs?q=" + Uri.EscapeDataString(title), Json);
        Assert.NotNull(found);
        var match = Assert.Single(found);
        Assert.Equal(created.Id, match.Id);
        Assert.Equal("done", match.Status);

        var missing = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs?q=zzzz-no-such-job", Json);
        Assert.NotNull(missing);
        Assert.Empty(missing);
    }

    [Fact]
    public async Task Add_client_and_reject_a_bad_status()
    {
        using var client = await SignedInClient();
        var name = "Paper Route " + Guid.NewGuid().ToString("N")[..6];
        var created = await client.PostAsJsonAsync("/api/clients", new
        {
            name,
            contact = "hello@paperroute.example"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var clients = await client.GetFromJsonAsync<List<ClientDto>>("/api/clients", Json);
        Assert.NotNull(clients);
        Assert.Contains(clients, c => c.Name == name && c.Contact == "hello@paperroute.example");

        var jobs = await client.GetFromJsonAsync<List<JobDto>>("/api/jobs", Json);
        Assert.NotNull(jobs);
        var target = jobs[0];
        var bad = await client.PatchAsJsonAsync($"/api/jobs/{target.Id}", new { status = "shipped" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    private async Task<HttpClient> SignedInClient()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "demo@shopdesk.dev",
            password = "demo-shop-desk"
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        Assert.DoesNotContain("demo-shop-desk", body, StringComparison.Ordinal);
        var login = JsonSerializer.Deserialize<LoginDto>(body, Json);
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }

    private sealed record LoginDto(string Token, string Email);
    private sealed record ClientDto(Guid Id, string Name, string Contact);
    private sealed record JobDto(Guid Id, Guid ClientId, string ClientName, string Title, string Status, DateOnly DueDate, string Notes);
}

public sealed class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("shopdesk")
            .WithUsername("shopdesk")
            .WithPassword("shopdesk")
            .Build();
        await _postgres.StartAsync();

        var connectionString = _postgres.GetConnectionString();
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", connectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-shop-desk-jwt-key-32chars!");
        Factory = new ShopApiFactory(connectionString);
        using var client = Factory.CreateClient();
        var health = await client.GetAsync("/api/health");
        health.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }
}

public sealed class ShopApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = connectionString,
                ["Jwt:Key"] = "test-only-shop-desk-jwt-key-32chars!",
                ["Jwt:Issuer"] = "shop-desk",
                ["Jwt:Audience"] = "shop-desk"
            });
        });
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
