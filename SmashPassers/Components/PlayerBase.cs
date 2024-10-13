using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jelly;
using Jelly.Components;
using Jelly.Graphics;

using SmashPassers.Graphics;

namespace SmashPassers.Components;

public class PlayerBase : Actor
{
    public const float Gravity = 20;
    public const float TerminalVelocity = 30;

    private static readonly Vector2 _defaultPivot = new(240, 332);

    public static Vector2 DefaultPivot => _defaultPivot;

    public Point HitboxOffset { get => bboxOffset; set => bboxOffset = value; }

    private string _state = "normal"; // please do NOT touch this thx
    private bool _stateJustChanged;

    public int StateTimer { get; protected set; }

    public string State {
        get => _state;
        protected set {
            if(_state != value)
            {
                _stateJustChanged = true;
                StateTimer = 0;

                OnStateExit(_state);

                _state = value;

                OnStateEnter(_state);
            }
        }
    }

    // baseline
    protected float baseMoveSpeed = 10;
    protected float baseJumpSpeed = -12;
    protected float baseGroundAcceleration = 5;
    protected float baseGroundFriction = 12;
    protected float baseAirAcceleration = 2;
    protected float baseAirFriction = 1;
    protected int baseJumpCount = 2; 

    // current
    private float moveSpeed;
    private float jumpSpeed;
    private float accel;
    private float fric;

    private int jumpCount;

    private bool jumpCancelled;
    private bool wasOnGround;
    private bool onJumpthrough;

    protected bool IsRunning { get; private set; }
    protected int InputDir { get; private set; }

    /// <summary>
    /// Set to true to enable after image effect
    /// </summary>
    protected bool FxTrail { get; set; }

    private int fxTrailCounter;
    private readonly List<AfterImage> afterImages = [];

    protected string AnimationId { get; set; }

    protected AnimatedSprite sprite = new();

    public PlayerInputMapping InputMapping { get; } = new();

    public bool UseGamePad { get; set; }
    public PlayerIndex GamePadIndex { get; set; }

    class AfterImage
    {
        public float Alpha = 1;
        public string TexturePath;
        public Vector2 Position;
        public SpriteEffects SpriteEffects;
        public Vector2 Pivot = Vector2.Zero;
        public Vector2 Scale = Vector2.One;
        public Color Color = Color.White;
        public float Rotation;

        public void Draw()
        {
            Renderer.SpriteBatch.Draw(
                ContentLoader.LoadContent<Texture2D>(TexturePath),
                Position,
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
        base.OnCreated();

        AnimationId = "idle";
        Entity.Depth = 50;
        Entity.Tag.Add(EntityTags.Player);
        MaxContinousMovementThreshold = 64f;
        jumpCount = baseJumpCount;
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

    protected void AddAnimation(AnimatedSprite.Animation animation)
    {
        animation.Sprite = sprite;
        animation.Pivot ??= DefaultPivot;

        sprite.Animations[animation.Id] = animation;
    }

    protected void SetHitbox(Rectangle mask, Point pivot)
    {
        bboxOffset = mask.Location;
        Width = mask.Width;
        Height = mask.Height;
        Entity.Pivot = pivot;
    }

    public override void Update()
    {
        InputDir = InputMapping.Right.IsDown.ToInt32() - InputMapping.Left.IsDown.ToInt32();

        wasOnGround = OnGround;
        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
        if(onJumpthrough) OnGround = true;
        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));

        if(!wasOnGround && OnGround)
        {
            // onland
            if(!CheckColliding(BottomEdge.Shift(1, 1)))
            {
                Entity.X++;
                Entity.Y++;
            }
            else if(!CheckColliding(BottomEdge.Shift(-1, 1)))
            {
                Entity.X--;
                Entity.Y++;
            }

            jumpCount = baseJumpCount;
            jumpCancelled = false;
        }

        velocity.Y = Util.Approach(velocity.Y, TerminalVelocity, Gravity * Time.DeltaTime);

        RecalculateStats();

        if(!_stateJustChanged)
        {
            CollidesWithJumpthroughs = true;
            CollidesWithSolids = true;
        }
        else
        {
            _stateJustChanged = false;
        }

        StateUpdate();

        if(jumpCount > 0 && InputMapping.Jump.Pressed)
        {
            velocity.Y = jumpSpeed;
            jumpCount -= 1;
        }

        MoveX(velocity.X * (Time.DeltaTime * 60), () => {
            if(State == "dead")
            {
                velocity.X = -velocity.X * 0.9f;
            }
            else for(int j = 0; j < Util.RoundToInt(MathHelper.Max(Time.DeltaTime, 1)); j++)
            {
                var nudgeDistance = OnGround ? -15 : -10;
                if(InputDir != 0 && !CheckColliding(Hitbox.Shift(InputDir, nudgeDistance)))
                {
                    if(CheckColliding(Hitbox.Shift(InputDir, 0), true) && !CheckColliding(Hitbox.Shift(0, nudgeDistance), true))
                    {
                        MoveY(nudgeDistance, null);
                        MoveX(InputDir * -nudgeDistance, null);
                    }
                }
                else
                {
                    // if (Math.Abs(velocity.X) >= 1)
                    // {
                    //     _audio_play_sound(sn_player_land, 0, false);
                    //     for (int i = 0; i < 3; i++)
                    //     {
                    //         with(instance_create_depth((x + (4 * sign(facing))), random_range((bbox_bottom - 12), (bbox_bottom - 2)), (depth - 1), fx_dust))
                    //         {
                    //             sprite_index = spr_fx_dust2;
                    //             vy = (Math.Abs(other.velocity.Y) > 0.6) ? other.velocity.Y * 0.5 : vy;
                    //             vz = 0;
                    //         }
                    //     }
                    // }
                    velocity.X = 0;
                    break;
                }
            }
        });
        MoveY(velocity.Y * (Time.DeltaTime * 60), () => {
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
                afterImages.Add(new AfterImage {
                    TexturePath = sprite.CurrentAnimation.ActiveTexturePath,
                    Position = Entity.Position.ToVector2() + sprite.CurrentAnimation.ActiveOffset,
                    SpriteEffects = sprite.CurrentAnimation.SpriteEffects,
                    Scale = sprite.CurrentAnimation.ActiveScale,
                    Color = sprite.CurrentAnimation.ActiveColor,
                    Rotation = sprite.CurrentAnimation.ActiveRotation,
                    Pivot = sprite.CurrentAnimation.ActivePivot,
                    Alpha = 0.75f * sprite.CurrentAnimation.ActiveAlpha
                });
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
                i--;
            }
        }
    }

