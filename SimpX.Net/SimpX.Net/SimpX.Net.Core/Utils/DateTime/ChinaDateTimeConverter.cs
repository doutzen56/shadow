using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SimpX.Net.Core;

/// <summary>
/// JSON时间序列化yyyy-MM-dd HH:mm:ss
/// </summary>
public class ChinaDateTimeConverter : DateTimeConverterBase
{
    private static readonly IsoDateTimeConverter DtConverter = new() { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" };

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return DtConverter.ReadJson(reader, objectType, existingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        DtConverter.WriteJson(writer, value, serializer);
    }
}

/// <summary>
/// JSON时间序列化yyyy-MM-dd HH:mm
/// </summary>
public class ChinaDateTimeConverterHH : DateTimeConverterBase
{
    private static readonly IsoDateTimeConverter dtConverter = new() { DateTimeFormat = "yyyy-MM-dd HH:mm" };

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return dtConverter.ReadJson(reader, objectType, existingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        dtConverter.WriteJson(writer, value, serializer);
    }
}

/// <summary>
/// JSON时间序列化yyyy-MM-dd
/// </summary>
public class ChinaDateTimeConverterDate : DateTimeConverterBase
{
    private static readonly IsoDateTimeConverter dtConverter = new() { DateTimeFormat = "yyyy-MM-dd" };

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return dtConverter.ReadJson(reader, objectType, existingValue, serializer);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        dtConverter.WriteJson(writer, value, serializer);
    }
}