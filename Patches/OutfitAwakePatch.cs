using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace UltimateOutfitSync
{
    [HarmonyPatch(typeof(Outfit), "Awake")]
    static class OutfitAwakePatch
    {
        static void Postfix(Outfit __instance)
        {
            if (UltimateOutfitMod.OutfitOverrides.TryGetValue(__instance.name, out int[] hash))
            {
                Debug.Log("OutfitAwakePatch found: " + __instance.name);
                if (!UltimateOutfitMod.HashOutfitOverrides.TryGetValue(UltimateOutfitMod.IntsToStringHash(hash), out Dictionary<string, Sprite> current))
                {
                    Debug.Log("OutfitAwakePatch HashOutfitOverrides notfound: " + __instance.name);

                    return;
                }
                Debug.Log("OutfitAwakePatch HashOutfitOverrides found: " + __instance.name);

                UltimateOutfitMod.ReplaceOutfit(__instance, current);
            }
        }
    }
}