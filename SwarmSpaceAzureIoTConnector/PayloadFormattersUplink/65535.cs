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
        if ((payloadText != "") && (payloadJson != null))
        {
            JObject location = new JObject();

            location.Add("lat", payloadJson.GetValue("lt"));
            location.Add("lon", payloadJson.GetValue("ln"));
            location.Add("alt", payloadJson.GetValue("a"));

            telemetryEvent.Add("DeviceLocation", location);
        }

        // Course & speed
        telemetryEvent.Add("Course", payloadJson.GetValue("c"));
        telemetryEvent.Add("Speed", payloadJson.GetValue("s"));

        // Battery voltage & current
        telemetryEvent.Add("BatteryVoltage", payloadJson.GetValue("bv"));
        telemetryEvent.Add("BatteryCurrent", payloadJson.GetValue("bi"));

        // Solar voltage
        telemetryEvent.Add("SolarVoltage", payloadJson.GetValue("sv"));

        // Modem current 
        telemetryEvent.Add("ModemCurrent", payloadJson.GetValue("ti"));

        // RSSI
        telemetryEvent.Add("RSSI", payloadJson.GetValue("r"));

        Message ioTHubmessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(telemetryEvent)));

        ioTHubmessage.Properties.Add("iothub-creation-time-utc", DateTimeOffset.FromUnixTimeSeconds((long)payloadJson.GetValue("d")).ToString("s", CultureInfo.InvariantCulture));

        return ioTHubmessage;
    }
}