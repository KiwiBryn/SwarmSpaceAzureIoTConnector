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

            Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

            _logger.LogInformation("Downlink-IoT Hub DeviceID:{DeviceId} LockToken:{LockToken}", context.DeviceId, message.LockToken);


            try
            {
                deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(context.DeviceId.ToString(), context));

                // Check that Message has property, UserApplicationId so it can be processed correctly
                if (!message.Properties.TryGetValue("UserApplicationId", out string value) || !ushort.TryParse(message.Properties["UserApplicationId"], out ushort userApplicationId))
                {
                    _logger.LogInformation("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} UserApplicationId property missing or invalid", context.DeviceId, message.LockToken);

                    await deviceClient.RejectAsync(message);

                    return;
                }

                if ((userApplicationId < Models.Constants.UserApplicationIdMinimum) || (userApplicationId > Models.Constants.UserApplicationIdMaximum))
                {
                    _logger.LogInformation("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} UserApplicationId:{userApplicationId} UserApplicationId property invalid {UserApplicationIdMinimum} to {UserApplicationIdMaximum}", context.DeviceId, message.LockToken, userApplicationId, Models.Constants.UserApplicationIdMinimum, Models.Constants.UserApplicationIdMaximum);

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
                    _logger.LogWarning(fex, "Downlink-DeviceId:{DeviceId} LockToken:{LockToken} Convert.ToString(payloadBytes) failed", context.DeviceId, message.MessageId, payloadBytes);
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
                        _logger.LogWarning(jex, "Downlink-DeviceId:{DeviceId} LockToken:{LockToken} JObject.Parse failed", context.DeviceId, message.LockToken);
                    }
                }

                // Retrieve the payload formatter, if cache miss get blob using UserApplicationId, then "compile" and cache the binary.
                IFormatterDownlink swarmSpaceFormatterDownlink;

                try
                {
                    swarmSpaceFormatterDownlink = await _payloadFormatterCache.DownlinkGetAsync(userApplicationId);
                }
                catch (CSScriptLib.CompilerException cex)
                {
                    _logger.LogWarning(cex, "Downlink-DeviceID:{deviceId} LockToken:{LockToken} UserApplicationId:{UserApplicationId} payload formatter compilation failed", context.DeviceId, message.LockToken, userApplicationId);

                    await deviceClient.RejectAsync(message);

                    return;
                }

                byte[] payloadData = swarmSpaceFormatterDownlink.Evaluate(message.Properties, context.OrganisationId, context.DeviceId, context.DeviceType, userApplicationId, payloadJson, payloadText, payloadBytes);

                await _swarmSpaceBumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, userApplicationId, payloadData);

                _logger.LogInformation("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} UserAplicationId:{userApplicationId} Payload:{4}", context.DeviceId, message.LockToken, userApplicationId, BitConverter.ToString(payloadData));

                await deviceClient.CompleteAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Downlink-MessageHandler processing failed");

                throw;
            }
        }
    }
}
