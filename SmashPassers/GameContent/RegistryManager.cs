using System.Text.Json;
using System.Text.Json.Serialization;

using Jelly;
using Jelly.GameContent;
using Jelly.Serialization;

namespace SmashPassers.GameContent;

public static class RegistryManager
{
    public static SceneRegistry SceneRegistry { get; } = new();

    private static readonly PolymorphicTypeResolver componentTypeResolver = new([typeof(Component), typeof(JsonEntity)]);

    public static JsonSerializerOptions SerializerOptions => new() {
        Converters = {
            new JsonStringEnumConverter(),
            new JsonPointConverter(),
            new JsonVector2Converter(),
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = componentTypeResolver,
    };

    public static void Initialize()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        if(assembly is not null)
        {
            componentTypeResolver.GetAllDerivedTypesFromAssembly(assembly);
        }

        Main.Logger.LogInfo($"Registered Components:\n  - {string.Join("\n  - ", componentTypeResolver.GetTypeSet(typeof(Component)).DerivedTypes)}");

        Registries.Add(new SceneRegistry());
    }
}
