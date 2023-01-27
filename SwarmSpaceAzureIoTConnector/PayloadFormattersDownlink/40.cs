using System;

using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(int organisationId, int deviceId, int deviceType, int userApplicationId, JObject payloadJson)
    {
        byte? status = payloadJson.Value<byte?>("FanStatus");

        if ( status.HasValue ) 
        { 
            return new byte[] { status.Value };
        }

        return new byte[]{};
    }
}
