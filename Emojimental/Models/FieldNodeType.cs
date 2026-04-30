namespace Emojimental.Models;

public enum FieldNodeType
{
    Energy,
    Snow
}

public static class FieldNodeTypeExtensions
{
    public static string DisplayName(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "Energy Box",
            FieldNodeType.Snow => "Snow Box",
            _ => "Box"
        };

    public static string Icon(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "☀️",
            FieldNodeType.Snow => "🪏",
            _ => "⬜"
        };

    public static string ProducedResourceName(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "Energy",
            FieldNodeType.Snow => "Ice",
            _ => "Resource"
        };

    public static string ProducedResourceIcon(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "⚡",
            FieldNodeType.Snow => "🧊",
            _ => "✨"
        };

    public static string CssModifier(this FieldNodeType type)
        => type switch
        {
            FieldNodeType.Energy => "energy",
            FieldNodeType.Snow => "snow",
            _ => "default"
        };
}
