using Jelly.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace SmashPassers.Graphics;

public static class BackgroundRenderer
{
    private static RenderTarget2D renderTarget;

    public static void Initialize()
    {
        renderTarget = new(Renderer.GraphicsDevice, Renderer.ScreenSize.X, Renderer.ScreenSize.Y);
    }
}
