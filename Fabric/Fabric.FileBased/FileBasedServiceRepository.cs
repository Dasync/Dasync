using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.ServiceRegistry;
using Newtonsoft.Json;

namespace Dasync.Fabric.FileBased
{
    public class FileBasedServiceRepository : IServiceDiscovery, IServicePublisher
    {
        public FileBasedServiceRepository()
        {
            Directory = Path.GetFullPath(
                Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(),
                    @"data\registry"));
        }

        public string Directory { get; }

        public Task<IEnumerable<ServiceRegistrationInfo>> DiscoverAsync(CancellationToken ct)
        {
            var result = new List<ServiceRegistrationInfo>();

            if (System.IO.Directory.Exists(Directory))
            {
                foreach (var filePath in System.IO.Directory.EnumerateFiles(
                    Directory, "*.json", SearchOption.TopDirectoryOnly))
                {
                    var json = File.ReadAllText(filePath);
                    var info = JsonConvert.DeserializeObject<ServiceRegistrationInfo>(json);
                    result.Add(info);
                }
            }

            return Task.FromResult((IEnumerable<ServiceRegistrationInfo>)result);
        }

        public Task PublishAsync(IEnumerable<ServiceRegistrationInfo> services, CancellationToken ct)
        {
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);

            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };

            foreach (var info in services)
            {
                var fileName = info.Name + ".json";
                var filePath = Path.Combine(Directory, fileName);
                var json = JsonConvert.SerializeObject(info, jsonSettings);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }

            return Task.FromResult(true);
        }
    }
}
