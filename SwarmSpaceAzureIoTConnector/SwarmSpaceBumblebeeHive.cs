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
namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public interface ISwarmSpaceBumblebeeHive
    {
        public Task<ICollection<Models.Device>> DeviceListAsync(CancellationToken cancellationToken);

        public Task SendAsync(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, byte[] payload, CancellationToken cancellationToken = default);
    }

    public class SwarmSpaceBumblebeeHive : ISwarmSpaceBumblebeeHive
    {
        private string _token = string.Empty;
        private DateTime _tokenActivityAtUtC = DateTime.MinValue;
        private readonly ILogger<SwarmSpaceBumblebeeHive> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Models.SwarmBumblebeeHiveSettings _bumblebeeHiveSettings;

        public SwarmSpaceBumblebeeHive(ILogger<SwarmSpaceBumblebeeHive> logger, IHttpClientFactory httpClientFactory,
                                  IOptions<Models.SwarmBumblebeeHiveSettings> bumblebeeHiveSettings) =>
                (_logger, _httpClientFactory, _bumblebeeHiveSettings) =
                (logger, httpClientFactory, bumblebeeHiveSettings.Value);

        private async Task TokenRefresh(CancellationToken cancellationToken)
        {
            if ((_tokenActivityAtUtC + _bumblebeeHiveSettings.TokenValidFor) < DateTime.UtcNow)
            {
                _logger.LogInformation("Login:{0}", _bumblebeeHiveSettings.UserName);

                HttpClient httpClient = _httpClientFactory.CreateClient();

                Models.LoginRequest loginRequest = new Models.LoginRequest()
                {
                    Username = _bumblebeeHiveSettings.UserName,
                    Password = _bumblebeeHiveSettings.Password
                };

                var httpResponse = await httpClient.PostAsJsonAsync<Models.LoginRequest>(_bumblebeeHiveSettings.BaseUrl + "/login", loginRequest, cancellationToken);

                httpResponse.EnsureSuccessStatusCode();

                Models.LoginResponse response = System.Text.Json.JsonSerializer.Deserialize<Models.LoginResponse>(await httpResponse.Content.ReadAsStringAsync());

                _token = response.Token;
                _tokenActivityAtUtC = DateTime.UtcNow;

                _logger.LogInformation("Login- UserName:{0} Token:{1}...{2}", _bumblebeeHiveSettings.UserName, _token[..5], _token[^5..]);
            }
        }

        public async Task<ICollection<Models.Device>> DeviceListAsync(CancellationToken cancellationToken)
        {
            await TokenRefresh(cancellationToken);

            HttpClient httpClient = _httpClientFactory.CreateClient();

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _token);

            return await httpClient.GetFromJsonAsync<ICollection<Models.Device>>(_bumblebeeHiveSettings.BaseUrl + "/api/v1/devices", cancellationToken);
        }

        public async Task SendAsync(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, byte[] data, CancellationToken cancellationToken)
        {
            await TokenRefresh(cancellationToken);

            _logger.LogInformation("SendAsync: OrganizationId:{0} DeviceType:{1} DeviceId:{2} UserApplicationId:{3} Data:{4} Enabled:{5}", organisationId, deviceType, deviceId, userApplicationId, Convert.ToBase64String(data), _bumblebeeHiveSettings.DownlinkEnabled);

            // To save the limited monthly allocation of mesages downlinks can be disabled
            if (_bumblebeeHiveSettings.DownlinkEnabled)
            {
                HttpClient httpClient = _httpClientFactory.CreateClient();

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", _token);

                Models.MessageSendRequest messageSendRequest = new Models.MessageSendRequest()
                {
                    OrganizationId = (int)organisationId, // Doesn't matter
                    DeviceType = deviceType,
                    DeviceId = (int)deviceId,
                    UserApplicationId = userApplicationId,
                    Data = data,
                };

                var httpResponse = await httpClient.PostAsJsonAsync<Models.MessageSendRequest>(_bumblebeeHiveSettings.BaseUrl + "/api/v1/messages", messageSendRequest, cancellationToken);

                httpResponse.EnsureSuccessStatusCode();

                Models.MessageSendResponse response = System.Text.Json.JsonSerializer.Deserialize<Models.MessageSendResponse>(await httpResponse.Content.ReadAsStringAsync());

                _logger.LogInformation("SendAsync-Result:{Status} PacketId:{PacketId}", response.Status, response.PacketId);
            }
        }
    }
}