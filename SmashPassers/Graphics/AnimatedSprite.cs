using System.Collections.Generic;
using System.Text.Json.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Jelly;
using Jelly.Graphics;
using System;
using System.Collections;

namespace SmashPassers.Graphics;

public class AnimatedSprite
{
    private string animationIndex;

    public Dictionary<string, Animation> Animations { get; } = [];

    public Animation? CurrentAnimation => Animations.TryGetValue(animationIndex ?? "", out Animation anim) ? anim : null;
    public string? CurrentAnimationId => animationIndex;

    public bool UseUnscaledTime { get; set; }

    public bool Visible { get; set; } = true;

    public Animation this[string index] {
        get => Animations[index];
        set => Animations[index] = value;
    }

    public delegate void AnimationEvent(Animation animation);
    public delegate void AnimationEvent<T>(Animation animation, T obj);

    public event AnimationEvent<int> FrameChanged;
    public event AnimationEvent Looped;
    public event AnimationEvent Finished;

    public void Update()
    {
        if(Animations.TryGetValue(animationIndex ?? "", out Animation anim))
        {
            if(anim.IsPlaying)
            {
                anim.Update(UseUnscaledTime);
            }
        }
    }

    public void Draw(Vector2 position)
    {
        if(!Visible) return;

        CurrentAnimation?.Draw(position);
    }

    public void SetAnimation(string id)
    {
        if(CurrentAnimation?.Id == id) return;

        CurrentAnimation?.Stop();
        animationIndex = id;
        CurrentAnimation?.Play();
    }

    public class Animation
    {
        private int frameIndex;
        private float frameDelay;
        private bool hasPlayed;
        private readonly string id;

        public string Id => id;

        [JsonIgnore]
        public AnimatedSprite Sprite { get; set; }

        [JsonIgnore]
        public bool IsPlaying { get; private set; }

        public bool Loop { get; set; } = true;

        public List<AnimationFrame> Frames { get; } = [];

        public SpriteEffects SpriteEffects { get; set; } = SpriteEffects.None;

        [JsonIgnore]
        public AnimationFrame CurrentFrame => Frames[frameIndex];

        public float CurrentFrameNumber {
            get {
                float duration = 0;
                foreach(var item in Frames)
                {
                    duration += item.Duration;
                }
                duration -= (CurrentFrame.DurationInSeconds - frameDelay) / CurrentFrame.DurationInSeconds;
                return duration;
            }
            set {
                float duration = 0;
                for(int i = 0; i < Frames.Count; i++)
                {
                    var item = Frames[i];

                    if(duration + item.Duration > value)
                    {
                        frameIndex = i;
                        frameDelay = CurrentFrame.DurationInSeconds - (value - duration) * CurrentFrame.DurationInSeconds;

                        Sprite.FrameChanged?.Invoke(this, frameIndex);
                        break;
                    }

                    duration += item.Duration;
                }
            }
        }

        public float PlaybackSpeed { get; set; } = 1;

        // Fallbacks
        public string TexturePath { get; set; }
        public Rectangle? SourceRectangle { get; set; }
        public Vector2 Scale { get; set; } = Vector2.One;
        public Color Color { get; set; } = Color.White;
        public float Alpha { get; set; } = 1f;
        public float Rotation { get; set; } = 0f;
        public Vector2 Pivot { get; set; } = Vector2.Zero;

        // use these to get per-frame (with fallback) values
        public Vector2 ActiveOffset => CurrentFrame.Offset ?? Vector2.Zero;
        public string ActiveTexturePath => CurrentFrame.TexturePath ?? (TexturePath + "-" + frameIndex);
        public Rectangle? ActiveSourceRectangle => CurrentFrame.SourceRectangle ?? SourceRectangle;
        public Vector2 ActiveScale => CurrentFrame.Scale ?? Scale;
        public Color ActiveColor => CurrentFrame.Color ?? Color;
        public float ActiveAlpha => CurrentFrame.Alpha ?? Alpha;
        public float ActiveRotation => CurrentFrame.Rotation ?? Rotation;
        public Vector2 ActivePivot => CurrentFrame.Pivot ?? Pivot;

        public Animation(AnimatedSprite sprite, string id)
        {
            this.id = id;
            Sprite = sprite;
        }

        public Animation(string id)
        {
            this.id = id;
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
            frameIndex = 0;
            frameDelay = 0;
            hasPlayed = false;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Update(bool useUnscaledTime)
        {
            int ind = frameIndex;

            if(Frames.Count == 0)
            {
                // Stop();
                // Sprite.Finished?.Invoke(this);
                // return;
                Frames.Add(new());
            }

            frameDelay = MathHelper.Max(0, frameDelay - (useUnscaledTime ? Time.UnscaledDeltaTime : Time.DeltaTime) * PlaybackSpeed);
            if(frameDelay == 0)
            {
                if(hasPlayed)
                    frameIndex++;
                hasPlayed = true;

                if(frameIndex >= Frames.Count)
                {
                    frameIndex = 0;
                    if(!Loop)
                    {
                        Stop();
                        Sprite.Finished?.Invoke(this);
                    }
                    else
                    {
                        Sprite.Looped?.Invoke(this);
                    }
                }

                if(frameIndex != ind)
                    Sprite.FrameChanged?.Invoke(this, frameIndex);

                frameDelay = Frames[frameIndex].DurationInSeconds;
            }
        }

        public void Draw(Vector2 position)
        {
            if(Frames.Count == 0)
            {
                Renderer.SpriteBatch.Draw(ContentLoader.LoadContent<Texture2D>(TexturePath + "-0"), position, SourceRectangle, Color * Alpha, Rotation, Pivot, Scale, SpriteEffects, 0f);
                return;
            }

            Renderer.SpriteBatch.Draw(
                ContentLoader.LoadContent<Texture2D>(ActiveTexturePath),
                position + ActiveOffset,
                ActiveSourceRectangle,
                ActiveColor * ActiveAlpha,
                ActiveRotation,
                ActivePivot,
                ActiveScale,
                SpriteEffects,
                0f
            );
        }

        public struct AnimationFrame()
        {
            [JsonIgnore]
            public readonly float DurationInSeconds => Duration / 24f;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int Duration { get; set; } = 1;

            public Vector2? Offset { get; set; }
            public string? TexturePath { get; set; }
            public Rectangle? SourceRectangle { get; set; }
            public Vector2? Scale { get; set; }
            public Color? Color { get; set; }
            public float? Alpha { get; set; }
            public float? Rotation { get; set; }
            public Vector2? Pivot { get; set; }
        }
    }
}
