#region

using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Assets.IO;

sealed class MaterialValueConverter : JsonConverter<IAssetMaterialValue>
{
    public override IAssetMaterialValue Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        if (doc.RootElement.ValueKind == JsonValueKind.Number)
        {
            if (doc.RootElement.TryGetInt32(out var intValue))
                return new AssetMaterialValue<int> { Value = intValue, Kind = UniformValueKind.Int };
            else if (doc.RootElement.TryGetSingle(out var floatValue))
                return new AssetMaterialValue<float> { Value = floatValue, Kind = UniformValueKind.Float };
        }

        if (!doc.RootElement.TryGetProperty("kind", out var kindProp)) throw new JsonException("Missing Kind");
        var kindStr = kindProp.GetString();
        if (!Enum.TryParse(kindStr, out UniformValueKind kind)) throw new JsonException($"Invalid Kind: {kindStr}");

        var json = doc.RootElement.GetRawText();
        return kind switch
        {
            UniformValueKind.Vec2 => JsonSerializer.Deserialize<AssetMaterialValue<Vector2>>(json, options)!,
            UniformValueKind.Vec3 => JsonSerializer.Deserialize<AssetMaterialValue<Vector3>>(json, options)!,
            UniformValueKind.Vec4 => JsonSerializer.Deserialize<AssetMaterialValue<Vector4>>(json, options)!,
            UniformValueKind.Mat4 => throw new NotSupportedException("Matrix kinds are intentionally not handled."),
            _ => throw new JsonException($"Unsupported kind: {kind}")
        };
    }

    public override void Write(Utf8JsonWriter writer, IAssetMaterialValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case AssetMaterialValue<float> mvf:
                writer.WriteNumberValue(mvf.Value);
                return;
            case AssetMaterialValue<int> mvi:
                writer.WriteNumberValue(mvi.Value);
                return;
            case AssetMaterialValue<Vector2> v2:
                JsonSerializer.Serialize(writer, v2, options);
                return;
            case AssetMaterialValue<Vector3> v3:
                JsonSerializer.Serialize(writer, v3, options);
                return;
            case AssetMaterialValue<Vector4> v4:
                JsonSerializer.Serialize(writer, v4, options);
                return;
            default:
                throw new NotSupportedException($"Unsupported IAssetMaterialValue type: {value.GetType().FullName}");
        }
    }
}

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