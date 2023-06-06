using Microsoft.Extensions.Hosting;

namespace NugetPacketAnalyzer
{
    public class Program
    {
        public static IHost Host;
        public static void Main(string[] args)
        {
            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                })
                .Build();

            Host.Run();
        }
    }
}