    protected virtual void OnStateEnter(string state)
    {
        switch(state)
        {
            case "none":
                break;
            case "normal":
                break;
            case "dead":
                break;
            default:
                break;
        }
    }

    protected virtual void StateUpdate()
    {
        switch (_state)
        {
            case "normal":
            {
                if(InputDir != 0)
                {
                    Facing = InputDir;

                    if(InputMapping.PrimaryFire.Pressed)
                    {
                        velocity.X += 15 * InputDir;
                        velocity.Y = MathHelper.Min(-2, velocity.Y);
                    }

                    if(OnGround)
                    {
                        IsRunning = true;
                        AnimationId = "run";
                    }

                    if(InputDir * velocity.X < 0)
                    {
                        if(OnGround && InputDir * velocity.X < -2)
                            AnimationId = "skid";

                        velocity.X = Util.Approach(velocity.X, 0, fric * Time.DeltaTime);
                    }
                    if(InputDir * velocity.X < moveSpeed)
                    {
                        velocity.X = Util.Approach(velocity.X, InputDir * moveSpeed, accel * Time.DeltaTime);
                    }

                    if(InputDir * velocity.X > moveSpeed && OnGround)
                    {
                        velocity.X = Util.Approach(velocity.X, InputDir * moveSpeed, fric * 2 * Time.DeltaTime);
                    }
                }
                else
                {
                    IsRunning = false;
                    velocity.X = Util.Approach(velocity.X, 0, fric * 2 * Time.DeltaTime);

                    if(OnGround)
                    {
                        if(Math.Abs(velocity.X) < 1)
                        {
                            AnimationId = "idle";
                        }
                    }
                }

                if(!OnGround)
                {
                    if(InputMapping.Jump.Released && velocity.Y < 0 && !jumpCancelled)
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

                break;
            }
            case "none": default:
                break;
        }
    }

    protected virtual void OnStateExit(string state)
    {
        switch(state)
        {
            case "none":
                break;
            case "normal":
                break;
            case "dead":
                break;
            default:
                break;
        }
    }

    public override void PreDraw()
    {
        sprite.SetAnimation(AnimationId);
        if(sprite.CurrentAnimation is not null)
        {
            sprite.CurrentAnimation.SpriteEffects = SpriteEffects;

            if(InputDir == 0 && velocity.X == 0)
            {
                sprite.CurrentAnimation.PlaybackSpeed = 0.2f;
            }
            else if(IsRunning)
            {
                sprite.CurrentAnimation.PlaybackSpeed = Math.Abs(velocity.X) / 2.5f;
            }
        }

        sprite.Update();
    }

    public override void Draw()
    {
        foreach(var img in afterImages)
        {
            img.Draw();
        }

        sprite.Draw(Entity.Position.ToVector2());
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
