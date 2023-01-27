﻿using System;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FormatterDownlink : PayloadFormatter.IFormatterDownlink
{
    public byte[] Evaluate(uint organisationId, uint deviceId, byte deviceType, ushort userApplicationId, JObject payloadJson)
    {
        return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(payloadJson));
    }
}
