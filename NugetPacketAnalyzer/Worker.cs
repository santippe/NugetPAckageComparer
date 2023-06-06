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
        private readonly NugetService _nugetService;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, NugetService nugetService)
        {
            _logger = logger;
            _conf = configuration;
            _nugetService = nugetService;            
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
                        var tmpList = _nugetService.GetPackagesInfo(await File.ReadAllTextAsync(projFile),
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
                        var lastVersion = await _nugetService.GetLastNugetPackageVersion(elemG.Key, Version.Parse(elem.Item2), maxVersion);
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
                        var tmpList = _nugetService.GetPackagesInfo(await File.ReadAllTextAsync(projFile),
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

        
    }
}