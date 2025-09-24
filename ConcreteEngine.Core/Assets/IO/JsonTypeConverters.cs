#region

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace ConcreteEngine.Core.Assets.IO;

sealed class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType != JsonTokenType.StartArray) throw new JsonException();
        r.Read();
        float x = r.GetSingle();
        r.Read();
        float y = r.GetSingle();
        r.Read();
        if (r.TokenType != JsonTokenType.EndArray) throw new JsonException();
        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter w, Vector2 v, JsonSerializerOptions o)
    {
        w.WriteStartArray();
        w.WriteNumberValue(v.X);
        w.WriteNumberValue(v.Y);
        w.WriteEndArray();
    }
}

sealed class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType != JsonTokenType.StartArray) throw new JsonException();
        r.Read();
        float x = r.GetSingle();
        r.Read();
        float y = r.GetSingle();
        r.Read();
        float z = r.GetSingle();
        r.Read();
        if (r.TokenType != JsonTokenType.EndArray) throw new JsonException();
        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter w, Vector3 v, JsonSerializerOptions o)
    {
        w.WriteStartArray();
        w.WriteNumberValue(v.X);
        w.WriteNumberValue(v.Y);
        w.WriteNumberValue(v.Z);
        w.WriteEndArray();
    }
}

sealed class Vector4Converter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        if (r.TokenType != JsonTokenType.StartArray) throw new JsonException();
        r.Read();
        float x = r.GetSingle();
        r.Read();
        float y = r.GetSingle();
        r.Read();
        float z = r.GetSingle();
        r.Read();
        float wv = r.GetSingle();
        r.Read();
        if (r.TokenType != JsonTokenType.EndArray) throw new JsonException();
        return new Vector4(x, y, z, wv);
    }

    public override void Write(Utf8JsonWriter w, Vector4 v, JsonSerializerOptions o)
    {
        w.WriteStartArray();
        w.WriteNumberValue(v.X);
        w.WriteNumberValue(v.Y);
        w.WriteNumberValue(v.Z);
        w.WriteNumberValue(v.W);
        w.WriteEndArray();
    }
}