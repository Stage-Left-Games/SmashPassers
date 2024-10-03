using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jelly;
using Jelly.Graphics;

namespace SmashPassers;

public class Main : Game
{
    private GraphicsDeviceManager _graphics;

    private static Camera camera;

    public static Camera Camera => camera;

    public static Logger Logger { get; } = new("Main");

    private static Scene Scene => SceneManager.ActiveScene;

    public Main()
    {
        _graphics = Renderer.GetDefaultGraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Renderer.Initialize(_graphics, GraphicsDevice, Window);
        camera = new Camera();

        ContentLoader.Init(Content);

        // Registry
        JellyBackend.Initialize(new ContentLoader());

        LocalizationManager.CurrentLanguage = "en-us";

        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        Renderer.LoadContent(Content);

        // TODO: use this.Content to load your game content here
    }

    protected override void Update(GameTime gameTime)
    {
        JellyBackend.PreUpdate(gameTime);

        Input.InputDisabled = !IsActive;

        Input.RefreshKeyboardState();
        Input.RefreshMouseState();
        Input.RefreshGamePadState();

        Input.UpdateTypingInput(gameTime);

        if(Input.GetPressed(Buttons.Back) || Input.GetPressed(Keys.Escape))
        {
            Exit();
            return;
        }

        if(Input.GetPressed(Keys.F1))
        {
            JellyBackend.DebugEnabled = !JellyBackend.DebugEnabled;
        }

        Scene?.PreUpdate();
        Scene?.Update();
        Scene?.PostUpdate();

        camera.Update();

        JellyBackend.PostUpdate();

        base.Update(gameTime);
    }

    private void PreDraw(GameTime gameTime)
    {
        Scene?.PreDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        PreDraw(gameTime);

        // Set culling region
        var rect = GraphicsDevice.ScissorRectangle;
        GraphicsDevice.ScissorRectangle = new(0, 0, Scene?.Width ?? Renderer.ScreenSize.X, Scene?.Height ?? Renderer.ScreenSize.Y);

        Renderer.BeginDraw(SamplerState.PointClamp);

        Scene?.Draw();
        Scene?.PostDraw();

        Renderer.EndDraw();

        // Revert culling region to whatever it was before
        GraphicsDevice.ScissorRectangle = rect;

        Renderer.BeginDrawUI();

        Scene?.DrawUI();

        Renderer.EndDrawUI();
        Renderer.FinalizeDraw();

        base.Draw(gameTime);
    }

    protected override void OnActivated(object sender, EventArgs args)
    {
        base.OnActivated(sender, args);

        Scene?.GainFocus();
    }

    protected override void OnDeactivated(object sender, EventArgs args)
    {
        base.OnDeactivated(sender, args);

        Scene?.LoseFocus();
    }
}
