//---------------------------------------------------------------------------------
// Copyright (c) December 2021, devMobile Software
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
    using System.Collections.Generic;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public enum ApplicationType
    {
        Undefined = 0,
        AzureIotHub,
        AzureIoTCentral
    }

    public class AzureIoTSettings
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ApplicationType ApplicationType { get; set; }

        public AzureIotHubSettings AzureIotHub { get; set; }

        public AzureIoTCentralSetting AzureIoTCentral { get; set; }
    }

    public enum AzureIotHubConnectionType
    {
        Undefined = 0,
        DeviceConnectionString,
        DeviceProvisioningService
    }

    public class AzureIotHubSettings
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureIotHubConnectionType ConnectionType { get; set; }

        public string ConnectionString { get; set; } = string.Empty;

        public AzureDeviceProvisioningServices DeviceProvisioningServices { get; set; }

        public string DtdlModelId { get; set; } = string.Empty;
    }

    public class AzureIoTCentralSetting
    {
        public AzureDeviceProvisioningServices DeviceProvisioningService { get; set; }

        public Dictionary<string, AzureIoTCentralMethodSetting> Methods { get; set; }
    }

    public class AzureIoTCentralMethodSetting
    {
        public byte UserApplicationId { get; set; } = 0;

        public string Payload { get; set; } = string.Empty;
    }

    public class AzureDeviceProvisioningServices
    {
        public string GlobalDeviceEndpoint { get; set; } = string.Empty;

        public string IdScope { get; set; } = string.Empty;

        public string GroupEnrollmentKey { get; set; } = string.Empty;

        public string DtdlModelId { get; set; } = string.Empty;
    }
}
