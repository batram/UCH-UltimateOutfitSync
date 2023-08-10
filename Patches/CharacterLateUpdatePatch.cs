using HarmonyLib;
using UnityEngine;

namespace UltimateOutfitSync
{
    [HarmonyPatch(typeof(Character), "LateUpdate")]
    static class CharacterLateUpdatePatch
    {
        static void Postfix(Character __instance)
        {
            if (UltimateOutfitMod.CharacterOverrides.TryGetValue(__instance.CharacterSFXName, out int[] hash))
            {
                if(UltimateOutfitMod.HashCharacterOverrides.TryGetValue(UltimateOutfitMod.IntsToStringHash(hash), out Texture2D texture))
                {
                    __instance.sprite.sprite = UltimateOutfitMod.SpriteFromCache(__instance.sprite.sprite, texture);
                }
            }
        }
    }
}