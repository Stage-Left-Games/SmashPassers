using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jelly;
using Jelly.Graphics;
using System.Collections.Generic;

namespace SmashPassers;

public class Main : Game
{
    private GraphicsDeviceManager _graphics;

    private static Camera camera;
    private static Scene Scene => SceneManager.ActiveScene;

    public static Camera Camera => camera;
    public static Logger Logger { get; } = new("Main");
    public static List<Entity> Players { get; } = [];

    public Main()
    {
        _graphics = Renderer.GetDefaultGraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        IsFixedTimeStep = true;
    }

    protected override void Initialize()
    {
        Renderer.ScreenSize = new Point(1920, 1080).Clamp(
            Point.Zero,
            new(GraphicsDevice.Adapter.CurrentDisplayMode.Width, GraphicsDevice.Adapter.CurrentDisplayMode.Height)
        );
        Renderer.PixelScale = GraphicsDevice.Adapter.CurrentDisplayMode.Width / Renderer.ScreenSize.X;

        _graphics.PreferredBackBufferWidth = Renderer.ScreenSize.X * Renderer.PixelScale;
        _graphics.PreferredBackBufferHeight = Renderer.ScreenSize.Y * Renderer.PixelScale;

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

    protected override void BeginRun()
    {
        SceneLoader.Load(SceneID.Title);

        base.BeginRun();
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

        if(Players.Count > 0)
        {
            Vector2 pos = Vector2.Zero;
            foreach(var player in Players)
            {
                var component = player.GetComponent<SmashPassers.Components.PlayerBase>();
                pos += component.Center.ToVector2() + new Vector2(
                    component.velocity.X * 24,
                    component.velocity.Y * component.velocity.Y / 4 * Math.Sign(component.velocity.Y)
                );
            }
            pos /= Players.Count;

            Camera.Position += (pos + new Vector2(-Renderer.ScreenSize.X / 2f, -Renderer.ScreenSize.Y / 2f) - Camera.Position) / 8f;
        }
        else
            Camera.Position += (Vector2.Zero + new Vector2(-Renderer.ScreenSize.X / 2f, -Renderer.ScreenSize.Y / 2f) - Camera.Position) / 8f;

        camera.Update();

        JellyBackend.PostUpdate();

        base.Update(gameTime);
    }

    protected override bool BeginDraw()
    {
        Scene?.PreDraw();

        return base.BeginDraw();
    }

    protected override void Draw(GameTime gameTime)
    {
        // Set culling region
        var rect = GraphicsDevice.ScissorRectangle;
        // GraphicsDevice.ScissorRectangle = new(0, 0, Scene?.Width ?? Renderer.ScreenSize.X, Scene?.Height ?? Renderer.ScreenSize.Y);

        Renderer.BeginDraw(SamplerState.PointClamp, camera.Transform);

        if(Scene is not null)
        {
            foreach(var tile in Scene.CollisionSystem.Collisions)
            {
                Renderer.SpriteBatch.Draw(Renderer.PixelTexture, tile, Color.Black);
            }

            Scene.Draw();
            Scene.PostDraw();
        }

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
