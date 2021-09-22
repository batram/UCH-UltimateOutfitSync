using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace UltimateOutfit
{
    [HarmonyPatch(typeof(Outfit), "Awake")]
    static class OutfitAwakePatch
    {
        static readonly Dictionary<string, string> quirks = new Dictionary<string, string> {
            {"Arara_Chickendie", "Arara_Chicken_Die"},
            {"Arara_ChickenPortrait", "Arara_Chicken_Portrait"},
            {"Arara_ChickenCursor", "Arara_Chicken_Cursor"},
        };

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
                if (quirks.TryGetValue(index.Substring(0, index.Length-4), out string quirk))
                {
                    index = quirk + index.Substring(index.Length-4);
                }

                index = index.Substring(__instance.outfitString.Length + __instance.AssociatedCharacter.ToString().Length + 1);

                if (current.TryGetValue(index, out Sprite replacement))
                {
                    __instance.outputSprites[i] = replacement;
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