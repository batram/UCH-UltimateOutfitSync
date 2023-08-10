using HarmonyLib;
using UnityEngine;

namespace UltimateOutfitSync
{
    [HarmonyPatch(typeof(Outfit), "ApplyHSVMaterialProperties")]
    static class OutfitApplyHSVPatch
    {
        static void Prefix(Outfit __instance)
        {
            if (UltimateOutfitMod.CharacterOverrides.TryGetValue(__instance.name, out int[] hash))
            {
                if (UltimateOutfitMod.HashCharacterOverrides.TryGetValue(UltimateOutfitMod.IntsToStringHash(hash), out Texture2D texture))
                {
                    for (int i = 0; i < __instance.outputSprites.Length; i++)
                    {
                        __instance.outputSprites[i] = UltimateOutfitMod.SpriteFromCache(__instance.outputSprites[i], texture);
                    }

                    __instance.hueShift = 0f;
                    __instance.saturationShift = 0f;
                    __instance.valueShift = 0f;
                    __instance.contrastShift = 1f;
                    __instance.colorize = false;
                }
            }
        }
    }
}