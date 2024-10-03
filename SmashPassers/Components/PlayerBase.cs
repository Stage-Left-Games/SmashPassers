using System;
using System.Collections.Generic;
using Jelly;
using Jelly.Components;
using Jelly.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SmashPassers.Components;

public class PlayerBase : Actor
{
    public const float Gravity = 20;
    public const float TerminalVelocity = 30;

    public Point HitboxOffset { get => bboxOffset; set => bboxOffset = value; }

    private readonly float baseMoveSpeed = 5;
    private readonly float baseJumpSpeed = -12;
    private readonly float baseGroundAcceleration = 5;
    private readonly float baseGroundFriction = 12;
    private readonly float baseAirAcceleration = 2;
    private readonly float baseAirFriction = 1;

    private float moveSpeed;
    private float jumpSpeed;
    private float accel;
    private float fric;

    private bool useGravity = true;
    private bool jumpCancelled;
    private bool running;
    private int inputDir;
    private bool wasOnGround;
    private bool onJumpthrough;

    private int fxTrailCounter;
    private readonly List<AfterImage> afterImages = [];
    protected bool FxTrail { get; set; }

    protected List<string> Textures { get; } = [];
    protected List<int> FrameCounts { get; } = [];
    protected float Frame { get; set; }
    protected int TextureIndex { get; set; }

    private SpriteComponent Sprite => Entity.GetComponent<SpriteComponent>();

    public PlayerInputMapping InputMapping { get; } = new() {
        
    };

    public bool UseGamePad { get; set; }
    public PlayerIndex GamePadIndex { get; set; }

    class AfterImage : Component
    {
        public float Alpha = 1;
        public string TexturePath;
        public int Frame;
        public Point Position;
        public SpriteEffects SpriteEffects;
        public Vector2 Pivot = Vector2.Zero;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float Rotation;

        public override void Draw()
        {
            Renderer.SpriteBatch.Draw(
                ContentLoader.LoadContent<Texture2D>(TexturePath),
                Position.ToVector2(),
                null,
                Color * Alpha,
                Rotation,
                Pivot,
                Scale,
                SpriteEffects,
                0
            );
        }
    }

    public override void OnCreated()
    {
        AddTexture(Sprite.TexturePath);

        base.OnCreated();
    }

    public override void EntityAwake()
    {
        Main.Players.Add(Entity);
        base.EntityAwake();
    }

    public override void SceneEnd(Scene scene)
    {
        Main.Players.Remove(Entity);

        base.SceneEnd(scene);
    }

    void AddTexture(string texture, int frameCount = 1)
    {
        Textures.Add(texture);
        FrameCounts.Add(frameCount);
    }

