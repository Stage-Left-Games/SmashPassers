namespace SmashPassers.Components;

public class MikiPlayer : PlayerBase
{
    public override void OnCreated()
    {
        base.OnCreated();

        AddAnimation(new("idle") {
            TexturePath = "Images/Characters/miki/miki_0",
            Frames = {new(), new(), new(), new()}, // 4 default frames
            Pivot = new(240, 332)
        });
        AddAnimation(new("run") {
            TexturePath = "Images/Characters/miki/miki_0",
            Pivot = new(240, 332)
        });
        AddAnimation(new("skid") {
            TexturePath = "Images/Characters/miki/miki_2",
            Pivot = new(240, 332)
        });
    }

    protected override void StateUpdate()
    {
        switch(State)
        {
            default: base.StateUpdate(); break;
        }
    }
}
