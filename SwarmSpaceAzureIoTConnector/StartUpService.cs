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
//---------------------------------------------------------------------------------
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector.StartUpService))]
namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

  
    public class StartUpService : BackgroundService
    {
        private readonly ILogger<StartUpService> _logger;
        private readonly ISwarmSpaceBumblebeeHive _swarmSpaceBumblebeeHive;
        private readonly Models.ApplicationSettings _applicationSettings;
        private readonly IAzureDeviceClientCache _azureDeviceClientCache;

        public StartUpService(ILogger<StartUpService> logger, IAzureDeviceClientCache azureDeviceClientCache, ISwarmSpaceBumblebeeHive swarmSpaceBumblebeeHive, IOptions<Models.ApplicationSettings> applicationSettings)//, IOptions<Models.AzureIoTSettings> azureIoTSettings)
        {
            _logger = logger;
            _azureDeviceClientCache = azureDeviceClientCache;
            _swarmSpaceBumblebeeHive = swarmSpaceBumblebeeHive;
            _applicationSettings = applicationSettings.Value;
            //_azureIoTSettings = azureIoTSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            _logger.LogInformation("StartUpService.ExecuteAsync start");

            try
            {
                _logger.LogInformation("BumblebeeHiveCacheRefresh start");

                await _swarmSpaceBumblebeeHive.Login(cancellationToken);

                foreach (SwarmSpace.BumblebeeHiveClient.Device device in await _swarmSpaceBumblebeeHive.DeviceListAsync(cancellationToken))
                {
                    _logger.LogInformation("BumblebeeHiveCacheRefresh DeviceId:{DeviceId} DeviceName:{DeviceName}", device.DeviceId, device.DeviceName);

                    Models.AzureIoTDeviceClientContext context = new Models.AzureIoTDeviceClientContext()
                    {
                        // TODO seems a bit odd getting this from application settings
                        OrganisationId = _applicationSettings.OrganisationId,
                        //UserApplicationId = device.UserApplicationId, deprecated
                        DeviceType = (byte)device.DeviceType,
                        DeviceId = (uint)device.DeviceId,
                    };

                    await _azureDeviceClientCache.GetOrAddAsync(device.DeviceId.ToString(), context);
                }

                _logger.LogInformation("BumblebeeHiveCacheRefresh finish");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartUpService.ExecuteAsync error");

                throw;
            }

            _logger.LogInformation("StartUpService.ExecuteAsync finish");
        }
    }
}
