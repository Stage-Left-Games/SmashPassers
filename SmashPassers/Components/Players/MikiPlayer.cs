using Microsoft.Xna.Framework;

namespace SmashPassers.Components;

public class MikiPlayer : PlayerBase
{
    private Rectangle MaskNormal = new(-128/2, -304, 128, 304);
    private Point PivotNormal = DefaultPivot.ToPoint();

    public override void OnCreated()
    {
        base.OnCreated();

        SetHitbox(MaskNormal, PivotNormal);

        AddAnimation(new("idle") {
            TexturePath = "Images/Characters/miki/miki_0",
            Frames = {new(), new(), new(), new()}, // 4 default frames
        });
        AddAnimation(new("run") {
            TexturePath = "Images/Characters/miki/miki_0",
        });
        AddAnimation(new("skid") {
            TexturePath = "Images/Characters/miki/miki_2",
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
