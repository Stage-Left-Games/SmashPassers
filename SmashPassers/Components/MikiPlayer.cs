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
        AddTexture("Images/Characters/miki/miki_0", 4);
        AddTexture("Images/Characters/miki/miki_0", 1);
    }

    public override void Update()
    {
        base.Update();

        if(running)
            TextureIndex = (int)TextureIndexes.Run;
        else if(inputDir == 0 && velocity.X == 0)
        {
            TextureIndex = (int)TextureIndexes.Idle;
            Frame += 0.08f;
        }
    }
}
