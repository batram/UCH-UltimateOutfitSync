using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace UltimateOutfit
{
    [HarmonyPatch(typeof(Outfit), "Awake")]
    static class OutfitAwakePatch
    {
        static void Postfix(Outfit __instance)
        {
            if (!UltimateOutfitMod.OutfitOverrides.TryGetValue(__instance.name, out Dictionary<string, Sprite> current))
            {
                return;
            }

            for(int i = 0; i < __instance.outputSprites.Length; i++)
            {
                var sprite = __instance.outputSprites[i];

                if (sprite == null) continue;

                string index = sprite.name;

                var replacement = current.Keys.FirstOrDefault(k => index.EndsWith(k, StringComparison.OrdinalIgnoreCase));

                if (replacement != null)
                {
                    __instance.outputSprites[i] = current[replacement];
                }
                else
                {
                    Debug.LogWarning($"Missing replacement for {__instance.name} {index}");
                }
            }

            __instance.hueShift = 0f;
            __instance.saturationShift = 0f;
            __instance.valueShift = 0f;
            __instance.contrastShift = 1f;
            __instance.colorize = false;
        }
    }
}