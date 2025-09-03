using Cassandra;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddResponseCompression();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<ISession>(_ =>
{
    var cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
    return cluster.Connect("retail");
});
builder.Services.AddSingleton<LocationProductReadRepository>();

var app = builder.Build();
app.UseResponseCompression();

app.MapGet("/api/locations/{locationId}/products", async (string locationId, LocationProductReadRepository repo, IMemoryCache cache) =>
{
    if (string.IsNullOrWhiteSpace(locationId))
        return Results.BadRequest();

    if (cache.TryGetValue(locationId, out List<ProductMappingDto> cached))
        return Results.Ok(cached);

    var data = await repo.GetByLocationAsync(locationId, Cassandra.ConsistencyLevel.LocalOne, CancellationToken.None);
    if (data.Count == 0) return Results.NotFound();

    cache.Set(locationId, data, TimeSpan.FromSeconds(30));
    return Results.Ok(data);
});

app.Run();

public record ProductMappingDto(string MasterProductId, string VendorProductId, string VendorName);

public class LocationProductReadRepository
{
    private readonly ISession _session;
    private readonly PreparedStatement _stmt;
    public LocationProductReadRepository(ISession session)
    {
        _session = session;
        _stmt = _session.Prepare("SELECT master_product_id, vendor_product_id, vendor_name FROM location_vendor_products WHERE location_id = ?");
    }
    public async Task<List<ProductMappingDto>> GetByLocationAsync(string locationId, ConsistencyLevel cl, CancellationToken ct)
    {
        var bound = _stmt.Bind(locationId).SetConsistencyLevel(cl);
        var rs = await _session.ExecuteAsync(bound);
        var list = new List<ProductMappingDto>();
        foreach (var row in rs)
        {
            list.Add(new ProductMappingDto(row.GetValue<string>("master_product_id"), row.GetValue<string>("vendor_product_id"), row.GetValue<string>("vendor_name")));
        }
        return list;
    }
}
