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

        private async Task<DeviceClient> AzureIoTHubDeviceProvisioningServiceConnectAsync(string deviceId, object context, Models.AzureDeviceProvisioningService deviceProvisioningService)
        {
            DeviceClient deviceClient;

            string deviceKey;
            using (var hmac = new HMACSHA256(Convert.FromBase64String(deviceProvisioningService.GroupEnrollmentKey)))
            {
                deviceKey = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(deviceId)));
            }

            using (var securityProvider = new SecurityProviderSymmetricKey(deviceId, deviceKey, null))
            {
                using (var transport = new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly))
                {
                    DeviceRegistrationResult result;

                    ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(
                        deviceProvisioningService.GlobalDeviceEndpoint,
                        deviceProvisioningService.IdScope,
                        securityProvider,
                        transport);

                    if (!string.IsNullOrEmpty(deviceProvisioningService.DtdlModelId))
                    {
                        ProvisioningRegistrationAdditionalData provisioningRegistrationAdditionalData = new ProvisioningRegistrationAdditionalData()
                        {
                            JsonData = PnpConvention.CreateDpsPayload(deviceProvisioningService.DtdlModelId)
                        };
                        result = await provClient.RegisterAsync(provisioningRegistrationAdditionalData);
                    }
                    else
                    {
                        result = await provClient.RegisterAsync();
                    }

                    if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                    {
                        _logger.LogWarning("Uplink-DeviceID:{deviceId} RegisterAsync status:{result.Status} failed ", deviceId, result.Status);

                        throw new ApplicationException($"Uplink-DeviceID:{deviceId} RegisterAsync status:{result.Status} failed");
                    }

                    IAuthenticationMethod authentication = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, (securityProvider as SecurityProviderSymmetricKey).GetPrimaryKey());

                    deviceClient = DeviceClient.Create(result.AssignedHub, authentication, TransportSettings);
                }
            }

            switch (_azureIoTSettings.ApplicationType)
            {
                case Models.ApplicationType.AzureIotHub:
                    await deviceClient.SetReceiveMessageHandlerAsync(AzureIoTHubMessageHandler, context);
                    break;
                case Models.ApplicationType.AzureIoTCentral:
                    await deviceClient.SetReceiveMessageHandlerAsync(AzureIoTCentralMessageHandler, context);
                    break;
                default:
                    _logger.LogError("Azure IoT Hub ApplicationType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                    throw new NotImplementedException("AzureIoT Hub unsupported ApplicationType");
            }

            await deviceClient.SetMethodDefaultHandlerAsync(AzureIoTHubClientDefaultMethodHandler, context);

            await deviceClient.OpenAsync();

            return deviceClient;
        }

        private static readonly MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
        {
            Priority = CacheItemPriority.NeverRemove
        };

        private static readonly ITransportSettings[] TransportSettings = new ITransportSettings[]
        {
            new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
            {
                AmqpConnectionPoolSettings = new AmqpConnectionPoolSettings()
                {
                    Pooling = true,
                }
             }
        };
    }
}
