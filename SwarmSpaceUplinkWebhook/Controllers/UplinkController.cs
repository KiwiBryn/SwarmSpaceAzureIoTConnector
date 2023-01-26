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
namespace devmobile.IoT.SwarmSpaceAzureIoTConnector.SwarmSpace.UplinkWebhook.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Azure.Storage.Queues;

    using Newtonsoft.Json;

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
        public async Task<IActionResult> Post([FromHeader(Name = "x-api-key")] string xApiKeyValue, [FromBody] Models.UplinkPayload payload)
        {
            if (!_applicationSettings.XApiKeys.TryGetValue(xApiKeyValue, out string apiKeyName))
            {
                return this.Unauthorized("Unauthorized client");
            }

            _logger.LogWarning("Authentication successful X-API-KEY name:{0}", apiKeyName);

            payload.Client = apiKeyName;

            if (payload.HiveRxTimeUtc == DateTime.MinValue)
            {
                _logger.LogWarning("Receive time validation failed");

                return this.BadRequest("Receive time validation failed");
            }

            QueueClient queueClient = _queueServiceClient.GetQueueClient("uplink");

            await queueClient.SendMessageAsync(JsonConvert.SerializeObject(payload));

            return this.Ok();
        }
    }
}
