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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using PayloadFormatter; // Short cut namespace for V1 formatters

    public partial class Connector
    {
        [Function("UplinkMessageQueueProcessor")]
        public async Task UplinkMessageQueueProcessor([QueueTrigger("uplink", Connection = "AzureFunctionsStorage")] Models.UplinkPayloadQueueDto payload)
        {
            DeviceClient deviceClient = null;

            Models.AzureIoTDeviceClientContext context = new Models.AzureIoTDeviceClientContext()
            {
                OrganisationId = payload.OrganizationId,
                //UserApplicationId = payload.UserApplicationId, deprecated
                DeviceType = payload.DeviceType,
                DeviceId = payload.DeviceId,
            };

            switch (_azureIoTSettings.ApplicationType)
            {
                case Models.ApplicationType.AzureIotHub:
                    switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                    {
                        case Models.AzureIotHubConnectionType.DeviceConnectionString:
                            deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(payload.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(payload.DeviceId.ToString(), context), memoryCacheEntryOptions);
                            break;
                        case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                            deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(payload.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(payload.DeviceId.ToString(), context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService), memoryCacheEntryOptions);
                            break;
                        default:
                            _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                            throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                    }
                    break;

                case Models.ApplicationType.AzureIoTCentral:
                    deviceClient = await _deviceClients.GetOrAddAsync<DeviceClient>(payload.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(payload.DeviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService), memoryCacheEntryOptions);
                    break;

                default:
                    _logger.LogError("AzureIoT application type unknown {0}", _azureIoTSettings.ApplicationType);

                    throw new NotImplementedException("AzureIoT unsupported ApplicationType");
            }

            IFormatterUplink payloadFormatterUplink;

            try
            {
                payloadFormatterUplink = await _payloadFormatterCache.UplinkGetAsync(payload.UserApplicationId);
            }
            catch (CSScriptLib.CompilerException cex)
            {
                _logger.LogInformation(cex, "Uplink-DeviceID:{deviceId} UserApplicationId:{UserApplicationId} payload formatter compilation failed", payload.DeviceId, payload.UserApplicationId);

                throw new InvalidProgramException("Uplink payload formatter invalid or not found");
            }

            byte[] payloadBytes = null;

            try
            {
                payloadBytes = Convert.FromBase64String(payload.Data);
            }
            catch (FormatException fex)
            {
                _logger.LogWarning(fex, "Uplink- DeviceId:{0} PacketId:{1} Convert.FromBase64String(payload.Data) failed", payload.DeviceId, payload.PacketId);

                throw new ArgumentException("Convert.FromBase64String(payload.Data) failed");
            }

            string payloadText = string.Empty;
            JObject payloadJson = null;

            if (payloadBytes.Length > 1)
            {
                try
                {
                    payloadText = Encoding.UTF8.GetString(payloadBytes);

                    payloadJson = JObject.Parse(payloadText);
                }
                catch (FormatException fex)
                {
                    _logger.LogInformation(fex, "Uplink- DeviceId:{0} PacketId:{1} Convert.ToString(payloadBytes) failed", payload.DeviceId, payload.PacketId);
                }
                catch (JsonReaderException jrex)
                {
                    _logger.LogInformation(jrex, "Uplink- DeviceId:{0} PacketId:{1} JObject.Parse(payloadText) failed", payload.DeviceId, payload.PacketId);
                }
            }

            JObject telemetryEvent = new JObject();

            Dictionary<string, string> properties = new Dictionary<string, string>();

            payloadFormatterUplink.Evaluate(properties, payload.OrganizationId, payload.DeviceId, context.DeviceType, payload.UserApplicationId, telemetryEvent, payloadJson, payloadText, payloadBytes);

            telemetryEvent.TryAdd("packetId", payload.PacketId);
            telemetryEvent.TryAdd("deviceType", payload.DeviceType);
            telemetryEvent.TryAdd("DeviceID", payload.DeviceId);
            telemetryEvent.TryAdd("OrganizationId", payload.OrganizationId);
            telemetryEvent.TryAdd("UserApplicationId", payload.UserApplicationId);
            telemetryEvent.TryAdd("SwarmHiveReceivedAtUtc", payload.SwarmHiveReceivedAtUtc.ToString("s", CultureInfo.InvariantCulture));
            telemetryEvent.TryAdd("UplinkWebHookReceivedAtUtc", payload.UplinkWebHookReceivedAtUtc.ToString("s", CultureInfo.InvariantCulture));
            telemetryEvent.TryAdd("DataLength", payload.Length);
            telemetryEvent.TryAdd("Data", payload.Data);
            telemetryEvent.TryAdd("Status", payload.Status);
            telemetryEvent.TryAdd("Client", payload.Client);

            _logger.LogDebug("Uplink-DeviceId:{0} PacketId:{1} TelemetryEvent:{0}", payload.DeviceId, payload.PacketId, JsonConvert.SerializeObject(telemetryEvent, Formatting.Indented));

            using (Message ioTHubmessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryEvent))))
            {
                // This is so nasty but can't find a better way
                foreach( var property in properties ) 
                {
                    ioTHubmessage.Properties.TryAdd( property.Key, property.Value);
                }

                ioTHubmessage.Properties.TryAdd("PacketId", payload.PacketId.ToString());
                ioTHubmessage.Properties.TryAdd("DeviceType", payload.DeviceType.ToString());
                ioTHubmessage.Properties.TryAdd("DeviceId", payload.DeviceId.ToString());
                ioTHubmessage.Properties.TryAdd("UserApplicationId", payload.UserApplicationId.ToString());
                ioTHubmessage.Properties.TryAdd("OrganizationId", payload.OrganizationId.ToString());
                ioTHubmessage.Properties.TryAdd("Client", payload.Client);

                await deviceClient.SendEventAsync(ioTHubmessage);
            };

            _logger.LogInformation("Uplink-DeviceID:{deviceId} PacketId:{1} SendEventAsync success", payload.DeviceId, payload.PacketId);
        }
    }
}