    public override void Update()
    {
        inputDir = InputMapping.Right.IsDown.ToInt32() - InputMapping.Left.IsDown.ToInt32();

        wasOnGround = OnGround;
        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(!wasOnGround && OnGround)
        {
            jumpCancelled = false;
        }

        velocity.Y = Util.Approach(velocity.Y, TerminalVelocity, Gravity * Time.DeltaTime);

        RecalculateStats();

        useGravity = true;

        if(inputDir != 0)
        {
            Facing = inputDir;

            if(InputMapping.PrimaryFire.Pressed)
            {
                velocity.X = 15 * inputDir;
                velocity.Y = MathHelper.Min(-2, velocity.Y);
            }

            if(OnGround)
            {
                running = true;
            }

            if(inputDir * velocity.X < 0)
            {
                velocity.X = Util.Approach(velocity.X, 0, fric * Time.DeltaTime);
            }
            if(inputDir * velocity.X < moveSpeed)
            {
                velocity.X = Util.Approach(velocity.X, inputDir * moveSpeed, accel * Time.DeltaTime);
            }

            if(inputDir * velocity.X > moveSpeed && OnGround)
            {
                velocity.X = Util.Approach(velocity.X, inputDir * moveSpeed, fric * 2 * Time.DeltaTime);
            }
        }
        else
        {
            running = false;
            velocity.X = Util.Approach(velocity.X, 0, fric * 2 * Time.DeltaTime);
        }

        if(!OnGround)
        {
            if(InputMapping.Down.Released && velocity.Y < 0 && !jumpCancelled)
            {
                jumpCancelled = true;
                velocity.Y /= 2;
            }
        }
        else
        {
            if(onJumpthrough && InputMapping.Down.IsDown && !CheckColliding(BottomEdge.Shift(new(0, 2)), true))
            {
                Entity.Y += 2;

                onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
                if(onJumpthrough) OnGround = true;
                else OnGround = CheckColliding(BottomEdge.Shift(0, 1));
            }
        }

        FxTrail = Math.Abs(velocity.X) > 1f * moveSpeed;

        // ...

        if(OnGround && InputMapping.Jump.Pressed)
        {
            velocity.Y = jumpSpeed;
        }

        MoveX(velocity.X, () => {
            velocity.X = 0;
        });
        MoveY(velocity.Y, () => {
            if(!(InputMapping.Down.IsDown && CheckCollidingJumpthrough(BottomEdge.Shift(new(0, 1)))))
                velocity.Y = 0;
        });

        // if(Left < 0)
        // {
        //     Entity.X = 0;
        //     velocity.X = 0;
        // }

        // if(Right > Scene.Width)
        // {
        //     Entity.X = Scene.Width - Width;
        //     velocity.X = 0;
        // }

        // if(Top < 0)
        // {
        //     Entity.Y = 0;
        //     velocity.Y = 0;
        // }

        // if(Bottom > Scene.Height)
        // {
        //     Entity.Y = Scene.Height - Height;
        //     velocity.Y = 0;
        // }

        if(FxTrail)
        {
            fxTrailCounter++;
            if(fxTrailCounter >= 3)
            {
                fxTrailCounter = 0;
                var afterImage = new AfterImage {
                    TexturePath = Textures[TextureIndex],
                    Position = Entity.Position,
                    SpriteEffects = SpriteEffects,
                    Scale = Sprite.Scale,
                    Color = Sprite.Color,
                    Rotation = Sprite.Rotation,
                    Pivot = Sprite.Pivot,
                    Alpha = 0.75f
                };
                afterImages.Add(afterImage);
                Entity.AddComponent(afterImage);
                Entity.AddComponent(Sprite);
            }
        }
        else
        {
            fxTrailCounter = 0;
        }

        for(int i = 0; i < afterImages.Count; i++)
        {
            AfterImage image = afterImages[i];

            image.Alpha = MathHelper.Max(image.Alpha - (1/20f), 0);
            if(image.Alpha == 0)
            {
                afterImages.RemoveAt(i);
                Entity.RemoveComponent(image);
                i--;
            }
        }

        Sprite.SpriteEffects = SpriteEffects;
    }

    private void RecalculateStats()
    {
        moveSpeed = baseMoveSpeed;
        jumpSpeed = baseJumpSpeed;

        accel = baseGroundAcceleration;
        fric = baseGroundFriction;
        if(!OnGround)
        {
            accel = baseAirAcceleration;
            fric = baseAirFriction;
        }
    }
}

public class PlayerInputMapping
{
    public MappedInput Right { get; set; } = new MappedInput.Keyboard(Keys.D);
    public MappedInput Left { get; set; } = new MappedInput.Keyboard(Keys.A);
    public MappedInput Down { get; set; } = new MappedInput.Keyboard(Keys.S);
    public MappedInput Up { get; set; } = new MappedInput.Keyboard(Keys.W);
    public MappedInput Jump { get; set; } = new MappedInput.Keyboard(Keys.Space);
    public MappedInput PrimaryFire { get; set; } = new MappedInput.Mouse(MouseButtons.LeftButton);
    public MappedInput SecondaryFire { get; set; } = new MappedInput.Mouse(MouseButtons.RightButton);
}
