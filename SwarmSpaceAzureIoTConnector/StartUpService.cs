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
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class StartUpService : BackgroundService
    {
        private readonly ILogger<StartUpService> _logger;
        private readonly ISwarmSpaceBumblebeeHive _swarmSpaceBumblebeeHive;
        //private readonly IAzureIoTDeviceClientCache _azureIoTDeviceClientCache;

        public StartUpService(ILogger<StartUpService> logger, ISwarmSpaceBumblebeeHive swarmSpaceBumblebeeHive)
        //public StartUpService(ILogger<StartUpService> logger, ISwarmSpaceBumblebeeHive swarmSpaceBumblebeeHive, IAzureIoTDeviceClientCache azureIoTDeviceClientCache)
        {
            _logger = logger;
            _swarmSpaceBumblebeeHive = swarmSpaceBumblebeeHive;
            //_azureIoTDeviceClientCache = azureIoTDeviceClientCache;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            _logger.LogInformation("StartUpService.ExecuteAsync start");
           
            try
            {
                await _swarmSpaceBumblebeeHive.Login(cancellationToken);

                //await _azureIoTDeviceClientCache.Load(cancellationToken);
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