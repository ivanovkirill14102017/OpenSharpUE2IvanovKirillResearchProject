using System;
using System.Collections.Generic;
using L2Viewer.UtxFile;

namespace L2Viewer.SceneDomain.Services;

public enum MaterialBlendModeHint
{
    Unknown = 0,
    Opaque,
    Masked,
    Translucent,
    Additive,
    Modulated
}

public sealed record MaterialKnownTraits(
    bool? TwoSided,
    bool HasDiffuseInput,
    bool HasDetailInput,
    bool HasOpacityInput,
    bool HasMaskInput,
    bool HasSpecularInput,
    bool HasSpecularityMaskInput,
    bool HasSelfIlluminationInput,
    bool HasPannerClassHint,
    bool HasFrameAnimationClassHint,
    bool HasAnimatedTextureFlipbookHint,
    int? TotalFrameCount,
    float? MinFrameRate,
    float? MaxFrameRate,
    float? UOffsetPerSecond,
    float? VOffsetPerSecond,
    MaterialBlendModeHint BlendModeHint);

public static class MaterialHeuristics
{
    public static MaterialKnownTraits GetKnownTraits(this ResolvedMaterialGraph graph)
    {
        return DeriveKnownMaterialTraits(graph.Nodes);
    }

    private static MaterialKnownTraits DeriveKnownMaterialTraits(IReadOnlyList<MaterialGraphNode> nodes)
    {
        bool? twoSided = null;
        var hasDiffuse = false;
        var hasDetail = false;
        var hasOpacity = false;
        var hasMask = false;
        var hasSpecular = false;
        var hasSpecularityMask = false;
        var hasSelfIllumination = false;
        var hasPannerClassHint = false;
        var hasFrameAnimationClassHint = false;
        var hasAnimatedTextureFlipbookHint = false;
        int? totalFrameCount = null;
        float? minFrameRate = null;
        float? maxFrameRate = null;
        float? uOffsetPerSecond = null;
        float? vOffsetPerSecond = null;
        var blendModeHint = MaterialBlendModeHint.Unknown;

        foreach (var node in nodes)
        {
            if (node.ClassName.Contains("Panner", StringComparison.OrdinalIgnoreCase))
            {
                hasPannerClassHint = true;
            }

            if (node.ClassName.Contains("Sequence", StringComparison.OrdinalIgnoreCase) ||
                node.ClassName.Contains("Oscillator", StringComparison.OrdinalIgnoreCase))
            {
                hasFrameAnimationClassHint = true;
            }

            foreach (var flag in node.BooleanParameters)
            {
                if (string.Equals(flag.Name, "TwoSided", StringComparison.OrdinalIgnoreCase))
                {
                    twoSided = flag.Value;
                }

                if ((string.Equals(flag.Name, "AlphaTest", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(flag.Name, "bMasked", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(flag.Name, "Masked", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(flag.Name, "bAlphaTexture", StringComparison.OrdinalIgnoreCase)) && flag.Value)
                {
                    blendModeHint = MaterialBlendModeHint.Masked;
                }
            }

            foreach (var numeric in node.NumericParameters)
            {
                if (string.Equals(numeric.Name, "TexUPanSpeed", StringComparison.OrdinalIgnoreCase))
                {
                    uOffsetPerSecond = (float)numeric.Value;
                }
                else if (string.Equals(numeric.Name, "TexVPanSpeed", StringComparison.OrdinalIgnoreCase))
                {
                    vOffsetPerSecond = (float)numeric.Value;
                }
                else if (string.Equals(numeric.Name, "MinFrameRate", StringComparison.OrdinalIgnoreCase))
                {
                    minFrameRate = (float)numeric.Value;
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
                else if (string.Equals(numeric.Name, "MaxFrameRate", StringComparison.OrdinalIgnoreCase))
                {
                    maxFrameRate = (float)numeric.Value;
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
                else if (string.Equals(numeric.Name, "TotalFrameNum", StringComparison.OrdinalIgnoreCase))
                {
                    totalFrameCount = (int)numeric.Value;
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
                else if (string.Equals(numeric.Name, "FrameBufferBlending", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(numeric.Name, "OutputBlending", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(numeric.Name, "BlendMode", StringComparison.OrdinalIgnoreCase))
                {
                    blendModeHint = ResolveBlendModeHintFromNumeric((int)numeric.Value, blendModeHint);
                }
            }

            foreach (var text in node.TextParameters)
            {
                if (string.Equals(text.Name, "FrameBufferBlending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.Name, "OutputBlending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(text.Name, "BlendMode", StringComparison.OrdinalIgnoreCase))
                {
                    blendModeHint = ResolveBlendModeHintFromText(text.Text, blendModeHint);
                }
            }

            foreach (var slot in node.ObjectSlots)
            {
                if (string.Equals(slot.SlotName, "Diffuse", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(slot.SlotName, "Material", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(slot.SlotName, "Material1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(slot.SlotName, "Material2", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(slot.SlotName, "DefaultMaterial", StringComparison.OrdinalIgnoreCase))
                {
                    hasDiffuse = true;
                }
                else if (string.Equals(slot.SlotName, "Detail", StringComparison.OrdinalIgnoreCase))
                {
                    hasDetail = true;
                }
                else if (string.Equals(slot.SlotName, "Opacity", StringComparison.OrdinalIgnoreCase))
                {
                    hasOpacity = true;
                }
                else if (string.Equals(slot.SlotName, "Mask", StringComparison.OrdinalIgnoreCase))
                {
                    hasMask = true;
                }
                else if (string.Equals(slot.SlotName, "Specular", StringComparison.OrdinalIgnoreCase))
                {
                    hasSpecular = true;
                }
                else if (string.Equals(slot.SlotName, "SpecularityMask", StringComparison.OrdinalIgnoreCase))
                {
                    hasSpecularityMask = true;
                }
                else if (string.Equals(slot.SlotName, "SelfIllumination", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(slot.SlotName, "SelfIlluminationMask", StringComparison.OrdinalIgnoreCase))
                {
                    hasSelfIllumination = true;
                }
                else if (string.Equals(slot.SlotName, "AnimNext", StringComparison.OrdinalIgnoreCase))
                {
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
            }
        }

        if (blendModeHint == MaterialBlendModeHint.Unknown)
        {
            if (hasMask)
            {
                blendModeHint = MaterialBlendModeHint.Masked;
            }
            else if (hasOpacity)
            {
                blendModeHint = MaterialBlendModeHint.Translucent;
            }
        }

        return new MaterialKnownTraits(
            twoSided,
            hasDiffuse,
            hasDetail,
            hasOpacity,
            hasMask,
            hasSpecular,
            hasSpecularityMask,
            hasSelfIllumination,
            hasPannerClassHint,
            hasFrameAnimationClassHint,
            hasAnimatedTextureFlipbookHint,
            totalFrameCount,
            minFrameRate,
            maxFrameRate,
            uOffsetPerSecond,
            vOffsetPerSecond,
            blendModeHint);
    }

    private static MaterialBlendModeHint ResolveBlendModeHintFromNumeric(int value, MaterialBlendModeHint current)
    {
        var candidate = value switch
        {
            1 => MaterialBlendModeHint.Modulated,
            2 => MaterialBlendModeHint.Translucent,
            3 => MaterialBlendModeHint.Masked,
            4 => MaterialBlendModeHint.Translucent,
            5 => MaterialBlendModeHint.Modulated,
            6 => MaterialBlendModeHint.Additive,
            _ => MaterialBlendModeHint.Unknown
        };

        return PromoteBlendMode(current, candidate);
    }

    private static MaterialBlendModeHint ResolveBlendModeHintFromText(string text, MaterialBlendModeHint current)
    {
        if (text.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Brighten", StringComparison.OrdinalIgnoreCase))
        {
            return PromoteBlendMode(current, MaterialBlendModeHint.Additive);
        }

        if (text.Contains("Modulat", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Darken", StringComparison.OrdinalIgnoreCase))
        {
            return PromoteBlendMode(current, MaterialBlendModeHint.Modulated);
        }

        if (text.Contains("Mask", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("AlphaTest", StringComparison.OrdinalIgnoreCase))
        {
            return PromoteBlendMode(current, MaterialBlendModeHint.Masked);
        }

        if (text.Contains("Alpha", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Transluc", StringComparison.OrdinalIgnoreCase))
        {
            return PromoteBlendMode(current, MaterialBlendModeHint.Translucent);
        }

        return current;
    }

    private static MaterialBlendModeHint PromoteBlendMode(MaterialBlendModeHint current, MaterialBlendModeHint candidate)
    {
        if (candidate == MaterialBlendModeHint.Unknown)
        {
            return current;
        }

        if (current == MaterialBlendModeHint.Unknown)
        {
            return candidate;
        }

        if (current == MaterialBlendModeHint.Masked)
        {
            return current;
        }

        return candidate;
    }
}
