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
namespace PayloadFormatter // Additional namespace for shortening interface when usage in formatter code
{
    using Microsoft.Azure.Devices.Client;

    using Newtonsoft.Json.Linq;

    public interface IFormatterUplink
    {
        public Message Evaluate(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject telemetryEvent, JObject payloadJson, string payloadText, byte[] payloadBytes);
    }

    public interface IFormatterDownlink
    {
        public byte[] Evaluate(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson);
    }
}

namespace devMobile.IoT.SwarmSpaceAzureIoTConnector.Connector
{
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    using CSScriptLib;

    using LazyCache;

    using PayloadFormatter;

    public interface IPayloadFormatterCache
    {
        public Task<IFormatterUplink> UplinkGetAsync(int userApplicationId);

        public Task<IFormatterDownlink> DownlinkGetAsync(int userApplicationId);
    }

    public class PayloadFormatterCache : IPayloadFormatterCache
    {
        private readonly ILogger<PayloadFormatterCache> _logger;
        private readonly Models.ApplicationSettings _applicationSettings;
        private readonly static IAppCache _payloadFormatters = new CachingService();

        public PayloadFormatterCache(ILogger<PayloadFormatterCache>logger, IOptions<Models.ApplicationSettings> applicationSettings)
        {
            _logger = logger;
            _applicationSettings = applicationSettings.Value;
        }

        public async Task<IFormatterUplink> UplinkGetAsync(int userApplicationId)
        {
            IFormatterUplink payloadFormatterUplink = await _payloadFormatters.GetOrAddAsync<PayloadFormatter.IFormatterUplink>($"U{userApplicationId}", (ICacheEntry x) => UplinkLoadAsync(userApplicationId), memoryCacheEntryOptions);

            return payloadFormatterUplink;
        }

        private async Task<IFormatterUplink> UplinkLoadAsync(int userApplicationId)
        {
            string payloadformatterFilePath = $"{_applicationSettings.PayloadFormattersUplinkFilePath}\\{userApplicationId}.cs";

            if (!File.Exists(payloadformatterFilePath))
            {
                _logger.LogInformation("PayloadFormatterUplink- UserApplicationId:{0} PayloadFormatterPath:{1} not found using default:{2}", userApplicationId, payloadformatterFilePath, _applicationSettings.PayloadFormatterUplinkDefault);

                return CSScript.Evaluator.LoadFile<PayloadFormatter.IFormatterUplink>(_applicationSettings.PayloadFormatterUplinkDefault);
            }

            _logger.LogInformation("PayloadFormatterUplink- UserApplicationId:{0} loading PayloadFormatterPath:{1}", userApplicationId, payloadformatterFilePath);

            return CSScript.Evaluator.LoadFile<PayloadFormatter.IFormatterUplink>(payloadformatterFilePath);
        }

        public async Task<IFormatterDownlink> DownlinkGetAsync(int userApplicationId)
        {
            IFormatterDownlink payloadFormatterUplink = await _payloadFormatters.GetOrAddAsync<PayloadFormatter.IFormatterDownlink>($"D{userApplicationId}", (ICacheEntry x) => DownlinkLoadAsync(userApplicationId), memoryCacheEntryOptions);

            return payloadFormatterUplink;
        }

        private async Task<IFormatterDownlink> DownlinkLoadAsync(int userApplicationId)
        {
            string payloadformatterFilePath = $"{_applicationSettings.PayloadFormattersDownlinkFilePath}\\{userApplicationId}.cs";

            if (!File.Exists(payloadformatterFilePath))
            {
                _logger.LogInformation("PayloadFormatterDownlink- UserApplicationId:{0} PayloadFormatterPath:{1} not found using default:{2}", userApplicationId, payloadformatterFilePath, _applicationSettings.PayloadFormatterDownlinkDefault);

                return CSScript.Evaluator.LoadFile<PayloadFormatter.IFormatterDownlink>(_applicationSettings.PayloadFormatterDownlinkDefault);
            }

            _logger.LogInformation("PayloadFormatterDownlink- UserApplicationId:{0} loading PayloadFormatterPath:{1}", userApplicationId, payloadformatterFilePath);

            return CSScript.Evaluator.LoadFile<PayloadFormatter.IFormatterDownlink>(payloadformatterFilePath);
        }

        private static readonly MemoryCacheEntryOptions memoryCacheEntryOptions = new MemoryCacheEntryOptions()
        {
            Priority = CacheItemPriority.NeverRemove
        };
    }
}
