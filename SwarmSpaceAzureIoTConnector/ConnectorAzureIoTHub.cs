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

    public partial class Connector
    {
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

                    await _swarmSpaceBumblebeeHive.SendAsync(context.OrganisationId, context.DeviceId, context.DeviceType, userApplicationId, message.GetBytes());

                    _logger.LogInformation("Downlink-IoT Hub DeviceID:{DeviceId} MessageID:{MessageId} UserApplicationId:{userApplicationId}", context.DeviceId, message.MessageId, userApplicationId);

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
