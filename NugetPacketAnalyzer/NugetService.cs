using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace NugetPacketAnalyzer
{
    public class NugetService
    {
        private readonly PackageMetadataResource _packageMetadataResource;

        public NugetService()
        {
            _packageMetadataResource = GetPackageMetadataResource().GetAwaiter().GetResult();
        }

        public IEnumerable<(string, string, string)> GetPackagesInfo(string xml, string projName)
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

        public async Task<string> GetLastNugetPackageVersion(string packageName, Version minVersion, Version maxVersion)
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

        public async Task<PackageMetadataResource> GetPackageMetadataResource()
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
