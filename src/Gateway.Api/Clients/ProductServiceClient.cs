using Common.Contracts.Products;
using Microsoft.AspNetCore.WebUtilities;

namespace Gateway.Api.Clients;

public sealed class ProductServiceClient(HttpClient http)
{
    public async Task<IReadOnlyList<ProductDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var pairs = ids.Select(id => new KeyValuePair<string, string>("ids", id.ToString()));
        var url = QueryHelpers.AddQueryString("/products", pairs);

        var products = await http.GetFromJsonAsync<List<ProductDto>>(url, cancellationToken: ct);
        return products ?? [];
    }
}
