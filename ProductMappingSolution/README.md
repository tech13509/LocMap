# ProductMappingSolution

Projects:
- ProductMappingApi: Minimal API with Cassandra read endpoint
- ProductMappingApi.Tests: Unit tests (xUnit, Moq)
- ProductMappingApi.PerfTests: Performance tests (BenchmarkDotNet + stress test)

Build & Run:
dotnet build
dotnet test
dotnet run --project ProductMappingApi

Run performance tests:
dotnet run -c Release --project ProductMappingApi.PerfTests
