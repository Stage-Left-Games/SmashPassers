using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Jelly;
using Jelly.Components;
using Jelly.Graphics;

using SmashPassers.Graphics;
using Jelly.Utilities;

namespace SmashPassers.Components;

public class PlayerBase : Actor
{
    public const float Gravity = 20;
    public const float TerminalVelocity = 23.15f;

    private static readonly Vector2 _defaultPivot = new(240, 332);

    public static Vector2 DefaultPivot => _defaultPivot;

    public Point HitboxOffset { get => bboxOffset; set => bboxOffset = value; }

    private string _state = BaseStates.Normal; // please do NOT touch this thx
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

    protected class BaseStates
    {
        public const string Normal = "normal";
        public const string Dead = "dead";
        public const string None = "none";
        public const string WallRun = "wallRun";
        public const string LedgeGrab = "ledgeGrab";
    }

    private Vector2 lastVelocity;
    private Point lastPosition;
    private Vector2 velAtStartOfState;

    // baseline
    protected float baseMoveSpeed = 15;
    protected float baseJumpSpeed = -12;
    protected float baseGroundAcceleration = 5;
    protected float baseGroundFriction = 12;
    protected float baseAirAcceleration = 2;
    protected float baseAirFriction = 1;
    protected float baseAbilityCooldown = 5;

    protected int baseJumpCount = 1;
    protected int baseBoostCount = 1;

    // current
    protected bool canUseAbility;
    protected bool canBoost;
    protected bool canJump;
    protected bool canWallJump;
    protected bool canLedgeGrab;
    protected bool useGravity;

    protected float moveSpeed;
    protected float jumpSpeed;
    protected int jumpCount;
    protected int boostCount;
    private float accel;
    private float fric;

    private float abilityCooldown;
    private float boostedJumpTimer;
    private float boostedJumpSpeed;
    private float jumpEarlyBuffer;
    private float coyoteJumpBuffer;
    private float ledgegrabCooldown;
    private float wallRunReactivationTimer;
    private float wallRunCooldown;

    private bool sloping;
    private bool wasOnGround;
    private bool onJumpthrough;

    protected bool CanGroundPoundBoost { get; set; } = true;

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

    public PlayerInputMapping InputMapping { get; } = new() {
        // Left = new MappedInput.GamePad(Buttons.LeftThumbstickLeft, PlayerIndex.One),
        // Right = new MappedInput.GamePad(Buttons.LeftThumbstickRight, PlayerIndex.One),
        // Up = new MappedInput.GamePad(Buttons.LeftThumbstickUp, PlayerIndex.One),
        // Down = new MappedInput.GamePad(Buttons.LeftThumbstickDown, PlayerIndex.One),
        // Jump = new MappedInput.GamePad(Buttons.A, PlayerIndex.One),
        // Boost = new MappedInput.GamePad(Buttons.X, PlayerIndex.One),
    };

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
        base.EntityAwake();

