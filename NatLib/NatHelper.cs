using Open.Nat;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NatLib
{
    public static class NatHelper
    {
        private static Mapping _mapping;
        private static NatDevice _device;
        public async static Task CreateUPnPMapping(int internalPort, int externalPort, string mappingName)
        {
            try
            {
                var discoverer = new NatDiscoverer();
                var cancellationToken = new CancellationTokenSource(5000);

                Console.WriteLine("Discovering UPnP device...");
                _device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cancellationToken);
                

                var ip = await _device.GetExternalIPAsync();
                Console.WriteLine($"Device discovered with external IP: {ip}");

                _mapping = new Mapping(Protocol.Udp, internalPort, externalPort, mappingName);

                await _device.CreatePortMapAsync(_mapping);

                Console.WriteLine($"Port mapping {_mapping.Description} created: internal({internalPort}) -> external({externalPort})");                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public async static Task DeleteMapping()
        {
            await _device.DeletePortMapAsync(_mapping);
            Console.WriteLine($"Port mapping {_mapping.Description} deleted");
        }
    }
}
