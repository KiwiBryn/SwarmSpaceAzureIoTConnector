// Copyright (c) January 2023, devMobile Software
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using LazyCache;

    public interface IAzureDeviceClientCache
    {
        public Task<DeviceClient> GetOrAddAsync(uint deviceId,object context);
    }

    partial class AzureDeviceClientCache: IAzureDeviceClientCache
    {
        private readonly static IAppCache _azuredeviceClients = new CachingService();

        private readonly ILogger<AzureDeviceClientCache> _logger;
        private readonly IPayloadFormatterCache _payloadFormatterCache;
        private readonly ISwarmSpaceBumblebeeHive _swarmSpaceBumblebeeHive;
        private readonly Models.AzureIoTSettings _azureIoTSettings;

        public AzureDeviceClientCache(ILogger<AzureDeviceClientCache> logger, IPayloadFormatterCache payloadFormatterCache, ISwarmSpaceBumblebeeHive swarmSpaceBumblebeeHive, IOptions<Models.AzureIoTSettings> azureIoTSettings)
        {
            _logger = logger;
            _payloadFormatterCache = payloadFormatterCache;
            _swarmSpaceBumblebeeHive = swarmSpaceBumblebeeHive;
            _azureIoTSettings = azureIoTSettings.Value;
        }

        public async Task<DeviceClient> GetOrAddAsync(uint deviceId, object context)
        {
            DeviceClient deviceClient = null;

            switch (_azureIoTSettings.ApplicationType)
            {
                case Models.ApplicationType.AzureIotHub:
                    switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                    {
                        case Models.AzureIotHubConnectionType.DeviceConnectionString:
                            deviceClient =await _azuredeviceClients.GetOrAddAsync<DeviceClient>(deviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(deviceId.ToString(), context), memoryCacheEntryOptions);
                            break;
                        case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                            deviceClient= await _azuredeviceClients.GetOrAddAsync<DeviceClient>(deviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(deviceId.ToString(), context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService), memoryCacheEntryOptions);
                            break;
                        default:
                            _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                            throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                    }
                    break;

                case Models.ApplicationType.AzureIoTCentral:
                    deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(deviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(deviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService), memoryCacheEntryOptions);
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
