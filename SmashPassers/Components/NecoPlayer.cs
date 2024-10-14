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
        baseJumpCount = 2;

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

    protected override void OnStateEnter(string state)
    {
        switch(state)
        {
            default:
                base.OnStateEnter(state);
                break;
        }
    }

    protected override void OnStateExit(string state)
    {
        switch(state)
        {
            default:
                base.OnStateEnter(state);
                break;
        }
    }

    protected override void StateUpdate()
    {
        switch(State)
        {
            default:
                base.StateUpdate();
                break;
        }
    }
}
