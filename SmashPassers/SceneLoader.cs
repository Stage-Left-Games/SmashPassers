using Microsoft.Xna.Framework;

using Jelly;
using Jelly.Components;
using Jelly.Graphics;

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
                    Width = Renderer.ScreenSize.X,
                    Height = Renderer.ScreenSize.Y,
                    Entities = {
                        // new Entity {
                        //     Position = new(220, 400),
                        //     Depth = 50,
                        //     Components = {
                        //         new SpriteComponent {
                        //             TexturePath = "Images/Characters/neco/neco_0-0",
                        //             Pivot = new(240, 332),
                        //         },
                        //         new Components.PlayerBase {
                        //             HitboxOffset = new(-32/2, -68),
                        //             Width = 32,
                        //             Height = 68
                        //         }
                        //     },
                        // },
                        new Entity {
                            Position = new(220, 400),
                            Depth = 50,
                            Components = {
                                new SpriteComponent {
                                    TexturePath = "Images/Characters/miki/miki_0-0",
                                    Pivot = new(240, 332),
                                },
                                new Components.PlayerBase {
                                    HitboxOffset = new(-128/2, -304),
                                    Width = 128,
                                    Height = 304
                                }
                            },
                        },
                    },
                };

                for(int i = 0; i < scene.CollisionSystem.Width; i++)
                    scene.CollisionSystem.SetTile(1, new(i, scene.CollisionSystem.Height - 1));
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
