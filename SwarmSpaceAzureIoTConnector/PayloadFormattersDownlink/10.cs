using System;

using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        bool? light = payloadJson.Value<bool?>("Light");

        if (light.HasValue)
        {
            return new byte[] { Convert.ToByte(light.Value) };
        }

        return new byte[] { };
    }
}
