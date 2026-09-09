namespace Zevoryn.Control.Tests;

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Zevoryn.Control.Application.Abstractions;
using Zevoryn.Control.Application.Services;
using Zevoryn.Control.Domain.Entities;
using Zevoryn.Control.Domain.Enums;
using Zevoryn.Control.Infrastructure.Integrations;

public sealed class CleanersFlowAccessTests
{
    [Fact]
    public async Task Without_access_references_only_control_key_is_sent()
    {
        var result = await SendAsync(null, null, new Dictionary<string, string?> { ["control"] = "control-value" });
        Assert.Equal(HttpStatusCode.OK, result.Status); Assert.Equal("control-value", result.Headers.GetValues("X-Zevoryn-Control-Key").Single()); Assert.False(result.Headers.Contains("CF-Access-Client-Id")); Assert.False(result.Headers.Contains("CF-Access-Client-Secret"));
    }

    [Fact]
    public async Task With_both_access_references_all_headers_are_sent()
    {
        var result = await SendAsync("access-id", "access-secret", new Dictionary<string, string?> { ["control"] = "control-value", ["access-id"] = "id-value", ["access-secret"] = "secret-value" });
        Assert.Equal("control-value", result.Headers.GetValues("X-Zevoryn-Control-Key").Single()); Assert.Equal("id-value", result.Headers.GetValues("CF-Access-Client-Id").Single()); Assert.Equal("secret-value", result.Headers.GetValues("CF-Access-Client-Secret").Single());
    }

    [Theory]
    [InlineData("access-id", null)]
    [InlineData(null, "access-secret")]
    public async Task Incomplete_access_configuration_is_rejected_without_request(string? idReference, string? secretReference)
    {
        var handler = new CaptureHandler();
        await Assert.ThrowsAsync<ArgumentException>(() => SendAsync(idReference, secretReference, new Dictionary<string, string?> { ["control"] = "control-value" }, handler));
        Assert.False(handler.Called);
    }

