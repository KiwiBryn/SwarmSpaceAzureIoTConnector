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
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Provisioning.Client;
    using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay;
    using Microsoft.Azure.Devices.Provisioning.Client.Transport;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using LazyCache;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class UplinkQueueProcessor
    {
        private readonly ILogger _logger;
        private readonly static IAppCache _deviceClients = new CachingService();
        private readonly Models.AzureIoTSettings _azureIoTSettings;

        public UplinkQueueProcessor(ILoggerFactory loggerFactory, IOptions<Models.AzureIoTSettings> azureIoTSettings)
        {
            _logger = loggerFactory.CreateLogger<UplinkQueueProcessor>();
            _azureIoTSettings = azureIoTSettings.Value;
        }

        [Function("UplinkQueueTrigger")]
        public async Task Run([QueueTrigger("uplink1", Connection = "AzureFunctionsStorage")] string payload)
        {
            DeviceClient deviceClient = null;

            Models.UplinkPayload uplinkPayload = JsonConvert.DeserializeObject<Models.UplinkPayload>(payload);

            Models.AzureIoTDeviceClientContext context = new Models.AzureIoTDeviceClientContext()
            {
                OrganisationId = uplinkPayload.OrganizationId,
                //UserApplicationId = uplinkPayload.UserApplicationId, deprecated
                DeviceType = uplinkPayload.DeviceType,
                DeviceId = uplinkPayload.DeviceId,
            };

            switch (_azureIoTSettings.ApplicationType)
            {
                case Models.ApplicationType.AzureIotHub:
                    switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                    {
                        case Models.AzureIotHubConnectionType.DeviceConnectionString:
                            deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(uplinkPayload.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(uplinkPayload.DeviceId.ToString(), context), memoryCacheEntryOptions);
                            break;
                        case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                            deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(uplinkPayload.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(uplinkPayload.DeviceId.ToString(), context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService), memoryCacheEntryOptions);
                            break;
                        default:
                            _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                            throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                    }
                    break;

                case Models.ApplicationType.AzureIoTCentral:
                    deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(uplinkPayload.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(uplinkPayload.DeviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService), memoryCacheEntryOptions);
                    break;

                default:
                    _logger.LogError("AzureIoT application type unknown {0}", _azureIoTSettings.ApplicationType);

                    throw new NotImplementedException("AzureIoT unsupported ApplicationType");
            }

            JObject telemetryEvent = new JObject
                {
                    { "packetId", uplinkPayload.PacketId},
                    { "deviceType" , uplinkPayload.DeviceType},
                    { "DeviceID", uplinkPayload.DeviceId },
                    { "OrganizationId", uplinkPayload.OrganizationId },
                    { "UserApplicationId", uplinkPayload.UserApplicationId },
                    { "ReceivedAtUtc", uplinkPayload.HiveRxTimeUtc.ToString("s", CultureInfo.InvariantCulture)},
                    { "DataLength", payload.Length },
                    { "Data", uplinkPayload.Data },
                    { "Status", uplinkPayload.Status },
                };

            // Send the message to Azure IoT Hub
            using (Message ioTHubmessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryEvent))))
            {
                // Ensure the displayed time is the acquired time rather than the uploaded time. 
                ioTHubmessage.Properties.Add("PacketId", uplinkPayload.PacketId.ToString());
                ioTHubmessage.Properties.Add("DeviceType", uplinkPayload.DeviceType.ToString());
                ioTHubmessage.Properties.Add("DeviceId", uplinkPayload.DeviceId.ToString());
                ioTHubmessage.Properties.Add("OrganizationId", uplinkPayload.OrganizationId.ToString());
                ioTHubmessage.Properties.Add("UserApplicationId", uplinkPayload.UserApplicationId.ToString());
                ioTHubmessage.Properties.Add("iothub-creation-time-utc", uplinkPayload.HiveRxTimeUtc.ToString("s", CultureInfo.InvariantCulture));
                ioTHubmessage.Properties.Add("Status", uplinkPayload.Status.ToString());

                await deviceClient.SendEventAsync(ioTHubmessage);

                _logger.LogInformation("Uplink-PacketID:{0} DeviceID:{1} SendEventAsync success", uplinkPayload.PacketId, uplinkPayload.DeviceId);
            }

            /*
            using (Message ioTHubmessage = swarmSpaceFormatterUplink.Evaluate(payload.OrganizationId, payload.DeviceId, context.DeviceType, payload.UserApplicationId, telemetryEvent, payloadJson, payloadText, payloadBytes))
            {
                _logger.LogDebug("Uplink-DeviceId:{0} PacketId:{1} TelemetryEvent after:{0}", payload.DeviceId, payload.PacketId, JsonConvert.SerializeObject(telemetryEvent, Formatting.Indented));

                ioTHubmessage.Properties.Add("PacketId", payload.PacketId.ToString());
                ioTHubmessage.Properties.Add("OrganizationId", payload.OrganizationId.ToString());
                ioTHubmessage.Properties.Add("UserApplicationId", payload.UserApplicationId.ToString());
                ioTHubmessage.Properties.Add("DeviceId", payload.DeviceId.ToString());
                ioTHubmessage.Properties.Add("deviceType", payload.DeviceType.ToString());

                await deviceClient.SendEventAsync(ioTHubmessage);

                _logger.LogInformation("Uplink-DeviceID:{deviceId} PacketId:{1} SendEventAsync success", payload.DeviceId, payload.PacketId);
            }
            */
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

                    // If TTI application does have a DTDLV2 ID 
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

                using (message)
                {
                    switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                    {
                        case Models.AzureIotHubConnectionType.DeviceConnectionString:
                            deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(context.DeviceId.ToString(), context), memoryCacheEntryOptions);
                            break;
                        case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                            deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(context.DeviceId.ToString(), context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService), memoryCacheEntryOptions);
                            break;
                        default:
                            _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                            throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                    }

                    /*
                    if (!message.Properties.TryGetValue("UserApplicationId", out string value) || !int.TryParse(message.Properties["UserApplicationId"], out int userApplicationId))
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
                    _logger.LogInformation("Downlink-IoT Hub DeviceID:{DeviceId} MessageID:{MessageId} UserApplicationId:{userApplicationId}", context.DeviceId, message.MessageId, userApplicationId);

                    await _bumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, userApplicationId, message.GetBytes());
                    */

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
                    deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(context.DeviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService), memoryCacheEntryOptions);

                    /*
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
                                swarmSpaceFormatterDownlink = await _formatterCache.DownlinkGetAsync(methodSetting.UserApplicationId);
                            }
                            catch (CSScriptLib.CompilerException cex)
                            {
                                _logger.LogInformation(cex, "Uplink-DeviceID:{deviceId} UserApplicationId:{UserApplicationId} payload formatter compilation failed", context.DeviceId, methodSetting.UserApplicationId);

                                await deviceClient.RejectAsync(message);

                                return;
                            }

                            byte[] payloadData = swarmSpaceFormatterDownlink.Evaluate(context.OrganisationId, context.DeviceId, context.DeviceType, methodSetting.UserApplicationId, payloadJson);

                            await _bumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, methodSetting.UserApplicationId, payloadData);

                            await deviceClient.CompleteAsync(message);
                        }
                    }
                    */
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
