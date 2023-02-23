// Copyright (c) February 2023, devMobile Software
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
// These classes were "adapted" from the previous ones generated with NSwag
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector.Models
{
    using System.Text.Json.Serialization;

    internal class LoginRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }

    internal class LoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }

    // Should be able to delete a lot of these fields
    public class Device
    {
        /// <summary>
        /// 3 bit device type. 1 = fieldBee, 2 = stratoBee, 3 = spaceBee, 4 = groundBee, 5 = Hive
        /// </summary>
        [JsonPropertyName("deviceType")]
        public int DeviceType { get; set; }

        /// <summary>
        /// 18 bit device id
        /// </summary>
        [JsonPropertyName("deviceId")]
        public int DeviceId { get; set; }

        /// <summary>
        /// Device name. By default, looks like F-0x00010 for deviceType = 1 and deviceId = 16
        /// </summary>
        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; }

        /* These might be useful in future
        /// <summary>
        /// Status of device
        /// </summary>
        //[JsonPropertyName("status")]
        public int Status { get; set; }

        /// <summary>
        /// Whether or not this device can receive return messages from the hive. Only applies to field devices.
        /// </summary>
        //[JsonPropertyName("twoWayEnabled")]
        public bool TwoWayEnabled { get; set; } = false;
        */
    }

    public partial class MessageSendRequest
    {
        /// <summary>
        /// Swarm device type
        /// </summary>
        [JsonPropertyName("deviceType")]
        public int DeviceType { get; set; }

        /// <summary>
        /// Swarm device ID
        /// </summary>
        [JsonPropertyName("deviceId")]
        public int DeviceId { get; set; }

        /// <summary>
        /// Application ID
        /// </summary>
        [JsonPropertyName("userApplicationId")]
        public int UserApplicationId { get; set; }

        /// <summary>
        /// Organization ID
        /// </summary>
        [JsonPropertyName("organizationId")]
        public int OrganizationId { get; set; }

        /// <summary>
        /// Base64 encoded data string
        /// </summary>
        [JsonPropertyName("data")]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public byte[] Data { get; set; }
    }

    public class MessageSendResponse
    {
        /// <summary>
        /// Swarm packet ID.
        /// </summary>
        [JsonPropertyName("packetId")]
        public long PacketId { get; set; }

        /// <summary>
        /// Submission status, "OK" or "ERROR" with a description of the error.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}



