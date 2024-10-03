using Microsoft.Xna.Framework;

using Jelly;
using Jelly.Components;

namespace SmashPassers;

public static class SceneLoader
{
    public static void Load(SceneID sceneID)
    {
        Scene scene;
        switch(sceneID)
        {
            case SceneID.Title:
            {
                scene = new() {
                    Name = "Title",
                    Entities = {
                        new Entity {
                            Position = new(200, 300),
                            Depth = 50,
                            Components = {
                                new SpriteComponent {
                                    TexturePath = "Images/arc_0-0",
                                    Pivot = new(480, 644)
                                }
                            }
                        },
                        new Entity {
                            Position = new(400, 300),
                            Depth = 50,
                            Components = {
                                new SpriteComponent {
                                    TexturePath = "Images/neco_0-0",
                                    Pivot = new(480, 644)
                                }
                            }
                        }
                    }
                };
                break;
            }
            default:
                throw new System.Exception("Scene does not exist");
        }

        SceneManager.ChangeSceneImmediately(scene);
    }
}

public enum SceneID
{
    Title
}
