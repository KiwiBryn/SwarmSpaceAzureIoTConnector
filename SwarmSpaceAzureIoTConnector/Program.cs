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
namespace devmobile.IoT.SwarmSpaceAzureIoTConnector.Connector
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
               .ConfigureFunctionsWorkerDefaults()
               .ConfigureAppConfiguration(c =>
               {
#if DEBUG
                   c.AddUserSecrets("00f193f0-7cd4-43c8-a885-336029a808b9");
#endif
                   c.AddEnvironmentVariables();
               })
                .ConfigureLogging((context, l) =>
                {
                    l.AddConsole();
                    l.AddApplicationInsightsWebJobs(o => o.ConnectionString = (context.Configuration.GetConnectionString("ApplicationInsights")));
                })
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
