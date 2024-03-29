﻿//---------------------------------------------------------------------------------
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class PayloadStringExtensions
	{
		public static bool IsPayloadEmpty(this string payloadText)
		{
			return payloadText == "@";
		}

		public static bool IsPayloadValidJson(this string payloadText)
		{
			// In this scenario a valid JSON string should start/end with {/} for an object or [/] for an array
			if ((payloadText.StartsWith("{") && payloadText.EndsWith("}")) || ((payloadText.StartsWith("[") && payloadText.EndsWith("]"))))
			{
				try
				{
					var obj = JToken.Parse(payloadText);
				}
				catch (JsonReaderException)
				{
					return false;
				}

				return true;
			}

			return false;
		}
	}
}
