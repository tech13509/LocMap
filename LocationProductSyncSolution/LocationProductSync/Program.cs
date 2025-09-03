using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Cassandra;
using LocationProductSync;

namespace LocationProductSyncApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((ctx, services) =>
                {
                    var cluster = Cluster.Builder()
                        .AddContactPoint("127.0.0.1")
                        .Build();
                    var session = cluster.Connect("product_keyspace");

                    services.AddSingleton<ISession>(session);
                    services.AddSingleton<ICassandraRepository, CassandraRepository>();
                    services.AddSingleton<ILocationService, DummyLocationService>();
                    services.AddSingleton<IFeedService, DummyFeedService>();
                    services.AddSingleton<LocationProductSyncOrchestrator>();
                    services.AddHostedService<SyncHostedService>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
