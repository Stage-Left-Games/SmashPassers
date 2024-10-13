using System;
using Jelly;
using Jelly.Utilities;
using Microsoft.Xna.Framework;

namespace SmashPassers.Components;

public class NecoPlayer : PlayerBase
{
    private Rectangle MaskNormal = new(-32/2, -70, 32, 70);
    private Point PivotNormal = DefaultPivot.ToPoint();

    public override void OnCreated()
    {
        base.OnCreated();

        baseMoveSpeed = 15;
        baseJumpSpeed = -12;
        baseGroundAcceleration = 10;
        baseGroundFriction = 12;
        baseAirAcceleration = 5;
        baseAirFriction = 1;
        baseJumpCount = 1;

        SetHitbox(MaskNormal, PivotNormal);

        AddAnimation(new("idle") {
            TexturePath = "Images/Characters/neco/neco_0",
        });
        AddAnimation(new("run") {
            TexturePath = "Images/Characters/neco/neco_0",
        });
        AddAnimation(new("skid") {
            TexturePath = "Images/Characters/neco/neco_0",
        });
    }

    private Vector2 velAtStartOfState;

    protected override void OnStateEnter(string state)
    {
        switch(state)
        {
            case "wallrun":
            {
                velAtStartOfState = velocity;

                velocity.X = 0;
                velocity.Y = -30;

                break;
            }

            default:
                base.OnStateEnter(state);
                break;
        }
    }

    protected override void OnStateExit(string state)
    {
        switch(state)
        {
            case "wallrun":
            {
                canBoost = true;
                canWallJump = true;

                break;
            }

            default:
                base.OnStateEnter(state);
                break;
        }
    }

    protected override void StateUpdate()
    {
        switch(State)
        {
            case BaseStates.Normal:
            {
                base.StateUpdate();

                var edge = InputDir >= 0 ? RightEdge : LeftEdge;

                // check for and initiate wallrun
                if (InputDir != 0
                    && !OnGround
                    && Math.Sign(velocity.X) == Math.Sign(InputDir)
                    && Math.Abs(velocity.X) > moveSpeed * 1.5f
                    && velocity.Y < 10
                    && CheckColliding(edge.Shift((int)velocity.X, 0), true)
                    && CheckColliding(edge.Shift((int)velocity.X, -15), true))
                {
                    MoveX(velocity.X, null);
                    Facing = InputDir;
                    State = "wallrun";
                }

                break;
            }

            case "wallrun":
            {
                canJump = false;
                canBoost = false;
                canWallJump = false;
                useGravity = false;
                jumpSpeed = 0.75f * baseJumpSpeed;

                FxTrail = true;

                // velocity.Y = MathUtil.Approach(velocity.Y, 0, baseAirFriction * 0.75f * Time.DeltaTime);

                // if(velocity.Y > -5)
                // {
                //     State = BaseStates.Normal;
                //     break;
                // }

                if(InputMapping.Jump.Pressed && InputDir == -Facing)
                {
                    jumpSpeed = 0.5f * baseJumpSpeed;
                    canWallJump = true;
                    TryWallJump(2.3f);
                    break;
                }

                if(!CheckColliding(BottomEdge.Shift(Facing, 0), true))
                {
                    if (InputDir == Facing || InputDir == 0)
                    {
                        State = BaseStates.Normal;
                        velocity.Y = MathHelper.Max(velocity.Y, jumpSpeed * 0.3f);
                        velocity.X = velAtStartOfState.X * 0.65f;
                    }
                    else
                    {
                        jumpSpeed = 1.5f * baseJumpSpeed;
                        canWallJump = true;
                        TryWallJump(0);
                        velocity.X = Math.Abs(velAtStartOfState.X) * 0.6f * InputDir;
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

            default:
                base.StateUpdate();
                break;
        }
    }
}
