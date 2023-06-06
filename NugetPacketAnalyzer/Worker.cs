using System;
using System.Xml;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetPacketAnalyzer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _conf;
        private readonly PackageMetadataResource _packageMetadataResource;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _conf = configuration;
            _packageMetadataResource = GetPackageMetadataResource().GetAwaiter().GetResult();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("1.\tType 1 to get upgradable packages to last version of .net core 3.1.xxx");
            Console.WriteLine("2.\tType 2 to get a list of all packages");
            Console.WriteLine("3.\tType 3 to get a list of all packages with project referral");
            Console.WriteLine("q.\tType q to quit");

            var command = Console.ReadLine();
            while (command != "q")
            {
                //readl al files .proj                                
                string rootDirectory = _conf.GetSection("root").Value;
                string searchPattern = "*.csproj";

                string[] projFiles = Directory.GetFiles(rootDirectory, searchPattern, SearchOption.AllDirectories);

                var list = new List<(string, string, string)>();

                if (command == "1")
                {
                    foreach (string projFile in projFiles)
                    {
                        var tmpList = GetPackagesInfo(await File.ReadAllTextAsync(projFile),
                            Path.GetFileNameWithoutExtension(projFile))
                            .Where(x => (x.Item1?.Contains("icrosoft") ?? false || (x.Item1?.Contains("ystem") ?? false))
                            );
                        list.AddRange(tmpList);
                    }

                    //var comparer = new MyEqualityComparer();
                    //list = list.Distinct(comparer)
                    //    .OrderBy(x => x.Item1)
                    //    .ToList();

                    var maxVersion = Version.Parse("3.0.0");
                    var minVersion = Version.Parse("3.0.0");

                    var listGrouped = list.GroupBy(x => x.Item1).ToList();

                    foreach (var elemG in listGrouped)
                    {
                        var elem = elemG.FirstOrDefault();
                        var lastVersion = await GetLastNugetPackageVersion(elemG.Key, Version.Parse(elem.Item2), maxVersion);
                        if (lastVersion != null)
                        {
                            Console.WriteLine($"{elemG.Key} - {elem.Item2} - {lastVersion}");
                            Console.ForegroundColor = ConsoleColor.Green;
                            var counter = 0;
                            foreach (var proj in elemG)
                            {
                                Console.WriteLine($"\t{proj.Item3} - {proj.Item2}");
                                counter++;
                                Console.ForegroundColor = counter % 2 == 0 ? ConsoleColor.Green : ConsoleColor.DarkGreen;
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                }

                if (command == "2" || command == "3")
                {
                    foreach (string projFile in projFiles)
                    {
                        var tmpList = GetPackagesInfo(await File.ReadAllTextAsync(projFile),
                            Path.GetFileNameWithoutExtension(projFile));
                        list.AddRange(tmpList);
                    }

                    //var comparer = new MyEqualityComparer();
                    //list = list.Distinct(comparer)
                    //    .OrderBy(x => x.Item1)
                    //    .ToList();

                    var listGrouped = list.GroupBy(x => x.Item1).ToList();

                    foreach (var elemG in listGrouped)
                    {
                        var elem = elemG.FirstOrDefault();
                        Console.WriteLine($"{elemG.Key} - {elem.Item2}");
                        if (command == "3")
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            var counter = 0;
                            foreach (var proj in elemG)
                            {
                                Console.WriteLine($"\t{proj.Item3} - {proj.Item2}");
                                counter++;
                                Console.ForegroundColor = counter % 2 == 0 ? ConsoleColor.Green : ConsoleColor.DarkGreen;
                            }
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                }

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("1.\tType 1 to get upgradable packages to last version of .net core 3.1.xxx");
                Console.WriteLine("2.\tType 2 to get a list of all packages");
                Console.WriteLine("q.\tType q to quit");
                command = Console.ReadLine();
                //await Task.Delay(1000, stoppingToken);
            }
            await Program.Host.StopAsync();
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

        private async Task<string> GetLastNugetPackageVersion(string packageName, Version minVersion, Version maxVersion)
        {
            IEnumerable<IPackageSearchMetadata> packageMetadata = await _packageMetadataResource.GetMetadataAsync(packageName, includePrerelease: false, includeUnlisted: false, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None);

            var lastVersion = packageMetadata.OrderByDescending(x => x.Identity.Version)
                .FirstOrDefault(x =>
                (minVersion == null || x.Identity.Version.Major >= minVersion.Major)
                && (maxVersion == null || x.Identity.Version.Major <= maxVersion.Major))?
                .Identity?.Version?.ToString();

            if (lastVersion != minVersion.ToString())
                return lastVersion;
            else return null;

            //foreach (IPackageSearchMetadata metadata in packageMetadata)
            //{
            //    Console.WriteLine($"Package Id: {metadata.Identity.Id}");
            //    Console.WriteLine($"Version: {metadata.Identity.Version}");
            //    Console.WriteLine($"Description: {metadata.Description}");
            //    Console.WriteLine($"Authors: {metadata.Authors}");
            //    Console.WriteLine($"Total Downloads: {metadata.DownloadCount}");
            //    Console.WriteLine($"Published Date: {metadata.Published}");
            //    Console.WriteLine($"Project URL: {metadata.ProjectUrl}");
            //}
        }

        static async Task<PackageMetadataResource> GetPackageMetadataResource()
        {
            SourceRepository sourceRepository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            PackageMetadataResource resource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
            return resource;
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
                hash = hash * 23 + obj.Item1?.GetHashCode() ?? 0;
                hash = hash * 23 + obj.Item2?.GetHashCode() ?? 0;
                hash = hash * 23 + obj.Item3?.GetHashCode() ?? 0;
                return hash;
            }
        }
    }
}