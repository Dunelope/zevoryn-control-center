namespace Zevoryn.Control.Tests;

using System.Net;
using System.Net.Http.Json;
using Zevoryn.Control.Application.Contracts;
using Zevoryn.Control.Domain.Enums;

public sealed class PostgresApiTests(PostgresApiFixture fixture) : IClassFixture<PostgresApiFixture>
{
    [Fact]
    public async Task Product_crud_duplicate_slug_and_deactivation_work()
    {
        var slug = $"test-{Guid.NewGuid():N}";
        var created = await fixture.Client.PostAsJsonAsync("/api/products", new CreateProductRequest("Integration Product", slug, "Integration"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = await created.Content.ReadFromJsonAsync<ProductDto>(); Assert.NotNull(product);

        var duplicate = await fixture.Client.PostAsJsonAsync("/api/products", new CreateProductRequest("Duplicate", slug, null)); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var update = await fixture.Client.PutAsJsonAsync($"/api/products/{product!.Id}", new UpdateProductRequest("Updated Product", slug, "Updated")); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var get = await fixture.Client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}"); Assert.Equal("Updated Product", get!.Name);
        var delete = await fixture.Client.DeleteAsync($"/api/products/{product.Id}"); Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var archived = await fixture.Client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}"); Assert.Equal(ProductStatus.Inactive, archived!.Status);
    }

    [Fact]
    public async Task Environment_and_connection_lifecycle_work_without_secret_values()
    {
        var productResponse = await fixture.Client.PostAsJsonAsync("/api/products", new CreateProductRequest("Environment Product", $"env-{Guid.NewGuid():N}", null));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>(); Assert.NotNull(product);
        var request = new CreateProductEnvironmentRequest("Production", EnvironmentType.Production, "https://example.com");
        var environmentResponse = await fixture.Client.PostAsJsonAsync($"/api/products/{product!.Id}/environments", request); Assert.Equal(HttpStatusCode.OK, environmentResponse.StatusCode);
        var environment = await environmentResponse.Content.ReadFromJsonAsync<ProductEnvironmentDto>(); Assert.NotNull(environment);
        var duplicate = await fixture.Client.PostAsJsonAsync($"/api/products/{product.Id}/environments", request); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var updated = await fixture.Client.PutAsJsonAsync($"/api/products/{product.Id}/environments/{environment!.Id}", new UpdateProductEnvironmentRequest("Production EU", EnvironmentType.Production, "https://example.org")); Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var connection = await fixture.Client.PostAsJsonAsync($"/api/products/{product.Id}/environments/{environment.Id}/connections", new CreateProductConnectionRequest(ConnectionType.InternalApi, "cleanersflow-production-control-api")); Assert.Equal(HttpStatusCode.OK, connection.StatusCode);
        var connectionDto = await connection.Content.ReadFromJsonAsync<ProductConnectionDto>(); Assert.NotNull(connectionDto); Assert.Equal("cleanersflow-production-control-api", connectionDto!.SecretReference); Assert.DoesNotContain("secret-value", connectionDto.SecretReference);
        var deleteEnvironment = await fixture.Client.DeleteAsync($"/api/products/{product.Id}/environments/{environment.Id}"); Assert.Equal(HttpStatusCode.Conflict, deleteEnvironment.StatusCode);
    }

    [Fact]
    public async Task Event_endpoint_persists_against_postgres()
    {
        var productResponse = await fixture.Client.PostAsJsonAsync("/api/products", new CreateProductRequest("Event Product", $"event-{Guid.NewGuid():N}", null));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>(); Assert.NotNull(product);
        var response = await fixture.Client.PostAsJsonAsync("/api/events", new CreateSaaSEventRequest(product!.Id, null, "ServiceHealthy", null, "{}", DateTime.UtcNow));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
