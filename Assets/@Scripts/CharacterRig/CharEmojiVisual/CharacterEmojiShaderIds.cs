using UnityEngine;

public static class CharacterEmojiShaderIds
{
    public static readonly int Reveal =
        Shader.PropertyToID("_Reveal");

    public static readonly int RevealSoftness =
        Shader.PropertyToID("_RevealSoftness");

    public static readonly int RevealDirection =
        Shader.PropertyToID("_RevealDirection");

    public static readonly int EdgeRimAmount =
        Shader.PropertyToID("_EdgeRimAmount");

    public static readonly int EdgeRimWidth =
        Shader.PropertyToID("_EdgeRimWidth");

    public static readonly int EdgeRimColor =
        Shader.PropertyToID("_EdgeRimColor");

    public static readonly int GlowAmount =
        Shader.PropertyToID("_GlowAmount");

    public static readonly int GlowColor =
        Shader.PropertyToID("_GlowColor");
}