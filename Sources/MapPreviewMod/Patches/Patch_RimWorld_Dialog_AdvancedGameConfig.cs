using HarmonyLib;
using LunarFramework.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace MapPreview.Patches;

[PatchGroup("Active")]
[HarmonyPatch(typeof(Dialog_AdvancedGameConfig))]
internal class Patch_RimWorld_Dialog_AdvancedGameConfig
{
    [HarmonyPostfix]
    [HarmonyPatch("DoWindowContents")]
    private static void DoWindowContents()
    {
        if (!GUI.changed) return;

        var world = Find.World;
        var currentPreviewMap = MapPreviewWindow.Instance?.CurrentPreviewMap;

        if (world != null && currentPreviewMap != null)
        {
            #if RW_1_6_OR_GREATER
            var newMapSize = MapSizeUtility.DetermineMapSize(world, currentPreviewMap.Tile, world.worldObjects.MapParentAt(currentPreviewMap.Tile));
            #else
            var newMapSize = MapSizeUtility.DetermineMapSize(world, world.worldObjects.MapParentAt(currentPreviewMap.Tile));
            #endif

            if (currentPreviewMap.Size != new IntVec3(newMapSize.x, currentPreviewMap.Size.y, newMapSize.z))
            {
                WorldInterfaceManager.RefreshPreview();
            }
        }
    }
}
