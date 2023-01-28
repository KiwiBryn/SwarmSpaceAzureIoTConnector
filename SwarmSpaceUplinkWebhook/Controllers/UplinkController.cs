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
namespace devmobile.IoT.SwarmSpaceAzureIoTConnector.SwarmSpace.UplinkWebhook.Controllers
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Azure.Storage.Queues;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    [Route("[controller]")]
    [ApiController]
    public class UplinkController : Controller
    {
        private readonly Models.ApplicationSettings _applicationSettings;
        private readonly ILogger<UplinkController> _logger;
        private readonly QueueServiceClient _queueServiceClient;

        public UplinkController(IOptions<Models.ApplicationSettings> applicationSettings, QueueServiceClient queueServiceClient, ILogger<UplinkController> logger)
        {
            _applicationSettings = applicationSettings.Value;
            _queueServiceClient = queueServiceClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromHeader(Name = "x-api-key")] string xApiKeyValue, [FromBody] Models.UplinkPayloadWebDto payloadWeb)
        {
            if (!_applicationSettings.XApiKeys.TryGetValue(xApiKeyValue, out string apiKeyName))
            {
                _logger.LogWarning("Authentication unsuccessful X-API-KEY value:{xApiKeyValue}", xApiKeyValue);

                return this.Unauthorized("Unauthorized client");
            }
            _logger.LogInformation("Authentication successful X-API-KEY value:{xApiKeyValue}", xApiKeyValue);

            // Could of used AutoMapper but didn't seem worth it for one place
            Models.UplinkPayloadQueueDto payloadQueue = new()
            {
                PacketId = payloadWeb.PacketId,
                DeviceType = payloadWeb.DeviceType,
                DeviceId = payloadWeb.DeviceId,
                UserApplicationId = payloadWeb.UserApplicationId,
                OrganizationId = payloadWeb.OrganizationId,
                Data = payloadWeb.Data,
                Length = payloadWeb.Len,
                Status = payloadWeb.Status,
                SwarmHiveReceivedAtUtc = payloadWeb.HiveRxTime,
                UplinkWebHookReceivedAtUtc = DateTime.UtcNow,
                Client = apiKeyName,                 
            };

            _logger.LogInformation("SendAsync queue name:{QueueName}", _applicationSettings.QueueName);

            await _queueServiceClient.GetQueueClient(_applicationSettings.QueueName).SendMessageAsync(JsonSerializer.Serialize(payloadQueue));

            return this.Ok();
        }
    }
}
