using MTCG_Peirl.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CardConverter : JsonConverter<Card>
{
    public override Card Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            JsonElement root = doc.RootElement;
            string name = root.GetProperty("Name").GetString();
            ElementType elementType = ElementType.normal; // Default if no specific element is found

            if (name.StartsWith("Fire", StringComparison.OrdinalIgnoreCase))
            {
                elementType = ElementType.fire;
            }
            else if (name.StartsWith("Water", StringComparison.OrdinalIgnoreCase))
            {
                elementType = ElementType.water;
            }
            else if (name.StartsWith("Regular", StringComparison.OrdinalIgnoreCase))
            {
                elementType = ElementType.normal;
            }

            if (name.EndsWith("Spell", StringComparison.OrdinalIgnoreCase))
            {
                var spellCard = JsonSerializer.Deserialize<Spellcard>(root.GetRawText(), options);
                spellCard.CardType = "Spell";
                spellCard.ElementType = elementType;
                return spellCard;
            }
            else
            {
                var monsterCard = JsonSerializer.Deserialize<Monstercard>(root.GetRawText(), options);
                monsterCard.CardType = "Monster";
                monsterCard.ElementType = elementType;
                return monsterCard;
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, Card value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}
