using System.IO;

using Microsoft.Xna.Framework;

using Jelly;

using SmashPassers.GameContent;

namespace SmashPassers;

public static class SceneLoader
{
    public static void Load(SceneID sceneID)
    {
        Load(SceneRegistry.GetDefStatic(sceneID.ToString()));
    }

    public static void LoadFile(string name)
    {
        if(!Path.HasExtension(name))
            name += ".json";

        Load(SceneRegistry.LoadFromFile(Path.Combine("Content", "Levels", name)));
    }

    private static void Load(SceneDef scene)
    {
        scene.Entities.Add(new() {
            Position = scene.RespawnPoint ?? Point.Zero,
            Components = [
                new Components.NecoPlayer {
                    HitboxOffset = new(-32/2, -70),
                    Width = 32,
                    Height = 70,
                },
            ],
        });

        SceneManager.ChangeSceneImmediately(scene.Build());
    }
}

public enum SceneID
{
    Title,
    Track_Office,
}
