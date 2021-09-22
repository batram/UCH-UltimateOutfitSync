using HarmonyLib;
using UnityEngine;

namespace UltimateOutfit
{
    [HarmonyPatch(typeof(Character), "LateUpdate")]
    static class CharacterLateUpdatePatch
    {
        static void Postfix(Character __instance)
        {
            if (UltimateOutfitMod.CharacterOverrides.TryGetValue(__instance.CharacterSFXName, out Texture2D texture))
            {
                __instance.sprite.sprite = UltimateOutfitMod.SpriteFromCache(__instance.sprite.sprite, texture);
            }
        }
    }
}