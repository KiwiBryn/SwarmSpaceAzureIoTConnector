using System;

using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(int organisationId, int deviceId, int deviceType, int userApplicationId, JObject payloadJson)
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
