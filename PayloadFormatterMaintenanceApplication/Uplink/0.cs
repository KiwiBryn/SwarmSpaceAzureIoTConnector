using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FormatterUplink : PayloadFormatter.IFormatterUplink
{
    public JObject Evaluate(IDictionary<string, string> properties, uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        JObject telemetryEvent = new JObject();

        telemetryEvent.Add("ASCII", payloadText);
        telemetryEvent.Add("Bits", BitConverter.ToString(payloadBytes));

        return telemetryEvent;
    }
}