        Main.Players.Add(this);
    }

    public override void OnEnable()
    {
        base.OnEnable();

        Main.Players.Add(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        Main.Players.Remove(this);
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);

        Main.Players.Remove(this);
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

        abilityCooldown = MathUtil.Approach(abilityCooldown, 0, Time.DeltaTime);
        ledgegrabCooldown = MathUtil.Approach(ledgegrabCooldown, 0, Time.DeltaTime);
        wallRunReactivationTimer = MathUtil.Approach(wallRunReactivationTimer, 0, Time.DeltaTime);
        wallRunCooldown = MathUtil.Approach(wallRunCooldown, 0, Time.DeltaTime);

        if(OnGround)
        {
            if (!wasOnGround)
            {
                OnLand();
            }

            canBoost = true;
            boostCount = baseBoostCount;
            coyoteJumpBuffer = 10/60f;
            boostedJumpTimer = MathUtil.Approach(boostedJumpTimer, 0, Time.DeltaTime);
        }
        else
        {
            coyoteJumpBuffer = MathUtil.Approach(coyoteJumpBuffer, 0, Time.DeltaTime);
            jumpEarlyBuffer = MathUtil.Approach(jumpEarlyBuffer, 0, Time.DeltaTime);
        }

        RecalculateStats();

        if (!_stateJustChanged)
        {
            CollidesWithJumpthroughs = true;
            CollidesWithSolids = true;
        }
        else
        {
            _stateJustChanged = false;
        }

        StateUpdate();

        if(OnGround && InputMapping.Down.Pressed && !onJumpthrough)
        {
            velocity.Y += Gravity * 4 * Time.DeltaTime;
        }

        if (!OnGround && useGravity)
        {
            var grv = Gravity;
            var term = TerminalVelocity;
            if (InputMapping.Down.IsDown && velocity.Y > 2 && !onJumpthrough)
            {
                if (velocity.Y < 20)
                    velocity.Y = 20;
                velocity.Y += 20 * Time.DeltaTime;
                grv += 5;
                term *= 30 / 23.15f;
            }

            velocity.Y = MathUtil.Approach(velocity.Y, term, grv * Time.DeltaTime);
        }

        if (InputMapping.Jump.Pressed || (OnGround && jumpEarlyBuffer > 0))
        {
            if(!OnGround)
                jumpEarlyBuffer = 15/60f;

            if(!TryWallJump() && canJump)
            {
                if(!OnGround && coyoteJumpBuffer > 0 && velocity.Y > 0)
                {
                    coyoteJumpBuffer = 0;
                    DoJump(false);
                }
                else if(OnGround && boostedJumpTimer > 0)
                {
                    boostedJumpTimer = 0;
                    DoJump(false, MathHelper.Max(1, boostedJumpSpeed));
                }
                else if(jumpCount > 0)
                    DoJump();
            }
        }

        if (!OnGround && canJump)
        {
            if (InputMapping.Jump.Released && velocity.Y < 0)
            {
                velocity.Y /= 4;
            }
        }

        if(canBoost && boostCount > 0 && InputMapping.Boost.Pressed && InputDir != 0)
        {
            velocity.X += MathHelper.Max(0, 15 - Math.Abs(velocity.X / 3)) * InputDir;
            velocity.Y = MathHelper.Min(-2, velocity.Y);
            boostCount--;
        }

        if(!OnGround)
        {
            if(velocity.Y >= -2)
                CheckLedgeGrab();
        }

        if (Input.GetDown(Keys.LeftControl))
        {
            velocity = Vector2.Zero;
            Entity.Position = Main.Camera.MousePositionInWorld;
        }

        if(!float.IsNormal(velocity.X)) velocity.X = 0;
        if(!float.IsNormal(velocity.Y)) velocity.Y = 0;

        lastPosition = Entity.Position;

        MoveX(velocity.X * (Time.DeltaTime * 60), OnCollideX);
        MoveY(velocity.Y * (Time.DeltaTime * 60), OnCollideY);

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

        if (FxTrail)
        {
            fxTrailCounter++;
            if (fxTrailCounter >= 3)
            {
                fxTrailCounter = 0;
                afterImages.Add(new AfterImage
                {
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

        for (int i = 0; i < afterImages.Count; i++)
        {
            AfterImage image = afterImages[i];

            image.Alpha = MathHelper.Max(image.Alpha - (1 / 20f), 0);
            if (image.Alpha == 0)
            {
                afterImages.RemoveAt(i);
                i--;
            }
        }

        lastVelocity = velocity;
    }

    protected virtual void OnCollideX()
    {
        if (State == BaseStates.Dead)
        {
            velocity.X = -velocity.X * 0.9f;
        }
        else for (int j = 0; j < MathUtil.RoundToInt(MathHelper.Max(Time.DeltaTime * 60, 1)); j++)
        {
            bool slopeCondition = !sloping || (sloping && InputDir == Math.Sign(Entity.X - lastPosition.X));
            var nudgeDistance = OnGround ? -15 : -10;
            if (InputDir != 0 && slopeCondition && !CheckColliding(Hitbox.Shift(InputDir, nudgeDistance), true))
            {
                int n = 0;
                if (CheckColliding(Hitbox.Shift(InputDir, 0), true))
                {
                    if (OnGround) velocity.Y = 0;

                    while (n < Math.Abs(nudgeDistance))
                    {
                        MoveY(-1, null);
                        n++;
                    }
                    MoveX(InputDir * Math.Abs(nudgeDistance), null);
                }
            }
            else if (InputDir != 0 && slopeCondition && !CheckColliding(Hitbox.Shift(InputDir, 10)))
            {
                int n = 0;
                if (CheckColliding(Hitbox.Shift(InputDir, 0), true))
                {
                    while (n < 10)
                    {
                        MoveY(1, null);
                        n++;
                    }
                    MoveX(InputDir, null);
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
    }

    protected virtual void OnCollideY()
    {
        if (velocity.Y > 0)
        {
            if (InputMapping.Down.IsDown)
            {
                if (CanGroundPoundBoost && (!sloping || Math.Sign(Entity.X - lastPosition.X) != InputDir))
                {
                    if (InputDir != 0 && velocity.X * InputDir < 3)
                        velocity.X = 7.5f * InputDir * (velocity.Y / TerminalVelocity * 2 - 0.5f);
                    boostedJumpTimer = 10 / 60f;
                    boostedJumpSpeed = velocity.Y / TerminalVelocity * 1.5f - 0.5f;
                }

                if (CheckCollidingJumpthrough(BottomEdge.Shift(new(0, 1))))
                    return;
            }

            if(sloping)
            {
                velocity.X = MathHelper.Max(Math.Abs(velocity.Y) * 1.2f, Math.Abs(velocity.X)) * Math.Sign(Entity.X - lastPosition.X);
            }
        }

        velocity.Y = 0;
    }

    protected bool TryWallJump(float horizontalSpeedMultiplier = 1)
    {
        if(OnGround) return false;
        if(!canWallJump) return false;

        var wallDistance = (int)Math.Max(1, Math.Abs(velocity.X) / 2);
        if (CheckColliding(RightEdge.Shift(wallDistance, 0), true) && CheckColliding(RightEdge.Shift(wallDistance, -15), true))
        {
            State = BaseStates.Normal;

            Facing = -1;
            velocity.X = -8 * horizontalSpeedMultiplier;
            velocity.Y = 0.75f * jumpSpeed;
            jumpCount = baseJumpCount - 1;
            boostCount = baseBoostCount;

            // walljump sfx / animation

            return true;
        }
        else if (CheckColliding(LeftEdge.Shift(-wallDistance, 0), true) && CheckColliding(LeftEdge.Shift(-wallDistance, -15), true))
        {
            State = BaseStates.Normal;

            Facing = 1;
            velocity.X = 8 * horizontalSpeedMultiplier;
            velocity.Y = 0.75f * jumpSpeed;
            jumpCount = baseJumpCount - 1;
            boostCount = baseBoostCount;

            // walljump sfx / animation

            return true;
        }

        return false;
    }

    protected void DoJump(bool subtractJumps = true, float multiplier = 1)
    {
        State = BaseStates.Normal;

        if(sloping) velocity.X = Math.Max(Math.Abs(velocity.Y), Math.Abs(velocity.X)) * Math.Sign(Entity.X - lastPosition.X);

        velocity.Y = jumpSpeed * multiplier;

        // begin jump animation, play sfx

        if(subtractJumps)
            jumpCount--;
    }

    protected void CheckLedgeGrab()
    {
        var _w = Scene.CollisionSystem.SolidPlace(Hitbox.Shift(InputDir, 0));
        if (canLedgeGrab && ledgegrabCooldown == 0 && _w is not null && !CheckColliding(Hitbox))
        {
            if(!CheckColliding(new((InputDir == 1) ? _w.Left + 1 : _w.Right - 1, _w.Top - 1, 1, 1), true)
            && !CheckColliding(new((InputDir == 1) ? _w.Left - 2 : _w.Right + 2, _w.Top + 35, 1, 1), true))
            {
                if (Math.Sign(Top - _w.Top) <= 0 && !CheckColliding(new(Left, _w.Top - 1, Width, Height), true) && !CheckColliding(Hitbox.Shift(0, 2), true))
                {
                    // wallslideTimer = 0;
                    State = BaseStates.LedgeGrab;

                    Entity.Y = _w.Top - bboxOffset.Y;
                    Entity.X = (InputDir == 1 ? _w.Left - Width : _w.Right) - bboxOffset.X;
                    Facing = Math.Sign(_w.Left - Left);

                    // SetHitbox(MaskLedge, PivotLedge);

                    // // set animation
                    // textureIndex = TextureIndex.LedgeGrab;

                    // platformTarget = _w;

                    return;
                }
            }
        }
    }

    protected virtual void OnLand()
    {
        // compensation for if player pressed jump slightly too early before landing
        if (jumpEarlyBuffer > 0)
        {
            jumpEarlyBuffer = 0;
            DoJump(false);
        }

        jumpCount = baseJumpCount;
    }

    protected virtual void OnStateEnter(string state)
    {
        switch(state)
        {
            case BaseStates.Normal:
                break;
            case BaseStates.Dead:
                velocity.X = MathHelper.Clamp(velocity.X, -8, 8);
                break;
            case BaseStates.WallRun:
            {
                velAtStartOfState = velocity;

                velocity.X = 0;
                velocity.Y = Math.Min(-30, -Math.Abs(velocity.X) / 2);

                break;
            }
            case BaseStates.LedgeGrab:
            {
                velocity = Vector2.Zero;
                if(Facing == 0) Facing = 1;

                jumpCount  = baseJumpCount - 1;

                break;
            }
            case BaseStates.None: default:
                break;
        }
    }

    protected virtual void OnStateExit(string state)
    {
        switch(state)
        {
            case BaseStates.Normal:
                break;
            case BaseStates.Dead:
                break;
            case BaseStates.WallRun:
            {
                canBoost = true;
                canWallJump = true;
                wallRunCooldown = 0.4f;

                break;
            }
            case BaseStates.LedgeGrab:
                ledgegrabCooldown = 15/60f;
                break;
            case BaseStates.None: default:
                break;
        }
    }

    protected virtual void StateUpdate()
    {
        switch (_state)
        {
            case BaseStates.Normal:
            {
                if(InputDir != 0)
                {
                    Facing = InputDir;

                    if(OnGround)
                    {
                        IsRunning = true;
                        AnimationId = "run";
                    }

                    if(InputDir * velocity.X < 0)
                    {
                        if(OnGround && InputDir * velocity.X < -2)
                            AnimationId = "skid";

                        velocity.X = MathUtil.Approach(velocity.X, 0, fric * Time.DeltaTime);
                    }
                    if(InputDir * velocity.X < moveSpeed)
                    {
                        velocity.X = MathUtil.Approach(velocity.X, InputDir * moveSpeed, accel * Time.DeltaTime);
                    }

                    if(InputDir * velocity.X > moveSpeed && OnGround)
                    {
                        velocity.X = MathUtil.Approach(velocity.X, InputDir * moveSpeed, fric * 2 * Time.DeltaTime);
                    }
                }
                else
                {
                    IsRunning = false;
                    velocity.X = MathUtil.Approach(velocity.X, 0, fric * 2 * Time.DeltaTime);

                    if(OnGround)
                    {
                        if(Math.Abs(velocity.X) < 1)
                        {
                            AnimationId = "idle";
                        }
                    }
                }

                if (OnGround)
                {
                    if (onJumpthrough && velocity.Y == 0 && InputMapping.Down.IsDown && InputMapping.Jump.Pressed && !CheckColliding(BottomEdge.Shift(new(0, 2)), true))
                    {
                        Entity.Y += 2;

                        onJumpthrough = CheckCollidingJumpthrough(BottomEdge.Shift(0, 1));
                        if (onJumpthrough) OnGround = true;
                        else OnGround = CheckColliding(BottomEdge.Shift(0, 1));
                    }
                }

                FxTrail = Math.Abs(velocity.X) > 10;

                var edge = InputDir >= 0 ? RightEdge : LeftEdge;

                // check for and initiate wallRun
                if (wallRunCooldown <= 0
                    && InputDir != 0
                    && !OnGround
                    && Math.Sign(velocity.X) == Math.Sign(InputDir)
                    && ((Math.Abs(velocity.X) > moveSpeed) || wallRunReactivationTimer > 0)
                    && velocity.Y < 20f
                    && CheckColliding(edge.Shift((int)moveSpeed * InputDir, 0), true)
                    && CheckColliding(edge.Shift((int)moveSpeed * InputDir, -15), true))
                {
                    MoveX(moveSpeed * InputDir, null);
                    Facing = InputDir;
                    State = BaseStates.WallRun;
                }

                break;
            }

            case BaseStates.Dead:
            {
                velocity.X = MathUtil.Approach(velocity.X, 0, 5 * Time.DeltaTime);

                if(!OnGround)
                    velocity.Y = MathUtil.Approach(velocity.Y, TerminalVelocity, Gravity * Time.DeltaTime);

                MoveX(velocity.X * (Time.DeltaTime * 60), () => velocity.X *= -0.8f);
                MoveX(velocity.Y * (Time.DeltaTime * 60), () => velocity.Y = 0);

                break;
            }

            case BaseStates.WallRun:
            {
                useGravity = false;
                canJump = false;
                canBoost = false;
                canWallJump = false;
                jumpSpeed = 0.75f * baseJumpSpeed;

                FxTrail = true;

                velocity.Y = MathUtil.Approach(velocity.Y, 0, Gravity * 2 * Time.DeltaTime);
                if(velocity.Y >= -5)
                {
                    State = BaseStates.Normal;
                    velocity.Y = 0;
                    break;
                }

                if(InputMapping.Jump.Pressed)
                {
                    wallRunReactivationTimer = 0.5f; // half a second
                    // jumpSpeed = 0.5f * baseJumpSpeed;
                    jumpSpeed = velocity.Y - 5f;
                    canWallJump = true;
                    TryWallJump(3f);
                    break;
                }

                if(InputDir == -Facing)
                {
                    canBoost = true;
                }

                if(!CheckColliding(BottomEdge.Shift(Facing, 0), true))
                {
                    if (InputDir == Facing || InputDir == 0)
                    {
                        State = BaseStates.Normal;
                        velocity.Y = jumpSpeed * 0.1f;

                        if(InputDir != 0)
                            velocity.X = velAtStartOfState.X * 0.65f;
                    }
                    else
                    {
                        jumpSpeed = 1.5f * baseJumpSpeed;
                        canWallJump = true;
                        TryWallJump(0);
                        velocity.X = Math.Max(Math.Abs(velAtStartOfState.X) * 0.6f, 15) * InputDir;
                    }
                    break;
                }

                if(CheckColliding(TopEdge.Shift(0, (int)velocity.Y), true))
                {
                    State = BaseStates.Normal;
                    break;
                }

                break;
            }

            case BaseStates.LedgeGrab:
            {
                useGravity = false;
                canBoost = false;
                canLedgeGrab = false;
                canWallJump = false;

                break;
            }

            case BaseStates.None: default:
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

        sprite.CurrentAnimation.Scale = new(
            MathHelper.Min(1, (TerminalVelocity - velocity.Y) / 40 + 0.9f),
            sprite.CurrentAnimation.Scale.Y
        );
        sprite.Draw(Entity.Position.ToVector2());
    }

    public override void DrawUI()
    {
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"Horizontal:{(velocity.X >= 0 ? " " : "")}{SpeedConverter.PpfToKph(velocity.X):F2} km/h",
            position: new Vector2(4, 0),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"Horizontal:{(velocity.X >= 0 ? " " : "")}{velocity.X:F2} px",
            position: new Vector2(4, 20),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"Vertical:{(velocity.Y >= 0 ? " " : "")}{SpeedConverter.PpfToKph(velocity.Y):F2} km/h",
            position: new Vector2(4, 40),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"Vertical: {(velocity.Y >= 0 ? " " : "")}{velocity.Y:F2} px",
            position: new Vector2(4, 60),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"Gravity: {SpeedConverter.PpfToKph(Gravity) / 3.6f :F2} m/s",
            position: new Vector2(4, 80),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"State: {_state}",
            position: new Vector2(4, 100),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"Sloping: {sloping}",
            position: new Vector2(4, 120),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
        Renderer.SpriteBatch.DrawStringSpacesFix(
            GraphicsUtilities.Fonts.RegularFont,
            text: $"BoostCount: {boostCount}",
            position: new Vector2(4, 140),
            color: Color.White,
            spaceSize: 6,
            rotation: 0,
            origin: Vector2.Zero,
            scale: 2
        );
    }

    private void RecalculateStats()
    {
        moveSpeed = baseMoveSpeed;
        jumpSpeed = baseJumpSpeed;

        useGravity = true;
        canJump = true;
        canWallJump = true;
        canUseAbility = true;
        canLedgeGrab = true;

        accel = baseGroundAcceleration;
        fric = baseGroundFriction;
        if(!OnGround)
        {
            accel = baseAirAcceleration * MathHelper.Min(1, (TerminalVelocity - velocity.Y) / 40 + 0.9f);
            fric = baseAirFriction * MathHelper.Min(1, (TerminalVelocity - velocity.Y) / 40 + 0.9f);
        }
    }

    // to gan: please, dont touch this lmao
    // it'll break really bad its so finicky
    public override void MoveX(float amount, Action? onCollide)
    {
		if(!float.IsNormal(amount)) amount = 0;

        RemainderX += amount;
        int move = (int)Math.Round(RemainderX);
        RemainderX -= move;

        if(move != 0)
        {
			if(!Collidable || Math.Abs(amount) > MaxContinousMovementThreshold)
			{
				Entity.X += move;
				return;
			}

            int sign = Math.Sign(move);
            while(move != 0)
            {
                bool col1 = CheckColliding((sign >= 0 ? RightEdge : LeftEdge).Shift(sign, 0));
                if(NudgeOnMove && col1 && !CheckColliding((sign >= 0 ? RightEdge : LeftEdge).Shift(sign, -1), true))
                {
                    // slope up
                    Entity.X += sign;
                    Entity.Y -= 1;
                    if(Math.Abs(velocity.X) > 5 && InputDir == sign)
                        velocity.X -= 0.03f * sign;
                    move -= sign;
                }
                else if(!col1)
                {
                    if(NudgeOnMove && OnGround)
                    {
                        // slope down
                        if(!CheckColliding(BottomEdge.Shift(sign, 1)) && CheckColliding(BottomEdge.Shift(sign, 2)))
                        {
                            Entity.Y += 1;
                            if(InputDir == sign)
                                velocity.Y += 0.02f;
                        }
                    }
                    Entity.X += sign;
                    move -= sign;
                }
                else
                {
                    onCollide?.Invoke();
                    break;
                }
            }
        }
    }

    public override void MoveY(float amount, Action? onCollide)
    {
		if(!float.IsNormal(amount)) amount = 0;

        RemainderY += amount;
        int move = (int)Math.Round(RemainderY);
        RemainderY -= move;

        if(move != 0)
        {
			if(!Collidable || Math.Abs(amount) > MaxContinousMovementThreshold)
			{
				Entity.Y += move;
				return;
			}

            int maxNudge = 5;

            int sign = Math.Sign(move);
            bool ignoreJumpthrus = sign < 0 || InputMapping.Down.IsDown;

            while(move != 0)
            {
                var rect = (sign >= 0 ? BottomEdge : TopEdge).Shift(0, sign);
                if (CheckColliding(rect, ignoreJumpthrus))
                {
                    if (InputMapping.Down.IsDown && !CheckColliding(rect.Shift(maxNudge, 0), ignoreJumpthrus))
                    {
                        sloping = true;
                        velocity.X += 0.01f;

                        for(var n = 0; n < maxNudge; n++)
                        {
                            if(!CheckColliding(LeftEdge.Shift(0, sign), ignoreJumpthrus))
                                break;

                            Entity.X++;
                        }
                        Entity.Y += sign;
                        move -= sign;
                        continue;
                    }
                    else if (InputMapping.Down.IsDown && !CheckColliding(rect.Shift(-maxNudge, 0), ignoreJumpthrus))
                    {
                        sloping = true;
                        velocity.X += 0.01f;

                        for(var n = 0; n < maxNudge; n++)
                        {
                            if(!CheckColliding(RightEdge.Shift(0, sign), ignoreJumpthrus))
                                break;

                            Entity.X--;
                        }
                        Entity.Y += sign;
                        move -= sign;
                        continue;
                    }
                    else
                    {
                        onCollide?.Invoke();
                        sloping = false;
                        break;
                    }
                }
                else
                {
                    sloping = false;
                    Entity.Y += sign;
                    move -= sign;
                }
            }
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
    public MappedInput Boost { get; set; } = new MappedInput.Mouse(MouseButtons.LeftButton);
    public MappedInput UseAbility { get; set; } = new MappedInput.Mouse(MouseButtons.LeftButton);
    public MappedInput UseItem { get; set; } = new MappedInput.Mouse(MouseButtons.RightButton);
}
