using System.Xml;

namespace NugetPacketAnalyzer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _conf;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _conf = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            {
                //readl al files .proj                                
                string rootDirectory = _conf.GetSection("root").Value;
                string searchPattern = "*.csproj";

                string[] projFiles = Directory.GetFiles(rootDirectory, searchPattern, SearchOption.AllDirectories);

                var list = new List<(string, string, string)>();

                foreach (string projFile in projFiles)
                {
                    var tmpList = GetPackagesInfo(await File.ReadAllTextAsync(projFile),
                        Path.GetFileNameWithoutExtension(projFile));
                    list.AddRange(tmpList);
                }

                var comparer = new MyEqualityComparer();
                list = list.Distinct(comparer)
                    .OrderBy(x => x.Item1)
                    .ToList();

                foreach (var elem in list)
                {
                    Console.WriteLine($"{elem.Item1} - {elem.Item2}");
                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private IEnumerable<(string, string, string)> GetPackagesInfo(string xml, string projName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlNodeList packageReferences = xmlDoc.SelectNodes("//PackageReference");
            foreach (XmlNode packageReference in packageReferences)
            {
                XmlAttribute include = packageReference.Attributes["Include"];
                XmlAttribute version = packageReference.Attributes["Version"];
                yield return (include?.Value, version?.Value, projName);
            }
        }

        class MyEqualityComparer : IEqualityComparer<(string, string, string)>
        {
            public bool Equals((string, string, string) x, (string, string, string) y)
            {
                return x.Item1 == y.Item1 && x.Item2 == y.Item2;
            }

            public int GetHashCode((string, string, string) obj)
            {
                int hash = 17;
                hash = hash * 23 + obj.Item1.GetHashCode();
                hash = hash * 23 + obj.Item2.GetHashCode();
                //hash = hash * 23 + obj.Item3.GetHashCode();
                return hash;
            }
        }
    }
}