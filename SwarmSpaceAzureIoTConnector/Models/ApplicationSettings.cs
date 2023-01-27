﻿// Copyright (c) January 2023, devMobile Software
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
    public class ApplicationSettings
    {
        public string PayloadFormattersUplinkFilePath { get; set; } = string.Empty;
        public string PayloadFormatterUplinkDefault { get; set; } = string.Empty;

        public string PayloadFormattersDownlinkFilePath { get; set; } = string.Empty;
        public string PayloadFormatterDownlinkDefault { get; set; } = string.Empty;

        public int OrganisationId { get; set; }

    }
}
