using System.IO;

using Jelly;
using Jelly.GameContent;
using Jelly.Graphics;
using Microsoft.Xna.Framework;

namespace SmashPassers.GameContent;

public class SceneRegistry : Registry<SceneDef>
{
    public override void Init()
    {
        string path = Path.Combine(Program.ProgramPath, "Content", "Levels");
        Directory.CreateDirectory(path);

        foreach(var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
        {
            string fileName = file[(path.Length + 1)..^5];
            Add(fileName);
        }

        foreach(var file in Directory.EnumerateFiles(Path.Combine(path, "Tracks"), "*.ldtkl", SearchOption.AllDirectories))
        {
            AddLDtk(file, "Track_" + Path.GetFileNameWithoutExtension(file));
        }

        Main.Logger.LogInfo($"Registered Scenes:\n  - {string.Join("\n  - ", this.Keys)}");
    }

    private bool Add(string name)
    {
        var def = LoadFromFile(name);
        if(def is not null)
        {
            return Register(def);
        }
        return false;
    }

    private bool AddLDtk(string path, string name)
    {
        SceneDef scene = new() {
            Name = name,
        };

        var level = LDtk.LDtkLevel.FromFile(path);
        Point gridSize = new(level.LayerInstances[0]._CWid, level.LayerInstances[0]._CHei);

        scene.Width = gridSize.X * CollisionSystem.TileSize;
        scene.Height = gridSize.Y * CollisionSystem.TileSize;

        scene.Collisions.Tiles = new int[gridSize.Y][];

        int c = 0;
        var array = level.LayerInstances[0].IntGridCsv;
        for(int y = 0; y < gridSize.Y; y++)
        {
            scene.Collisions.Tiles[y] = new int[gridSize.X];
            for(int x = 0; x < gridSize.X; x++)
            {
                scene.Collisions.Tiles[y][x] = array[c];
                c++;
            }
        }

        // TODO: import entities and slopes

        foreach(var entity in level.LayerInstances[1].EntityInstances)
        {
            if(entity._Identifier == "Respawn")
            {
                scene.RespawnPoint = entity.Px;
            }
        }

        return Register(scene);
    }

    public static SceneDef? LoadFromFile(string path)
    {
        return SceneDef.Deserialize(File.ReadAllText(Path.Combine("Content", "Levels", path + ".json")));
    }
}
