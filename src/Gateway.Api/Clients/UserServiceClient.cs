using Common.Contracts.Users;
using System.Net;

namespace Gateway.Api.Clients;

public sealed class UserServiceClient(HttpClient http)
{
    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        using var resp = await http.GetAsync($"/users/{userId}", ct);

        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct);
    }
}
