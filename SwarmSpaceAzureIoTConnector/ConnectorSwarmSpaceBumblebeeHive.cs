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
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    public partial class Connector
    {
        /*
        [Function("BumblebeeHiveCacheRefresh")]
        public async Task<IActionResult> BumblebeeHiveCacheRefreshRun([HttpTrigger(AuthorizationLevel.Function, "get")] CancellationToken cancellationToken)
        {
            _logger.LogInformation("BumblebeeHiveCacheRefresh start");

            await _swarmSpaceBumblebeeHive.Login(cancellationToken);

            foreach (SwarmSpace.BumblebeeHiveClient.Device device in await _swarmSpaceBumblebeeHive.DeviceListAsync(cancellationToken))
            {
                _logger.LogInformation("BumblebeeHiveCacheRefresh DeviceId:{DeviceId} DeviceName:{DeviceName}", device.DeviceId, device.DeviceName);

                Models.AzureIoTDeviceClientContext context = new Models.AzureIoTDeviceClientContext()
                {
                    // TODO seems a bit odd getting this from application settings
                    OrganisationId = _applicationSettings.OrganisationId, 
                    //UserApplicationId = device.UserApplicationId, deprecated
                    DeviceType = (byte)device.DeviceType,
                    DeviceId = (uint)device.DeviceId,
                };

                switch (_azureIoTSettings.ApplicationType)
                {
                    case Models.ApplicationType.AzureIotHub:
                        switch (_azureIoTSettings.AzureIotHub.ConnectionType)
                        {
                            case Models.AzureIotHubConnectionType.DeviceConnectionString:
                                await _azureDeviceClientCache.GetOrAddAsync<DeviceClient>(device.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceConnectionStringConnectAsync(device.DeviceId.ToString(), context));
                                break;
                            case Models.AzureIotHubConnectionType.DeviceProvisioningService:
                                await _azureDeviceClientCache.GetOrAddAsync<DeviceClient>(device.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(device.DeviceId.ToString(), context, _azureIoTSettings.AzureIotHub.DeviceProvisioningService));
                                break;
                            default:
                                _logger.LogError("Azure IoT Hub ConnectionType unknown {0}", _azureIoTSettings.AzureIotHub.ConnectionType);

                                throw new NotImplementedException("AzureIoT Hub unsupported ConnectionType");
                        }
                        break;

                    case Models.ApplicationType.AzureIoTCentral:
                        await _azureDeviceClientCache.GetOrAddAsync<DeviceClient>(device.DeviceId.ToString(), (ICacheEntry x) => AzureIoTHubDeviceProvisioningServiceConnectAsync(device.DeviceId.ToString(), context, _azureIoTSettings.AzureIoTCentral.DeviceProvisioningService));
                        break;

                    default:
                        _logger.LogError("AzureIoT application type unknown {0}", _azureIoTSettings.ApplicationType);

                        throw new NotImplementedException("AzureIoT unsupported ApplicationType");
                }
            }

            _logger.LogInformation("BumblebeeHiveCacheRefresh finish");

            return new OkResult();
        }
        */
    }
}
