using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FormatterUplink : PayloadFormatter.IFormatterUplink
{
    public void Evaluate(Dictionary<string, string> properties, uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject telemetryEvent, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        telemetryEvent.Add("ASCII", payloadText);
        telemetryEvent.Add("Bits", BitConverter.ToString(payloadBytes));
    }
}