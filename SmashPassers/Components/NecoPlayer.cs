using Microsoft.Xna.Framework;

namespace SmashPassers.Components;

public class NecoPlayer : PlayerBase
{
    private Rectangle MaskNormal = new(-32/2, -70, 32, 70);
    private Point PivotNormal = DefaultPivot.ToPoint();

    public override void OnCreated()
    {
        base.OnCreated();

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
        base.OnStateEnter(state);
    }

    protected override void OnStateExit(string state)
    {
        base.OnStateExit(state);
    }

    protected override void StateUpdate()
    {
        switch(State)
        {
            default: base.StateUpdate(); break;
        }
    }
}
