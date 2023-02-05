using devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector.Models;
using LazyCache;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayloadFormatter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector
{
    public interface IAzureDeviceClientCache
    {
        public Task<DeviceClient> GetOrAddAsync(string deviceId, object context);
    }

    partial class AzureDeviceClientCache: IAzureDeviceClientCache
    {
        private readonly static IAppCache _azuredeviceClients = new CachingService();

        private readonly ILogger<AzureDeviceClientCache> _logger;
        private readonly IPayloadFormatterCache _payloadFormatterCache;
        private readonly ISwarmSpaceBumblebeeHive _swarmSpaceBumblebeeHive;
        private readonly AzureIoTSettings _azureIoTSettings;

        public AzureDeviceClientCache(ILogger<AzureDeviceClientCache> logger, IPayloadFormatterCache payloadFormatterCache, ISwarmSpaceBumblebeeHive swarmSpaceBumblebeeHive, IOptions<Models.AzureIoTSettings> azureIoTSettings)
        {
            _logger = logger;
            _payloadFormatterCache = payloadFormatterCache;
            _swarmSpaceBumblebeeHive = swarmSpaceBumblebeeHive;
            _azureIoTSettings = azureIoTSettings.Value;
        }

        public async Task<DeviceClient> GetOrAddAsync(string deviceId, object context)
        {
            DeviceClient deviceClient = null;

            switch (_azureIoTSettings.ApplicationType)
            {
                case Models.ApplicationType.AzureIotHub:
                    switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                    {
                        case Models.AzureIotHubConnectionType.DeviceConnectionString:
                            deviceClient =await _azuredeviceClients.GetOrAddAsync<DeviceClient>(deviceId, (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(deviceId, context));
                            break;
                        case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                            deviceClient= await _azuredeviceClients.GetOrAddAsync<DeviceClient>(deviceId, (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(deviceId, context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService));
                            break;
                        default:
                            _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                            throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                    }
                    break;

                case Models.ApplicationType.AzureIoTCentral:
                    deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(deviceId, (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(deviceId, context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService));
                    break;
                default:
                    _logger.LogError("AzureIoT application type unknown {0}", _azureIoTSettings.ApplicationType);

                    throw new NotImplementedException("AzureIoT unsupported ApplicationType");
            }

            return deviceClient;
        }
        

        private static readonly MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
        {
            Priority = CacheItemPriority.NeverRemove
        };
    }
}
