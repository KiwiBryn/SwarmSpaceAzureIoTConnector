using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(Dictionary<string, string> properties, uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        double? min = payloadJson.Value<double?>("TemperatureMin");

        if (min.HasValue)
        {
            byte[] result = new byte[9];

            result[0] = 1;

            BitConverter.GetBytes(min.Value).CopyTo(result, 1);

            return result;
        }

        double? max = payloadJson.Value<double?>("TemperatureMax");

        if (max.HasValue)
        {
            byte[] result = new byte[9];

            result[0] = 2;

            BitConverter.GetBytes(max.Value).CopyTo(result, 1);

            return result;
        }

        return new byte[] { };
    }
}
