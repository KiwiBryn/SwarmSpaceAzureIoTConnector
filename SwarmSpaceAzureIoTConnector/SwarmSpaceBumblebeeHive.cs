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
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using devMobile.SwarmSpace.BumblebeeHiveClient;

    public interface ISwarmSpaceBumblebeeHive
    {
        public Task Login(CancellationToken cancellationToken);

        public Task Logout(CancellationToken cancellationToken);

        public Task<ICollection<Device>> DeviceListAsync(CancellationToken cancellationToken);

        public Task SendAsync(int organisationId, int deviceId, int deviceType, int userApplicationId, byte[] payload);
    }

    public class SwarmSpaceBumblebeeHive : ISwarmSpaceBumblebeeHive
    {
        private string _token = string.Empty;
        private readonly ILogger<SwarmSpaceBumblebeeHive> _logger;
        private readonly Models.BumblebeeHiveSettings _bumblebeeHiveSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public SwarmSpaceBumblebeeHive(ILogger<SwarmSpaceBumblebeeHive> logger,
                                IHttpClientFactory httpClientFactory,
                                IOptions<Models.BumblebeeHiveSettings> bumblebeeHiveSettings) =>
                (_logger, _httpClientFactory, _bumblebeeHiveSettings) =
                (logger, httpClientFactory, bumblebeeHiveSettings.Value);

        public async Task Login(CancellationToken cancellationToken)
        {
            using (HttpClient httpClient = _httpClientFactory.CreateClient())
            {
                Client client = new Client(httpClient);

                client.BaseUrl = _bumblebeeHiveSettings.BaseUrl;

                LoginForm loginForm = new LoginForm()
                {
                    Username = _bumblebeeHiveSettings.UserName,
                    Password = _bumblebeeHiveSettings.Password,
                };

                _logger.LogInformation("Login:{0}", loginForm.Username);

                Response response = await client.PostLoginAsync(loginForm, cancellationToken);

                _logger.LogInformation("Login:{0} Token:{1}...{2}", loginForm.Username, response.Token[..5], response.Token[^5..]);

                _token = response.Token;
            }
        }

        public async Task Logout(CancellationToken cancellationToken)
        {
            using (HttpClient httpClient = _httpClientFactory.CreateClient())
            {
                Client client = new Client(httpClient);

                client.BaseUrl = _bumblebeeHiveSettings.BaseUrl;

                await client.GetLogoutAsync(cancellationToken);

                _logger.LogInformation("Logout: Token:{0}...{1}", _token[..5], _token[^5..]);
            }
        }

        public async Task<ICollection<Device>> DeviceListAsync(CancellationToken cancellationToken)
        {
            using (HttpClient httpClient = _httpClientFactory.CreateClient())
            {
                Client client = new Client(httpClient);

                client.BaseUrl = _bumblebeeHiveSettings.BaseUrl;

                httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {_token}");

                return await client.GetDevicesAsync(null, null, null, null, null, null, null, null, null, cancellationToken);
            }
        }

        public async Task SendAsync(int organisationId, int deviceId, int deviceType, int userApplicationId, byte[] data)
        {
            using (HttpClient httpClient = _httpClientFactory.CreateClient())
            {
                Client client = new Client(httpClient);

                client.BaseUrl = _bumblebeeHiveSettings.BaseUrl;

                httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {_token}");

                UserMessage userMessage = new UserMessage()
                {
                    OrganizationId = organisationId,
                    DeviceType = deviceType,
                    DeviceId = deviceId,
                    UserApplicationId = userApplicationId,
                    Data = data,
                };

                _logger.LogInformation("SendAsync: OrganizationId:{0} DeviceType:{1} DeviceId:{2} UserApplicationId:{3} Data:{4} Enabled:{5}", organisationId, deviceType, deviceId, userApplicationId, Convert.ToBase64String(data), _bumblebeeHiveSettings.DownlinkEnabled);

                // To save the limited monthly allocation of mesages downlinks can be disabled
                if (_bumblebeeHiveSettings.DownlinkEnabled)
                {
                    await client.AddApplicationMessageAsync(userMessage);
                }
            }
        }
    }
}