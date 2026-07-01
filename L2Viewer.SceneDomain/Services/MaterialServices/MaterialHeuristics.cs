using System;
using System.Collections.Generic;
using L2Viewer.UtxFile;

namespace L2Viewer.SceneDomain.Services.MaterialServices;

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
                if (flag.Name.Is("TwoSided"))
                {
                    twoSided = flag.Value;
                }

                if ((flag.Name.Is("AlphaTest") ||
                     flag.Name.Is("bMasked") ||
                     flag.Name.Is("Masked") ||
                     flag.Name.Is("bAlphaTexture")) && flag.Value)
                {
                    blendModeHint = MaterialBlendModeHint.Masked;
                }
            }

            foreach (var numeric in node.NumericParameters)
            {
                if (numeric.Name.Is("TexUPanSpeed"))
                {
                    uOffsetPerSecond = (float)numeric.Value;
                }
                else if (numeric.Name.Is("TexVPanSpeed"))
                {
                    vOffsetPerSecond = (float)numeric.Value;
                }
                else if (numeric.Name.Is("MinFrameRate"))
                {
                    minFrameRate = (float)numeric.Value;
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
                else if (numeric.Name.Is("MaxFrameRate"))
                {
                    maxFrameRate = (float)numeric.Value;
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
                else if (numeric.Name.Is("TotalFrameNum"))
                {
                    totalFrameCount = (int)numeric.Value;
                    hasAnimatedTextureFlipbookHint = true;
                    hasFrameAnimationClassHint = true;
                }
                else if (numeric.Name.Is("FrameBufferBlending") ||
                         numeric.Name.Is("OutputBlending") ||
                         numeric.Name.Is("BlendMode"))
                {
                    blendModeHint = ResolveBlendModeHintFromNumeric((int)numeric.Value, blendModeHint);
                }
            }

            foreach (var text in node.TextParameters)
            {
                if (text.Name.Is("FrameBufferBlending") ||
                    text.Name.Is("OutputBlending") ||
                    text.Name.Is("BlendMode"))
                {
                    blendModeHint = ResolveBlendModeHintFromText(text.Text, blendModeHint);
                }
            }

            foreach (var slot in node.ObjectSlots)
            {
                if (slot.SlotName.Is("Diffuse") ||
                    slot.SlotName.Is("Material") ||
                    slot.SlotName.Is("Material1") ||
                    slot.SlotName.Is("Material2") ||
                    slot.SlotName.Is("DefaultMaterial"))
                {
                    hasDiffuse = true;
                }
                else if (slot.SlotName.Is("Detail"))
                {
                    hasDetail = true;
                }
                else if (slot.SlotName.Is("Opacity"))
                {
                    hasOpacity = true;
                }
                else if (slot.SlotName.Is("Mask"))
                {
                    hasMask = true;
                }
                else if (slot.SlotName.Is("Specular"))
                {
                    hasSpecular = true;
                }
                else if (slot.SlotName.Is("SpecularityMask"))
                {
                    hasSpecularityMask = true;
                }
                else if (slot.SlotName.Is("SelfIllumination") ||
                         slot.SlotName.Is("SelfIlluminationMask"))
                {
                    hasSelfIllumination = true;
                }
                else if (slot.SlotName.Is("AnimNext"))
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
