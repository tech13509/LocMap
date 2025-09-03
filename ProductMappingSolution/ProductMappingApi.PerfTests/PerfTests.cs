using System.Diagnostics;
using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ProductMappingApi.PerfTests
{
    public class ProductApiBenchmarks
    {
        private HttpClient _client = null!;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new WebApplicationFactory<Program>();
            _client = factory.CreateClient();
        }

        [Benchmark(Description = "Get products cached")]
        public async Task GetProducts_Cached()
        {
            var r = await _client.GetFromJsonAsync<List<ProductMappingDto>>("/api/locations/LOC1/products?cache=true");
        }

        [Benchmark(Description = "Get products no cache")]
        public async Task GetProducts_NoCache()
        {
            var r = await _client.GetFromJsonAsync<List<ProductMappingDto>>("/api/locations/LOC1/products?cache=false");
        }
    }

    public class ProgramPerf
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ProductApiBenchmarks>();
        }
    }

    public class StressTests
    {
        [Fact]
        public async Task ShouldHandle1000RequestsQuickly()
        {
            var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();
            var sw = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, 1000)
                .Select(_ => client.GetFromJsonAsync<List<ProductMappingDto>>("/api/locations/LOC1/products?cache=true"));
            await Task.WhenAll(tasks);

            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 2000, $"Too slow: {sw.ElapsedMilliseconds} ms");
        }
    }
}
