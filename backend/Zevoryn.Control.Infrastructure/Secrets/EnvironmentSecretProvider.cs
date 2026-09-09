namespace Zevoryn.Control.Infrastructure.Secrets;

using Zevoryn.Control.Application.Abstractions;
using Microsoft.Extensions.Configuration;

public sealed class EnvironmentSecretProvider(IConfiguration configuration) : ISecretProvider
{
    public Task<string?> GetSecretAsync(string secretReference, CancellationToken cancellationToken)
    {
        var key = "ZEVORYN_SECRET_" + secretReference.Trim().ToUpperInvariant().Replace('-', '_').Replace('.', '_').Replace(':', '_');
        return Task.FromResult(configuration[key]);
    }
}
