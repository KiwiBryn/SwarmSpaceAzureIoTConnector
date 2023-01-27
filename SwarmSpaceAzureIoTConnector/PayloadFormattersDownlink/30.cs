using System;

using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson)
    {
        // Case sensitive ?
        double? min = payloadJson.Value<double?>("min");

        double? max = payloadJson.Value<double?>("max");

        if (min.HasValue && max.HasValue)
        {
            byte[] result = new byte[16];

            BitConverter.GetBytes(min.Value).CopyTo(result, 0);

            BitConverter.GetBytes(max.Value).CopyTo(result, 8);

            return result;
        }

        return new byte[] { };
    }
}
