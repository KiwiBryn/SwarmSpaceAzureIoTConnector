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
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using PayloadFormatter; // Short cut namespace for V1 formatters

    partial class AzureDeviceClientCache : IAzureDeviceClientCache
    {
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

    }
}
