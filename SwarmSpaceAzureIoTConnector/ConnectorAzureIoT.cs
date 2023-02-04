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
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Logging;

    partial class AzureDeviceClientCache : IAzureDeviceClientCache
    {
        private async Task<MethodResponse> AzureIoTHubClientDefaultMethodHandler(MethodRequest methodRequest, object userContext)
        {
            Models.AzureIoTDeviceClientContext context = (Models.AzureIoTDeviceClientContext)userContext;

            _logger.LogWarning("Downlink-DeviceID:{deviceId} DefaultMethodHandler name:{Name} payload:{DataAsJson}", context.DeviceId, methodRequest.Name, methodRequest.DataAsJson);

            return new MethodResponse(Encoding.ASCII.GetBytes("{\"message\":\"The SwarmSpace Connector does not support Direct Methods.\"}"), 400);
        }
    }
}
