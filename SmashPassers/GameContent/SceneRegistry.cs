using System.IO;
using System.Text.Json;
using Jelly;
using Jelly.Components;
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

        var entityLayer = level.LayerInstances[0];
        var tileLayer = level.LayerInstances[1];

        Point gridSize = new(tileLayer._CWid, tileLayer._CHei);

        scene.Width = gridSize.X * CollisionSystem.TileSize;
        scene.Height = gridSize.Y * CollisionSystem.TileSize;

        scene.Collisions.Tiles = new int[gridSize.Y][];

        int c = 0;
        var array = tileLayer.IntGridCsv;
        for(int y = 0; y < gridSize.Y; y++)
        {
            scene.Collisions.Tiles[y] = new int[gridSize.X];
            for(int x = 0; x < gridSize.X; x++)
            {
                scene.Collisions.Tiles[y][x] = array[c];
                c++;
            }
        }

        for(int i = 0; i < entityLayer.EntityInstances.Length; i++)
        {
            var entity = entityLayer.EntityInstances[i];
            if(entity._Identifier == "Respawn")
                scene.RespawnPoint = entity.Px;

            if(entity._Identifier == "Ledge")
            {
                scene.Entities.Add(new() {
                    Position = entity.Px,
                    Components = [
                        new Solid {
                            DefaultBehavior = false,
                            Width = entity.Width / CollisionSystem.TileSize,
                            Height = entity.Height / CollisionSystem.TileSize,
                        }
                    ]
                });
            }

            if(entity._Identifier == "JumpThrough")
                scene.Collisions.JumpThroughs.Add(new(entity.Px, new(entity.Width, MathHelper.Max(entity.Height - 1, 1))));

            if(entity._Identifier.EndsWith("Slope"))
            {
                Point point1 = entity.Px;
                Point point2 = new Point(entity.FieldInstances[0]._Value[0].GetProperty("cx").GetInt32() * CollisionSystem.TileSize, entity.FieldInstances[0]._Value[0].GetProperty("cy").GetInt32() * CollisionSystem.TileSize);

                if(entity._Identifier == "JumpThrough_Slope")
                    scene.Collisions.JumpThroughSlopes.Add(new(point1, point2, 2));
                if(entity._Identifier == "Slope")
                    scene.Collisions.Slopes.Add(new(point1, point2, 2));
            }
        }

        return Register(scene);
    }

    public static SceneDef? LoadFromFile(string path)
    {
        return SceneDef.Deserialize(File.ReadAllText(Path.Combine("Content", "Levels", path + ".json")));
    }
}
