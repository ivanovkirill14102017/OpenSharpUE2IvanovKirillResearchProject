using System.Numerics;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDTO;

public static class BspUvResolver
{
    public static Vector2 ComputeRawUv(Vector3 point, UnrModelSurface surface, UnrModelObject model, UnrPolysObject? brushPolys)
    {
        var basis = TryResolveBrushPolyBasis(surface, brushPolys) ?? ResolveModelSurfaceBasis(surface, model);
        var offset = point - basis.Base;
        return new Vector2(Vector3.Dot(offset, basis.TextureU), Vector3.Dot(offset, basis.TextureV));
    }

    public static UnrPolysObject? ResolveBrushPolys(UnrFile.UnrFile unr, UnrModelObject model)
    {
        if (model.PolysReference?.ExportIndex is not int polysExportIndex)
        {
            return null;
        }

        return unr.ExportObjects.FirstOrDefault(x => x.Export.Index == polysExportIndex)?.Object as UnrPolysObject;
    }

    private static UvBasis ResolveModelSurfaceBasis(UnrModelSurface surface, UnrModelObject model)
    {
        return new UvBasis(
            surface.BaseIndex >= 0 && surface.BaseIndex < model.Points.Length
                ? model.Points[surface.BaseIndex]
                : surface.BaseIndex >= 0 && surface.BaseIndex < model.Vectors.Length
                    ? model.Vectors[surface.BaseIndex]
                : Vector3.Zero,
            surface.TextureUIndex >= 0 && surface.TextureUIndex < model.Vectors.Length
                ? model.Vectors[surface.TextureUIndex]
                : Vector3.UnitX,
            surface.TextureVIndex >= 0 && surface.TextureVIndex < model.Vectors.Length
                ? model.Vectors[surface.TextureVIndex]
                : Vector3.UnitY);
    }

    private static UvBasis? TryResolveBrushPolyBasis(UnrModelSurface surface, UnrPolysObject? brushPolys)
    {
        if (brushPolys is null || surface.BrushPolyIndex < 0)
        {
            return null;
        }

        var poly = brushPolys.Polys.FirstOrDefault(x => x.IBrushPoly == surface.BrushPolyIndex || x.Index == surface.BrushPolyIndex)
                   ?? (surface.BrushPolyIndex < brushPolys.Polys.Length
                       ? brushPolys.Polys[surface.BrushPolyIndex]
                       : null);

        return poly is null
            ? null
            : new UvBasis(poly.Base, poly.TextureU, poly.TextureV);
    }

    private readonly record struct UvBasis(Vector3 Base, Vector3 TextureU, Vector3 TextureV);
}
