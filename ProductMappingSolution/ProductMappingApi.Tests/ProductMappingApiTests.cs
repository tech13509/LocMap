using System.Net;
using Cassandra;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ProductMappingApi.Tests
{
    public class ProductMappingApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<LocationProductReadRepository> _repoMock;

        public ProductMappingApiTests(WebApplicationFactory<Program> factory)
        {
            _repoMock = new Mock<LocationProductReadRepository>(MockBehavior.Strict, Mock.Of<ISession>());
            _repoMock.Setup(r => r.GetByLocationAsync("LOC1", It.IsAny<ConsistencyLevel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductMappingDto> { new("M1","V1","VendorA") });
            _repoMock.Setup(r => r.GetByLocationAsync("EMPTY", It.IsAny<ConsistencyLevel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ProductMappingDto>());

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(_repoMock.Object);
                });
            });
        }

        [Fact]
        public async Task Ok_WhenDataExists()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/locations/LOC1/products");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task NotFound_WhenNoData()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/locations/EMPTY/products");
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task BadRequest_WhenInvalid()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/api/locations//products");
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task Cache_IsUsedOnRepeatedCalls()
        {
            var client = _factory.CreateClient();
            var resp1 = await client.GetAsync("/api/locations/LOC1/products?cache=true");
            var resp2 = await client.GetAsync("/api/locations/LOC1/products?cache=true");
            _repoMock.Verify(r => r.GetByLocationAsync("LOC1", It.IsAny<ConsistencyLevel>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