    [Fact]
    public async Task Missing_access_secret_resolution_is_safe_and_does_not_expose_values()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync("access-id", "access-secret", new Dictionary<string, string?> { ["control"] = "control-value", ["access-id"] = null, ["access-secret"] = "secret-value" }));
        Assert.DoesNotContain("secret-value", exception.Message); Assert.DoesNotContain("access-id", exception.Message); Assert.DoesNotContain("access-secret", exception.Message);
    }

    [Fact]
    public void Product_connection_rejects_single_access_reference()
    {
        var environmentId = Guid.NewGuid();
        var idException = Assert.Throws<ArgumentException>(() => ProductConnection.Create(environmentId, ConnectionType.InternalApi, "control", "access-id", null));
        var secretException = Assert.Throws<ArgumentException>(() => ProductConnection.Create(environmentId, ConnectionType.InternalApi, "control", null, "access-secret"));
        Assert.DoesNotContain("access-id", idException.Message); Assert.DoesNotContain("access-secret", secretException.Message);
    }

    private static async Task<CaptureResponse> SendAsync(string? idReference, string? secretReference, Dictionary<string, string?> values, CaptureHandler? handler = null)
    {
        var product = Product.Create("CleanersFlow", "cleanersflow"); var environment = ProductEnvironment.Create(product.Id, "Staging", EnvironmentType.Staging, "https://staging.example.test"); var connection = ProductConnection.Create(environment.Id, ConnectionType.InternalApi, "control", idReference, secretReference);
        var productRepository = new SingleProductRepository(product); var environmentRepository = new SingleEnvironmentRepository(environment); var connectionRepository = new SingleConnectionRepository(connection); var secretProvider = new DictionarySecretProvider(values); handler ??= new CaptureHandler();
        var provider = new CleanersFlowBetaInvitationProvider(new Factory(handler), productRepository, environmentRepository, connectionRepository, secretProvider, new ConfigurationBuilder().Build());
        await provider.CreateInvitationAsync(new(product.Id, environment.Id, "user@example.com", "source"), CancellationToken.None);
        return new CaptureResponse(handler.Status, handler.Headers);
    }

    private sealed record CaptureResponse(HttpStatusCode Status, HttpHeaders Headers);
    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory { public HttpClient CreateClient(string _) => new(handler); }
    private sealed class CaptureHandler : HttpMessageHandler { public bool Called; public HttpStatusCode Status = HttpStatusCode.OK; public HttpHeaders Headers = null!; protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken _) { Called = true; var response = new HttpResponseMessage(Status) { Content = new StringContent($"{{\"id\":\"{Guid.NewGuid()}\",\"email\":\"user@example.com\",\"status\":\"Sent\",\"createdAtUtc\":\"2026-01-01T00:00:00Z\",\"expiresAtUtc\":\"2026-01-02T00:00:00Z\"}}") }; foreach (var header in request.Headers) response.Headers.TryAddWithoutValidation(header.Key, header.Value); Headers = request.Headers; return Task.FromResult(response); } }
    private sealed class DictionarySecretProvider(Dictionary<string, string?> values) : ISecretProvider { public Task<string?> GetSecretAsync(string reference, CancellationToken _) => Task.FromResult(values.GetValueOrDefault(reference)); }
    private sealed class SingleProductRepository(Product product) : IProductRepository { public Task<Product?> GetByIdAsync(Guid id, CancellationToken _) => Task.FromResult<Product?>(id == product.Id ? product : null); public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken _) => Task.FromResult<IReadOnlyList<Product>>([product]); public Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken _) => GetByIdAsync(id, _); public Task<bool> ExistsBySlugAsync(string _, CancellationToken __) => Task.FromResult(false); public Task<bool> ExistsBySlugAsync(string _, Guid __, CancellationToken ___) => Task.FromResult(false); public Task AddAsync(Product _, CancellationToken __) => Task.CompletedTask; public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask; }
    private sealed class SingleEnvironmentRepository(ProductEnvironment environment) : IProductEnvironmentRepository { public Task<ProductEnvironment?> GetByIdAsync(Guid _, Guid id, CancellationToken __) => Task.FromResult<ProductEnvironment?>(id == environment.Id ? environment : null); public Task<IReadOnlyList<ProductEnvironment>> GetByProductIdAsync(Guid _, CancellationToken __) => Task.FromResult<IReadOnlyList<ProductEnvironment>>([environment]); public Task<ProductEnvironment?> GetByIdForUpdateAsync(Guid a, Guid b, CancellationToken c) => GetByIdAsync(a, b, c); public Task<bool> ExistsByNameAsync(Guid _, string __, CancellationToken ___) => Task.FromResult(false); public Task<bool> ExistsByNameAsync(Guid _, string __, Guid ___, CancellationToken ____) => Task.FromResult(false); public Task AddAsync(ProductEnvironment _, CancellationToken __) => Task.CompletedTask; public Task DeleteAsync(ProductEnvironment _, CancellationToken __) => Task.CompletedTask; public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask; }
    private sealed class SingleConnectionRepository(ProductConnection connection) : IProductConnectionRepository { public Task<IReadOnlyList<ProductConnection>> GetByEnvironmentIdAsync(Guid _, CancellationToken __) => Task.FromResult<IReadOnlyList<ProductConnection>>([connection]); public Task<ProductConnection?> GetByIdAsync(Guid _, Guid __, CancellationToken ___) => Task.FromResult<ProductConnection?>(connection); public Task<ProductConnection?> GetByIdForUpdateAsync(Guid _, Guid __, CancellationToken ___) => Task.FromResult<ProductConnection?>(connection); public Task<bool> ExistsByEnvironmentIdAsync(Guid _, CancellationToken __) => Task.FromResult(true); public Task<bool> ExistsByTypeAsync(Guid _, ConnectionType __, Guid? ___, CancellationToken ____) => Task.FromResult(false); public Task AddAsync(ProductConnection _, CancellationToken __) => Task.CompletedTask; public Task DeleteAsync(ProductConnection _, CancellationToken __) => Task.CompletedTask; public Task SaveChangesAsync(CancellationToken _) => Task.CompletedTask; }
}
