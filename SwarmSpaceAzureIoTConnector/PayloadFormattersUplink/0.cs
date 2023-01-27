using System;
using System.Globalization;
using System.Text;

using Microsoft.Azure.Devices.Client;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FormatterUplink : PayloadFormatter.IFormatterUplink
{
    public Message Evaluate(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject telemetryEvent, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        Message ioTHubmessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryEvent)));

        return ioTHubmessage;
    }
}