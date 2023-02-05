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
    using System.Security.Cryptography;
    using System;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Provisioning.Client.PlugAndPlay;
    using Microsoft.Azure.Devices.Provisioning.Client.Transport;
    using Microsoft.Azure.Devices.Provisioning.Client;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Logging;

    partial class AzureDeviceClientCache : IAzureDeviceClientCache
    {
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

        private async Task<MethodResponse> AzureIoTHubClientDefaultMethodHandler(MethodRequest methodRequest, object userContext)
        {
            Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

            _logger.LogWarning("Downlink-DeviceID:{deviceId} DefaultMethodHandler name:{Name} payload:{DataAsJson}", context.DeviceId, methodRequest.Name, methodRequest.DataAsJson);

            return new MethodResponse(Encoding.ASCII.GetBytes("{\"message\":\"The SwarmSpace Connector does not support Direct Methods.\"}"), 400);
        }
    }
}
