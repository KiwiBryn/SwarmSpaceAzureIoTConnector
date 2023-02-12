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
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using RestSharp;

    public interface ISwarmSpaceBumblebeeHive
    {
        public Task<ICollection<Models.Device>> DeviceListAsync(CancellationToken cancellationToken);

        public Task SendAsync(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, byte[] payload, CancellationToken cancellationToken);
    }

    public class SwarmSpaceBumblebeeHive : ISwarmSpaceBumblebeeHive
    {
        private string _token = string.Empty;
        private DateTime _TokenActivityAtUtC = DateTime.MinValue;
        private readonly ILogger<SwarmSpaceBumblebeeHive> _logger;
        private readonly Models.SwarmBumblebeeHiveSettings _bumblebeeHiveSettings;

        public SwarmSpaceBumblebeeHive(ILogger<SwarmSpaceBumblebeeHive> logger,
                                  IOptions<Models.SwarmBumblebeeHiveSettings> bumblebeeHiveSettings) =>
                (_logger, _bumblebeeHiveSettings) =
                (logger, bumblebeeHiveSettings.Value);

        private async Task TokenRefresh(CancellationToken cancellationToken)
        {
            if ((_TokenActivityAtUtC + _bumblebeeHiveSettings.TokenValidFor) < DateTime.UtcNow)
            {
                _logger.LogInformation("Login:{0}", _bumblebeeHiveSettings.UserName);

                RestClientOptions restClientOptions = new RestClientOptions()
                {
                    BaseUrl = new Uri(_bumblebeeHiveSettings.BaseUrl),
                    ThrowOnAnyError = true,
                };

                RestClient client = new RestClient(restClientOptions);

                RestRequest request = new RestRequest("login", Method.Post);

                Models.LoginRequest loginRequest = new Models.LoginRequest()
                {
                    Username = _bumblebeeHiveSettings.UserName,
                    Password = _bumblebeeHiveSettings.Password
                };

                request.AddBody(loginRequest);

                var response = await client.PostAsync<Models.LoginResponse>(request, cancellationToken);

                _token = response.Token;

                _logger.LogInformation("Login- UserName:{0} Token:{1}...{2}", _bumblebeeHiveSettings.UserName, _token[..5], _token[^5..]);

                _TokenActivityAtUtC = DateTime.UtcNow;
            }
        }

        public async Task<ICollection<Models.Device>> DeviceListAsync(CancellationToken cancellationToken)
        {
            await TokenRefresh(cancellationToken);

            RestClientOptions restClientOptions = new RestClientOptions()
            {
                BaseUrl = new Uri(_bumblebeeHiveSettings.BaseUrl),
                ThrowOnAnyError = true,
            };

            RestClient client = new RestClient(restClientOptions);

            RestRequest request = new RestRequest("api/v1/devices");

            request.AddHeader("Authorization", $"bearer {_token}");

            return await client.GetAsync<ICollection<Models.Device>>(request);
        }

        public async Task SendAsync(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, byte[] data, CancellationToken cancellationToken)
        {
            await TokenRefresh(cancellationToken);

            _logger.LogInformation("SendAsync: OrganizationId:{0} DeviceType:{1} DeviceId:{2} UserApplicationId:{3} Data:{4} Enabled:{5}", organisationId, deviceType, deviceId, userApplicationId, Convert.ToBase64String(data), _bumblebeeHiveSettings.DownlinkEnabled);

            Models.MessageSendRequest message = new Models.MessageSendRequest()
            {
                OrganizationId = (int)organisationId,
                DeviceType = deviceType,
                DeviceId = (int)deviceId,
                UserApplicationId = userApplicationId,
                Data = data,
            };

            RestClientOptions restClientOptions = new RestClientOptions()
            {
                BaseUrl = new Uri(_bumblebeeHiveSettings.BaseUrl),
                ThrowOnAnyError = true,
            };

            RestClient client = new RestClient(restClientOptions);

            RestRequest request = new RestRequest("api/v1/messages", Method.Post);

            request.AddBody(message);

            request.AddHeader("Authorization", $"bearer {_token}");

            // To save the limited monthly allocation of mesages downlinks can be disabled
            if (_bumblebeeHiveSettings.DownlinkEnabled)
            {
                var response = await client.PostAsync<Models.MessageSendResponse>(request, cancellationToken);

                _logger.LogInformation("SendAsync-Result:{Status} PacketId:{PacketId}", response.Status, response.PacketId);
            }
        }
    }
}