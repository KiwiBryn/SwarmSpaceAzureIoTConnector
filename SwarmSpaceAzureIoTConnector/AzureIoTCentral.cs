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
        public async Task AzureIoTCentralMessageHandler(Message message, object userContext)
        {
            DeviceClient deviceClient;

            Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

            try
            {
                _logger.LogInformation("Downlink-IoT Central DeviceID:{DeviceId} LockToken:{LockToken} OrganisationId:{OrganisationId}", context.DeviceId, message.LockToken,context.OrganisationId);

                using (message)
                {
                    deviceClient = await _azuredeviceClients.GetOrAddAsync<DeviceClient>(context.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(context.DeviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService));

                    // Check that Message has property, method-name so it can be processed correctly
                    if (!message.Properties.TryGetValue("method-name", out string methodName) || string.IsNullOrWhiteSpace(methodName))
                    {
                        _logger.LogWarning("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} method-name:property missing or empty", context.DeviceId, message.LockToken);

                        await deviceClient.RejectAsync(message);

                        return;
                    }

                    // Look up the method settings to get UserApplicationId and optional downlink message payload JSON.
                    if ((_azureIoTSettings.AzureIoTCentral.Methods == null) || !_azureIoTSettings.AzureIoTCentral.Methods.TryGetValue(methodName, out Models.AzureIoTCentralMethodSetting methodSetting))
                    {
                        _logger.LogWarning("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} method-name:{methodName} has no settings", context.DeviceId, message.LockToken, methodName);

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
                        _logger.LogWarning(fex, "Downlink-DeviceId:{DeviceId} LockToken:{LockToken} Encoding.UTF8.GetString(2) failed", context.DeviceId, message.LockToken, BitConverter.ToString(payloadBytes));
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
                            _logger.LogWarning("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} method-name:{methodName} IsPayloadValidJson:{Payload} failed", context.DeviceId, message.LockToken, methodName, methodSetting.Payload);

                            await deviceClient.RejectAsync(message);

                            return;
                        }
                    }
                    else
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
                    }

                    _logger.LogInformation("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} Method:{methodName} UserAplicationId:{userApplicationId} Payload:{4}", context.DeviceId, message.LockToken, methodName, methodSetting.UserApplicationId, BitConverter.ToString(payloadBytes));

                    // Retrieve the payload formatter, if cache miss get blob using UserApplicationId, then "compile" and cache the binary.
                    IFormatterDownlink swarmSpaceFormatterDownlink;

                    try
                    {
                        swarmSpaceFormatterDownlink = await _payloadFormatterCache.DownlinkGetAsync(methodSetting.UserApplicationId);
                    }
                    catch (CSScriptLib.CompilerException cex)
                    {
                        _logger.LogInformation(cex, "Downlink-DeviceID:{deviceId} LockToken:{LockToken} UserApplicationId:{UserApplicationId} payload formatter compilation failed", context.DeviceId, message.LockToken, methodSetting.UserApplicationId);

                        await deviceClient.RejectAsync(message);

                        return;
                    }

                    byte[] payloadData = swarmSpaceFormatterDownlink.Evaluate(context.OrganisationId, context.DeviceId, context.DeviceType, methodSetting.UserApplicationId, payloadJson, payloadText, payloadBytes);

                    await _swarmSpaceBumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, methodSetting.UserApplicationId, payloadData);

                    await deviceClient.CompleteAsync(message);

                    _logger.LogInformation("Downlink-DeviceID:{DeviceId} LockToken:{LockToken} Method:{methodName} UserAplicationId:{userApplicationId} Payload:{4}", context.DeviceId, message.LockToken, methodName, methodSetting.UserApplicationId, BitConverter.ToString(payloadData));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Downlink-DeviceID:{DeviceId} LockToken:{LockToken} MessageHandler processing failed", context.DeviceId, message.LockToken);

                throw;
            }
        }
    }
}
