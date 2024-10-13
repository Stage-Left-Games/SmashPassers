namespace SmashPassers;

public static class SpeedConverter
{
    public static float KphToPpf(float value)
    {
        return value / 3.6f * 100f / 60;
    }

    public static float PpfToKph(float value)
    {
        return value * 60f / 100f * 3.6f;
    }
}
