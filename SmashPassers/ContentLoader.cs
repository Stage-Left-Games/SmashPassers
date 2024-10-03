using System;

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Jelly;
using System.Collections.Generic;

namespace SmashPassers;

public class ContentLoader : ContentProvider
{
    private static bool _initialized;

    private static ContentManager content;

    private static readonly List<string> missingAssets = [];

    private static void CheckInitialized()
    {
        if(!_initialized) throw new InvalidOperationException("ContentLoader not yet initialized");
    }

    public static void Init(ContentManager contentManager)
    {
        if(_initialized) throw new InvalidOperationException("ContentLoader already initialized");
        _initialized = true;

        content = contentManager;
    }

    public static T LoadContent<T>(string assetName)
    {
        CheckInitialized();

        if(missingAssets.Contains(assetName)) return default;

        try
        {
            return content.Load<T>(assetName);
        }
        catch(Exception e)
        {
            Main.Logger.LogError(e.GetType().FullName + $": The content file \"{assetName}\" was not found.");
            missingAssets.Add(assetName);
            return default;
        }
    }

    public override Texture2D GetTexture(string pathName)
    {
        return LoadContent<Texture2D>(pathName);
    }
}
