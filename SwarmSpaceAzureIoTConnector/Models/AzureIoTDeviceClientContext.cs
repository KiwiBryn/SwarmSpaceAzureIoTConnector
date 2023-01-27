//---------------------------------------------------------------------------------
// Copyright (c) December 2022, devMobile Software
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
namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector.Models
{
    public class AzureIoTDeviceClientContext
    {
        public uint OrganisationId { get; set; } //public uint OrganisationId { get; set; }

        //public int UserApplicationId { get; set; } deprecated

        public uint DeviceId { get; set; } //public uint DeviceId { get; set; }

        public int DeviceType { get; set; }
    }
}
