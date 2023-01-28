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
// Used new Visual Studio feature "Edit->Paste Special->Paste JSON as Clases" functionality
//
//---------------------------------------------------------------------------------
namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector.Models
{
    using System;

    public class UplinkPayloadQueueDto
    {
        public ulong PacketId { get; set; }
        public byte DeviceType { get; set; }
        public uint DeviceId { get; set; }
        public ushort UserApplicationId { get; set; }
        public uint OrganizationId { get; set; }
        public string Data { get; set; } = string.Empty;
        public byte Length { get; set; }
        public int Status { get; set; }
        public DateTime SwarmHiveReceivedAtUtc { get; set; }
        public DateTime UplinkWebHookReceivedAtUtc { get; set; }
        public string Client { get; set; } = string.Empty;
    }
}
