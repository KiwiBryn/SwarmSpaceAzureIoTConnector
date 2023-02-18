﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(Dictionary<string, string> properties, uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson, string payloadText, byte[] payloadBytes)
    {
        byte? status = payloadJson.Value<byte?>("FanStatus");

        if ( status.HasValue ) 
        { 
            return new byte[] { status.Value };
        }

        return new byte[]{};
    }
}
