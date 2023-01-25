//---------------------------------------------------------------------------------
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
namespace devmobile.IoT.SwarmConnector.SwarmSpace.UplinkWebhook
{
    // TODO: Add status enum ? Message status. Possible values: 0 = incoming message (from a device) 1 = outgoing message (to a device) 2 = incoming message, acknowledged as seen by customer. OR a outgoing message packet is on groundstation 3 = outgoing message, packet is on satellite -1 = error -3 = failed to deliver, retrying -4 = failed to deliver, will not re-attempt
    public static class Constants
    {
        public const string ApiKeyHeaderName = "x-api-key";

        public const long PacketIdMinimum = long.MinValue;
        public const long PacketIdMaximum = long.MaxValue;

        //TODO 3 bit device type. 1 = fieldBee, 2 = stratoBee, 3 = spaceBee, 4 = groundBee, 5 = Hive
        public const byte DeviceTypeMinimum = byte.MinValue; 
        public const byte DeviceTypeMaximum = byte.MaxValue;

        public const uint DeviceIdMinimum = uint.MinValue;
        public const uint DeviceIdMaximum = uint.MaxValue;

        public const ushort UserApplicationIdMinimum = ushort.MinValue;
        public const ushort UserApplicationIdMaximum = ushort.MaxValue; //Swarm reserves 65000 - 65535.

        public const uint OrganisationIdMinimum = uint.MinValue;
        public const uint OrganisationIdMaximum = uint.MaxValue;

        public const byte PayloadLengthMinimum = byte.MinValue;
        public const byte PayloadLengthMaximum = 192;
    }
}
