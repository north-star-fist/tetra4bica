using Newtonsoft.Json;
using System;
using UnityEngine;

/// <summary> Custom JSON converter for Unity <see cref="Vector2Int"/>. </summary>
public class Vector2IntJsonConverter : JsonConverter<Vector2Int> {
    public override Vector2Int ReadJson(
        JsonReader reader,
        Type objectType,
        Vector2Int existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    ) {
        var xy = serializer.Deserialize<int[]>(reader);
        return new Vector2Int(xy[0], xy[1]);
    }

    public override void WriteJson(JsonWriter writer, Vector2Int value, JsonSerializer serializer) {
        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.y);
        writer.WriteEndArray();
    }
}
