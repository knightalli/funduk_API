using Common.Contracts.Orders;
using Microsoft.AspNetCore.WebUtilities;

namespace Gateway.Api.Clients;

public sealed class OrderServiceClient(HttpClient http)
{
    public async Task<IReadOnlyList<OrderDto>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var url = QueryHelpers.AddQueryString("/orders", "userId", userId.ToString());
        var orders = await http.GetFromJsonAsync<List<OrderDto>>(url, cancellationToken: ct);
        return orders ?? [];
    }
}
