using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace UltimateOutfitSync
{
    static class NetworkOutfitPatch
    {
        public const int OutfitArrayIndex = 7;
        public const int OutfitArrayLength = 8;

        [HarmonyPatch(typeof(Character), "GetOutfitsAsArray")]
        static class GetOutfitsAsArrayCtorPatch
        {
            static void Postfix(Character __instance, ref int[] __result)
            {

                string name = __instance.CharacterSFXName;
                int skinNum = __result[(int)Outfit.OutfitType.Skin];
                if (skinNum != -1)
                {
                    Outfit[] componentsInChildren = __instance.GetComponentsInChildren<Outfit>();
                    if (componentsInChildren[skinNum])
                    {
                        name = componentsInChildren[skinNum].name;
                    }
                }

                //__instance.associatedGamePlayer.IsLocalPlayer
                if (__instance.LocalPlayer != null && (UltimateOutfitMod.CharacterOverrides.TryGetValue(name, out int[] hash) || UltimateOutfitMod.OutfitOverrides.TryGetValue(name, out hash)))
                {
                    if (__result.Length < OutfitArrayIndex + OutfitArrayLength)
                    {
                        Array.Resize<int>(ref __result, OutfitArrayIndex + OutfitArrayLength);
                    }
                    Array.Copy(hash, 0, __result, OutfitArrayIndex, OutfitArrayLength);
                    Debug.Log("GetOutfitsAsArray: Array foxed");

                }
                Debug.Log("GetOutfitsAsArray: " + name + "  a: " + __result.Length + "  a[3]: " + __result[3] + " IsLocalPlayer: " + (__instance.LocalPlayer != null));
                Debug.Log("GetOutfitsAsArray: " + name + "  CharacterOverrides: " + UltimateOutfitMod.CharacterOverrides.TryGetValue(name, out int[] _));
                Debug.Log("GetOutfitsAsArray: " + name + "  OutfitOverrides: " + UltimateOutfitMod.OutfitOverrides.TryGetValue(name, out _));
            }
        }

        [HarmonyPatch(typeof(Character), "SetOutfitsFromArray", new Type[] { typeof(int[]) })]
        static class SetOutfitsFromArrayCtorPatch
        {
            static void Postfix(Character __instance, ref int[] outfitsArray)
            {
                Debug.Log("SetOutfitsFromArray length: " + outfitsArray.Length);

                string name = MissingSkinIcon.ObjectName;
                MissingSkinIcon missingSkin = __instance.nameTag.UCHNetIcon.transform.parent.Find(name)?.gameObject.GetComponent<MissingSkinIcon>();

                if (missingSkin)
                {
                    //hide SkinMissing icon
                    missingSkin.setIcon(false);
                }

                if (outfitsArray.Length >= OutfitArrayIndex + OutfitArrayLength)
                {
                    int[] hash = new int[OutfitArrayLength];
                    Array.Copy(outfitsArray, OutfitArrayIndex, hash, 0, OutfitArrayLength);

                    Debug.Log("SetOutfitsFromArray hash: " + UltimateOutfitMod.IntsToStringHash(hash));

                    if (UltimateOutfitMod.HashCharacterOverrides.TryGetValue(UltimateOutfitMod.IntsToStringHash(hash), out Texture2D texture))
                    {
                        Debug.Log("SetOutfitsFromArray character overwrite: " + texture.name);
                        return;
                    }

                    if (UltimateOutfitMod.HashOutfitOverrides.TryGetValue(UltimateOutfitMod.IntsToStringHash(hash), out Dictionary<string, Sprite> dic))
                    {
                        Debug.Log("SetOutfitsFromArray outfit overwrite: " + dic.Keys.First());

                        int skinNum = outfitsArray[(int)Outfit.OutfitType.Skin];
                        if (skinNum != -1)
                        {
                            Outfit[] componentsInChildren = __instance.GetComponentsInChildren<Outfit>();
                            if (componentsInChildren[skinNum])
                            {
                                Outfit skinOutfit = componentsInChildren[skinNum];
                                UltimateOutfitMod.ReplaceOutfit(skinOutfit, dic);
                            }
                        }

                        return;
                    }

                    // missing skin for hash
                    if (!missingSkin)
                    {
                        missingSkin = MissingSkinIcon.createMissingSkinIcon(__instance.nameTag, hash);
                        missingSkin.setIcon(true);
                        missingSkin.showWarnMessage(__instance.LocalizedName);
                    }
                    else
                    {
                        missingSkin.setIcon(true);

                        if (UltimateOutfitMod.IntsToStringHash(missingSkin.hash) != UltimateOutfitMod.IntsToStringHash(hash))
                        {
                            missingSkin.hash = hash;
                            missingSkin.showWarnMessage(__instance.LocalizedName);
                        }
                    }
                }
            }
        }

    }
}