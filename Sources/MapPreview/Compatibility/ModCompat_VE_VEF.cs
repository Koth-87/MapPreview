using System;
using System.Reflection;
using HarmonyLib;
using LunarFramework.Patching;
using RimWorld.Planet;
using Verse;

namespace MapPreview.Compatibility;

[HarmonyPatch]
internal class ModCompat_VE_VEF : ModCompat
{
    #if RW_1_6_OR_GREATER
    public override string TargetAssemblyName => "VEF";
    #else
    public override string TargetAssemblyName => "VFECore";
    #endif

    public override string DisplayName => "Vanilla Expanded Framework";

    #if RW_1_6_OR_GREATER

    private Type _tileMutatorExtensionType;
    private FieldInfo _mapSizeOverrideXField;
    private FieldInfo _mapSizeOverrideZField;
    private FieldInfo _mapSizeMultiplierField;

    protected override bool OnApply()
    {
        _tileMutatorExtensionType = AccessTools.TypeByName("VEF.Maps.TileMutatorExtension");

        if (_tileMutatorExtensionType != null)
        {
            _mapSizeOverrideXField = AccessTools.Field(_tileMutatorExtensionType, "mapSizeOverrideX");
            _mapSizeOverrideZField = AccessTools.Field(_tileMutatorExtensionType, "mapSizeOverrideZ");
            _mapSizeMultiplierField = AccessTools.Field(_tileMutatorExtensionType, "mapSizeMultiplier");

            if (_mapSizeOverrideXField != null && _mapSizeOverrideZField != null && _mapSizeMultiplierField != null)
            {
                MapSizeUtility.MapSizeTransforms.Add(TransformMapSize);
            }
        }

        return true;
    }

    /// <summary>
    /// Mirroring the logic in VanillaExpandedFramework_Game_InitNewGame_Patch
    /// </summary>
    private IntVec2 TransformMapSize(World world, PlanetTile tile, IntVec2 size)
    {
        try
        {
            var mutators = tile.Tile?.Mutators;
            if (mutators == null) return size;

            float multiplier = 1f;
            float sizeX = size.x;
            float sizeZ = size.z;

            foreach (var mutator in mutators)
            {
                var extension = mutator.modExtensions?.FirstOrDefault(ext => ext.GetType() == _tileMutatorExtensionType);
                if (extension != null)
                {
                    var mapSizeOverrideX = (int) _mapSizeOverrideXField.GetValue(extension);
                    var mapSizeOverrideZ = (int) _mapSizeOverrideZField.GetValue(extension);
                    var mapSizeMultiplier = (float) _mapSizeMultiplierField.GetValue(extension);

                    multiplier *= mapSizeMultiplier;

                    if (mapSizeOverrideX != -1) sizeX = mapSizeOverrideX;
                    if (mapSizeOverrideZ != -1) sizeZ = mapSizeOverrideZ;
                }
            }

            return new IntVec2((int)(sizeX * multiplier), (int)(sizeZ * multiplier));
        }
        catch (Exception e)
        {
            MapPreviewAPI.Logger.Warn("Error while checking for map size overrides from VFE", e);
            return size;
        }
    }

    #endif

    [HarmonyPrefix]
    #if RW_1_6_OR_GREATER
    [HarmonyPatch("VEF.Maps.VanillaExpandedFramework_TerrainGrid_SetTerrain", "Postfix")]
    #else
    [HarmonyPatch("VFECore._TerrainGrid", "Postfix")]
    #endif
    private static bool TerrainGrid_SetTerrain_Postfix()
    {
        return !MapPreviewGenerator.IsGeneratingOnCurrentThread;
    }
}
