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
/*
 https://json2csharp.com/
    
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Root
    {
        public int packetId { get; set; }
        public int deviceType { get; set; }
        public int deviceId { get; set; }
        public int userApplicationId { get; set; }
        public int organizationId { get; set; }
        public string data { get; set; }
        public int len { get; set; }
        public int status { get; set; }
        public DateTime hiveRxTime { get; set; }
    }
*/

namespace devmobile.IoT.SwarmSpaceAzureIoTConnector.Connector.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    using Newtonsoft.Json;

    public class UplinkPayload
    {
        [Required]
        [JsonProperty("packetId")]
        [Range(Constants.PacketIdMinimum, Constants.PacketIdMaximum, ErrorMessage = "{0} must be between {1} and {2}")]
        public long PacketId { get; set; }

        [Required]
        [JsonProperty("deviceType")]
        [Range(Constants.DeviceTypeMinimum, Constants.DeviceTypeMaximum, ErrorMessage = "{0} must be between {1} and {2}")]
        public byte DeviceType { get; set; }

        [Required]
        [JsonProperty("deviceId")]
        [Range(Constants.DeviceIdMinimum, Constants.DeviceIdMaximum, ErrorMessage = "{0} must be between {1} and {2}")]
        public uint DeviceId { get; set; } // public uint DeviceId { get; set; }

        [Required]
        [JsonProperty("userApplicationId")]
        [Range(Constants.UserApplicationIdMinimum, Constants.UserApplicationIdMaximum, ErrorMessage = "{0} must be between {1} and {2}")]
        public ushort UserApplicationId { get; set; }

        [Required]
        [JsonProperty("organizationId")]
        [Range(Constants.OrganisationIdMinimum, Constants.OrganisationIdMaximum, ErrorMessage = "{0} must be between {1} and {2}")]
        public uint OrganizationId { get; set; }

        [Required]
        [JsonProperty("data")]
        public string Data { get; set; } = String.Empty;

        [Required]
        [JsonProperty("Len")]
        [Range(Constants.PayloadLengthMinimum, Constants.PayloadLengthMaximum, ErrorMessage = "UplinkPayload value for {0} must be between {1} and {2}")]
        public byte Length { get; set; }

        [Required]
        [JsonProperty("status")]
        public sbyte Status { get; set; }

        [Required]
        [JsonProperty("hiveRxTime")]
        public DateTime HiveRxTimeUtc { get; set; }

        [JsonProperty("Client", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Client { get; set; } = string.Empty;
    }
}
