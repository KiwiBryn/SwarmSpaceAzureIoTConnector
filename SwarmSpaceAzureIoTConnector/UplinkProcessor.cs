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
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    public class UplinkProcessor
    {
        private readonly ILogger _logger;

        public UplinkProcessor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UplinkProcessor>();
        }

        [Function("UplinkQueueTrigger")]
        public async Task Run([QueueTrigger("uplink1", Connection = "AzureFunctionsStorage")] string payload)
        {
            Models.UplinkPayload uplinkPayload = JsonConvert.DeserializeObject<Models.UplinkPayload>(payload);

            _logger.LogInformation($"C# Queue trigger function processed: {uplinkPayload.PacketId}");
        }
    }
}
