using Microsoft.Xna.Framework;

using Jelly;
using Jelly.Components;
using Jelly.Graphics;

using LDtk;
using LDtk.Renderer;
using SmashPassers.GameContent;

namespace SmashPassers;

public static class SceneLoader
{
    static LDtkFile lDtkFile;
    static LDtkWorld lDtkWorld;
    static ExampleRenderer lDtkRenderer;

    public static void Load(SceneID sceneID)
    {
        SceneManager.ChangeSceneImmediately(SceneRegistry.GetDefStatic(sceneID.ToString()).Build());
    }

    public static void LoadLDtk(SceneID sceneID)
    {
        SceneManager.ChangeSceneImmediately(SceneRegistry.LoadFromFile(sceneID.ToString()).Build());
    }
}

public enum SceneID
{
    Title
}
