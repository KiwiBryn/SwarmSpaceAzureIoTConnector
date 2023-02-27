﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

public class FormatterUplink : PayloadFormatter.IFormatterUplink
{
    public JObject Evaluate(IDictionary<string, string> properties, uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        JObject telemetryEvent = new JObject();

        if ((payloadText != "") && (payloadJson != null))
        {
            JObject location = new JObject();

            location.Add("lat", payloadJson.GetValue("lt"));
            location.Add("lon", payloadJson.GetValue("ln"));
            location.Add("alt", payloadJson.GetValue("al"));

            telemetryEvent.Add("DeviceLocation", location);
        }

        // Course & speed
        telemetryEvent.Add("Course", payloadJson.GetValue("hd"));
        telemetryEvent.Add("Speed", payloadJson.GetValue("sp"));

        // Battery voltage & current

        telemetryEvent.Add("BatteryVoltage", payloadJson.GetValue("bv"));

        /*
        telemetryEvent.Add("BatteryCurrent", payloadJson.GetValue("bi"));

        // Solar voltage
        telemetryEvent.Add("SolarVoltage", payloadJson.GetValue("sv"));

        // Modem current 
        telemetryEvent.Add("ModemCurrent", payloadJson.GetValue("ti"));
        */

        // RSSI
        telemetryEvent.Add("RSSI", payloadJson.GetValue("rs"));

        properties.Add("iothub-creation-time-utc", DateTimeOffset.FromUnixTimeSeconds((long)payloadJson.GetValue("dt")).ToString("s", CultureInfo.InvariantCulture));

        return telemetryEvent;
    }
}