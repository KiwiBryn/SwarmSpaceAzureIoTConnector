using System;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(int organisationId, int deviceId, int deviceType, int userApplicationId, JObject payloadJson)
    {
        return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(payloadJson));
    }
}
