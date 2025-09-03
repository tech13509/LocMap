using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocationProductSync;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocationProductSync.Tests
{
    public class LocationProductSyncOrchestratorTests
    {
        private readonly Mock<ILocationService> _locationService = new();
        private readonly Mock<IFeedService> _feedService = new();
        private readonly Mock<ICassandraRepository> _repository = new();
        private readonly Mock<ILogger<LocationProductSyncOrchestrator>> _logger = new();

        private LocationProductSyncOrchestrator CreateSut() =>
            new LocationProductSyncOrchestrator(
                _locationService.Object,
                _feedService.Object,
                _repository.Object,
                _logger.Object);

        [Fact]
        public async Task SyncAsync_ShouldTraverseHierarchy_AndCallFeedServiceForEachLocation()
        {
            var country = new Location { Id = "IN", Name = "India", Level = 0 };
            var region = new Location { Id = "IN-N", Name = "North India", Level = 1, ParentId = "IN" };
            var city = new Location { Id = "DEL", Name = "Delhi", Level = 3, ParentId = "IN-N" };
            country.Children.Add(region);
            region.Children.Add(city);

            _locationService.Setup(s => s.GetLocationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Location> { country });

            _feedService.Setup(s => s.GetVendorProductsForLocationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<VendorProduct> { new VendorProduct { VendorProductId = "V123", VendorName = "VendorA", MasterProductId = "M456", LocationId = "X" } });

            var sut = CreateSut();
            await sut.SyncAsync(CancellationToken.None);

            _feedService.Verify(s => s.GetVendorProductsForLocationAsync("IN", It.IsAny<CancellationToken>()), Times.Once);
            _feedService.Verify(s => s.GetVendorProductsForLocationAsync("IN-N", It.IsAny<CancellationToken>()), Times.Once);
            _feedService.Verify(s => s.GetVendorProductsForLocationAsync("DEL", It.IsAny<CancellationToken>()), Times.Once);
            _repository.Verify(r => r.UpsertVendorProductsAsync(It.IsAny<IEnumerable<VendorProduct>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _repository.Verify(r => r.DeleteVendorProductsNotInFeedAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task SyncAsync_ShouldSkipIfNoProductsReturned()
        {
            var country = new Location { Id = "US", Name = "United States", Level = 0 };
            _locationService.Setup(s => s.GetLocationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Location> { country });

            _feedService.Setup(s => s.GetVendorProductsForLocationAsync("US", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<VendorProduct>());

            var sut = CreateSut();
            await sut.SyncAsync(CancellationToken.None);

            _repository.Verify(r => r.UpsertVendorProductsAsync(It.IsAny<IEnumerable<VendorProduct>>(), It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(r => r.DeleteVendorProductsNotInFeedAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
