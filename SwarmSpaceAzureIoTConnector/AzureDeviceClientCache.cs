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

    internal class AzureDeviceClientCache: IAzureDeviceClientCache
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

        private async Task<DeviceClient> AzureIoTHubDeviceConnectionStringConnectAsync(string deviceId, object context)
        {
            DeviceClient deviceClient;

            if (string.IsNullOrEmpty(_azureIoTSettings.AzureIotHub.DtdlModelId))
            {
                _logger.LogInformation("Uplink-DeviceID:{deviceId} IoT Hub Application settings DTDL not configured", deviceId);

                deviceClient = DeviceClient.CreateFromConnectionString(_azureIoTSettings.AzureIotHub.ConnectionString, deviceId, TransportSettings);
            }
            else
            {
                ClientOptions clientOptions = new ClientOptions()
                {
                    ModelId = _azureIoTSettings.AzureIotHub.DtdlModelId
                };

                deviceClient = DeviceClient.CreateFromConnectionString(_azureIoTSettings.AzureIotHub.ConnectionString, deviceId, TransportSettings, clientOptions);
            }

            await deviceClient.SetReceiveMessageHandlerAsync(AzureIoTHubMessageHandler, context);

            await deviceClient.SetMethodDefaultHandlerAsync(AzureIoTHubClientDefaultMethodHandler, context);

            await deviceClient.OpenAsync();

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

        public async Task AzureIoTHubMessageHandler(Message message, object userContext)
        {
            DeviceClient deviceClient;

            try
            {
                Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

                switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                {
                    case Models.AzureIotHubConnectionType.DeviceConnectionString:
                        deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(context.DeviceId.ToString(), context));
                        break;
                    case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                        deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(context.DeviceId.ToString(), context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService));
                        break;
                    default:
                        _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                        throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                }

                using (message)
                {
                    if (!message.Properties.TryGetValue("UserApplicationId", out string value) || !ushort.TryParse(message.Properties["UserApplicationId"], out ushort userApplicationId))
                    {
                        _logger.LogInformation("Downlink-IoT Hub DeviceID:{DeviceId} MessageID:{MessageId} DeviceType:{context.DeviceType} OrganisationId:{OrganisationId} - UserApplicationId property missing or invalid", context.DeviceId, message.MessageId, context.DeviceType, context.OrganisationId);

                        await deviceClient.RejectAsync(message);

                        return;
                    }

                    if ((userApplicationId < Models.Constants.UserApplicationIdMinimum) || (userApplicationId > Models.Constants.UserApplicationIdMaximum))
                    {
                        _logger.LogInformation("Downlink-IoT Hub DeviceID:{DeviceId} MessageID:{MessageId} UserApplicationId:{userApplicationId} - UserApplicationId property invalid {UserApplicationIdMinimum} to {UserApplicationIdMaximum}", context.DeviceId, message.MessageId, userApplicationId, Models.Constants.UserApplicationIdMinimum, Models.Constants.UserApplicationIdMaximum);

                        await deviceClient.RejectAsync(message);

                        return;
                    }

                    byte[] payloadBytes = message.GetBytes();

                    string payloadText = string.Empty;

                    // Normally wouldn't use exceptions for flow control but, I can't think of a better way...
                    try
                    {
                        payloadText = Encoding.UTF8.GetString(payloadBytes);
                    }
                    catch (FormatException fex)
                    {
                        _logger.LogWarning(fex, "Uplink- DeviceId:{0} MessageId:{1} Convert.ToString(payloadBytes) failed", context.DeviceId, message.MessageId);
                    }

                    JObject payloadJson = null;

                    if (payloadText != string.Empty)
                    {
                        try
                        {
                            JContainer.Parse(payloadText);

                            payloadJson = JObject.Parse(payloadText);
                        }
                        catch (JsonException jex)
                        {
                            _logger.LogWarning(jex, "Uplink- DeviceId:{0} MessageId:{1} JObject failed", context.DeviceId, message.MessageId);
                        }
                    }

                    IFormatterDownlink swarmSpaceFormatterDownlink;

                    byte[] payloadData;

                    try
                    {
                        swarmSpaceFormatterDownlink = await _payloadFormatterCache.DownlinkGetAsync(userApplicationId);

                        payloadData = swarmSpaceFormatterDownlink.Evaluate(context.OrganisationId, context.DeviceId, context.DeviceType, userApplicationId, payloadJson, payloadText, payloadBytes);
                    }
                    catch (CSScriptLib.CompilerException cex)
                    {
                        _logger.LogWarning(cex, "Uplink-DeviceID:{deviceId} UserApplicationId:{UserApplicationId} payload formatter compilation failed", context.DeviceId, userApplicationId);

                        await deviceClient.RejectAsync(message);

                        return;
                    }

                    _logger.LogInformation("Downlink-IoT Hub SendAsync Start DeviceID:{DeviceId} UserAplicationId:{userApplicationId}", context.DeviceId, userApplicationId);

                    await _swarmSpaceBumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, userApplicationId, payloadData);

                    _logger.LogInformation("Downlink-IoT Hub SendAsync Finish DeviceID:{DeviceId} MessageID:{MessageId} UserApplicationId:{userApplicationId}", context.DeviceId, message.MessageId, userApplicationId);

                    await deviceClient.CompleteAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Downlink-MessageHandler processing failed");

                throw;
            }
        }

        public async Task AzureIoTCentralMessageHandler(Message message, object userContext)
        {
            DeviceClient deviceClient;

            try
            {
                Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

                using (message)
                {
                    deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(context.DeviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService));

                    string methodName;

                    // Check to see if "raw" Azure IoT hub message, no transformations just send the payload
                    if (!message.Properties.TryGetValue("method-name", out methodName) || string.IsNullOrWhiteSpace(methodName))
                    {
                        _logger.LogWarning("Downlink-DeviceID:{DeviceId} MessagedID:{MessageId} method-name:property missing or empty", context.DeviceId, message.MessageId);

                        await deviceClient.RejectAsync(message);
                        return;
                    }

                    // Look up the method settings to get confirmed, port, priority, and queue
                    if ((_azureIoTSettings.AzureIoTCentral.Methods == null) || !_azureIoTSettings.AzureIoTCentral.Methods.TryGetValue(methodName, out Models.AzureIoTCentralMethodSetting methodSetting))
                    {
                        _logger.LogWarning("Downlink-DeviceID:{DeviceId} MessagedID:{MessageId} method-name:{methodName} has no settings", context.DeviceId, message.MessageId, methodName);

                        await deviceClient.RejectAsync(message);

                        return;
                    }

                    byte[] payloadBytes = message.GetBytes();

                    string payloadText = string.Empty;

                    try
                    {
                        payloadText = Encoding.UTF8.GetString(payloadBytes);
                    }
                    catch (FormatException fex)
                    {
                        _logger.LogWarning(fex, "Uplink- DeviceId:{0} MessageId:{1} Convert.ToString(payloadBytes) failed", context.DeviceId, message.MessageId);
                    }

                    JObject payloadJson = null;

                    // Check to see if special case for Azure IoT central command with no request payload
                    if (payloadText.IsPayloadEmpty())
                    {
                        if (methodSetting.Payload.IsPayloadValidJson())
                        {
                            payloadJson = JObject.Parse(methodSetting.Payload);
                        }
                        else
                        {
                            _logger.LogWarning("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} method-name:{methodName} payload invalid {Payload}", context.DeviceId, message.LockToken, methodName, methodSetting.Payload);

                            await deviceClient.RejectAsync(message);
                            return;
                        }

                        if (!payloadText.IsPayloadEmpty())
                        {
                            if (payloadText.IsPayloadValidJson())
                            {
                                payloadJson = JObject.Parse(payloadText);
                            }
                            else
                            {
                                // Normally wouldn't use exceptions for flow control but, I can't think of a better way...
                                try
                                {
                                    payloadJson = new JObject(new JProperty(methodName, JProperty.Parse(payloadText)));
                                }
                                catch (JsonException ex)
                                {
                                    payloadJson = new JObject(new JProperty(methodName, payloadText));
                                }
                            }

                            _logger.LogInformation("Downlink-IoT Central DeviceID:{DeviceId} Method:{methodName} MessageID:{MessageId} UserAplicationId:{userApplicationId}", context.DeviceId, methodName, message.MessageId, methodSetting.UserApplicationId);

                            IFormatterDownlink swarmSpaceFormatterDownlink;

                            try
                            {
                                swarmSpaceFormatterDownlink = await _payloadFormatterCache.DownlinkGetAsync(methodSetting.UserApplicationId);
                            }
                            catch (CSScriptLib.CompilerException cex)
                            {
                                _logger.LogInformation(cex, "Uplink-DeviceID:{deviceId} UserApplicationId:{UserApplicationId} payload formatter compilation failed", context.DeviceId, methodSetting.UserApplicationId);

                                await deviceClient.RejectAsync(message);

                                return;
                            }

                            byte[] payloadData = swarmSpaceFormatterDownlink.Evaluate(context.OrganisationId, context.DeviceId, context.DeviceType, methodSetting.UserApplicationId, payloadJson, payloadText, payloadBytes);

                            await _swarmSpaceBumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, methodSetting.UserApplicationId, payloadData);

                            await deviceClient.CompleteAsync(message);
                        }
                    }

                    await deviceClient.CompleteAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Downlink-MessageHandler processing failed");

                throw;
            }
        }

        private async Task<MethodResponse> AzureIoTHubClientDefaultMethodHandler(MethodRequest methodRequest, object userContext)
        {
            Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

            _logger.LogWarning("Downlink-DeviceID:{deviceId} DefaultMethodHandler name:{Name} payload:{DataAsJson}", context.DeviceId, methodRequest.Name, methodRequest.DataAsJson);

            return new MethodResponse(Encoding.ASCII.GetBytes("{\"message\":\"The SwarmSpace Connector does not support Direct Methods.\"}"), 400);
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
