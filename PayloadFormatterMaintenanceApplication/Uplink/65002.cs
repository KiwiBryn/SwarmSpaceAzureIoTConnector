using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

public class FormatterUplink : PayloadFormatter.IFormatterUplink
{
    public JObject Evaluate(IDictionary<string, string> properties, uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        JObject telemetryEvent = new JObject();

        if (payloadJson != null)
        {
            JObject location = new JObject();

            if ((payloadJson.GetValue("lt").Value<double>() != 0.0) && (payloadJson.GetValue("ln").Value<double>() != 0.0))
            {
                location.Add("lat", payloadJson.GetValue("lt"));
                location.Add("lon", payloadJson.GetValue("ln"));
                location.Add("alt", payloadJson.GetValue("al"));

                telemetryEvent.Add("DeviceLocation", location);

                // Course & speed
                telemetryEvent.Add("Course", payloadJson.GetValue("hd"));
                telemetryEvent.Add("Speed", payloadJson.GetValue("sp"));
            }

            telemetryEvent.Add("BatteryVoltage", payloadJson.GetValue("bv"));

            telemetryEvent.Add("RSSI", payloadJson.GetValue("rs"));

            properties.Add("iothub-creation-time-utc", DateTimeOffset.FromUnixTimeSeconds((long)payloadJson.GetValue("dt")).ToString("s", CultureInfo.InvariantCulture));
        }

        return telemetryEvent;
    }
}