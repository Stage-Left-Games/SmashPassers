using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace SmashPassers.Graphics;

public static class GraphicsUtilities
{
    private static SpriteFont _regularFont;
    private static SpriteFont _regularFontBold;

    public static class Fonts
    {
        public static SpriteFont RegularFont => _regularFont;
        public static SpriteFont RegularFontBold => _regularFontBold;
    }

    internal static void LoadContent(ContentManager content)
    {
        _regularFont = content.Load<SpriteFont>("Fonts/default");
        _regularFontBold = content.Load<SpriteFont>("Fonts/defaultBold");
    }
}

