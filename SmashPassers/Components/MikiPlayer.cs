namespace SmashPassers.Components;

public class MikiPlayer : PlayerBase
{
    private enum TextureIndexes
    {
        Idle,
        Run
    }

    public override void OnCreated()
    {
        base.OnCreated();

        AddAnimation(new(sprite, "idle") {
            TexturePath = "Images/Characters/miki/miki_0",
            Frames = {
                new(),
                new(),
                new(),
                new(),
            },
            Pivot = new(240, 332)
        });
        AddAnimation(new(sprite, "run") {
            TexturePath = "Images/Characters/miki/miki_0",
            Frames = {new()},
            Pivot = new(240, 332)
        });
        AddAnimation(new(sprite, "skid") {
            TexturePath = "Images/Characters/miki/miki_2",
            Frames = {new()},
            Pivot = new(240, 332)
        });

        AnimationId = "idle";
    }

    public override void Update()
    {
        base.Update();
    }
}
