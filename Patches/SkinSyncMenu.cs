using HarmonyLib;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UltimateOutfitSync
{
    partial class SkinSyncMenu
    {
        public static string labelSkinText = "Skin";
        public static string labelLoadSkinText = "Load Skin";
        public static string labelHideSkinText = "Hide Skin";

        //hacks since i can't figure out TabletButtonEvent
        public static RectTransform SkinLoadConfirm;

        static void OpenSkinLoadConfirm(PickCursor _, LobbyPlayer lobbyPlayer)
        {
            Debug.Log("OpenSkinLoadConfirm = " + lobbyPlayer);

            var playersScreen = SkinLoadConfirm.parent.parent.GetComponent<TabletOnlinePlayersScreen>();
            playersScreen.subdialogController.TransitionLeftTo(SkinLoadConfirm, TabletScreen.TransitionSound.Modal, true);

            var button = SkinLoadConfirm.Find("Group(Clone)/ConfirmButton").GetComponent<TabletButton>();
            button.OnClick = new TabletButtonEvent();
            button.OnClick.AddListener((xd) => SkinSyncMenu.SkinLoadConfirmed(xd, lobbyPlayer));
        }

        static void SkinLoadConfirmed(PickCursor pc, LobbyPlayer lobbyPlayer)
        {
            Debug.Log("SkinLoadConfirmed = " + lobbyPlayer);

            // request skin from external user
            SyncNetwork.RequestSkinLoad(pc.LocalPlayer.AssociatedLobbyPlayer, lobbyPlayer);

            //maybe wait for skin to sync before moving on
            var playersScreen = SkinLoadConfirm.parent.parent.GetComponent<TabletOnlinePlayersScreen>();
            playersScreen.subdialogController.TransitionRightTo(playersScreen.mainDialog, TabletScreen.TransitionSound.None, true);
        }

        [HarmonyPatch(typeof(TabletOnlinePlayersScreen), "Start")]
        static class TabletOnlinePlayersScreenStartPatch
        {
            static void Postfix(TabletOnlinePlayersScreen __instance)
            {
                SkinLoadConfirm = GameObject.Instantiate(__instance.kickConfirmDialog, __instance.kickConfirmDialog.transform.parent);
                SkinLoadConfirm.name = "SkinLoadConfirm";

                var textLabel = SkinLoadConfirm.GetComponentInChildren<TabletTextLabel>();

                Debug.Log("SkinLoadConfirm label: " + textLabel);
                if (textLabel)
                {
                    textLabel.text = "Are you sure you want to load an external custom skin?";
                }


                var button = SkinLoadConfirm.Find("Group(Clone)/ConfirmButton").GetComponent<TabletButton>();
                button.OnClick = new TabletButtonEvent();

                Debug.Log("ConfirmButton: " + button);

                var buttonLabel = button.GetComponentInChildren<TabletTextLabel>();
                buttonLabel.Term = labelLoadSkinText;
                buttonLabel.text = labelLoadSkinText;
            }
        }


        [HarmonyPatch(typeof(TabletOnlinePlayer), "Initialize")]
        static class TabletOnlinePlayerInitializePatch
        {
            static void Postfix(TabletOnlinePlayer __instance, LobbyPlayer lobbyPlayer)
            {
                var go = __instance.voteKickButton.transform.parent.Find("SkinButton")?.gameObject;
                if (!go)
                {
                    go = GameObject.Instantiate(__instance.voteKickButton, __instance.voteKickButton.transform.parent).gameObject;
                    go.name = "SkinButton";
                    var textLabel = go.GetComponentInChildren<TabletTextLabel>();
                    textLabel.Term = labelSkinText;
                    textLabel.text = labelSkinText;
                }

                go.gameObject.SetActive(!lobbyPlayer.IsLocalPlayer);
                var button = go.GetComponent<TabletButton>();
                button.SetDisabled(true);
                button.OnClick = new TabletButtonEvent();
                button.OnClick.AddListener((x) => SkinSyncMenu.OpenSkinLoadConfirm(x, lobbyPlayer));
            }
        }

        [HarmonyPatch(typeof(TabletOnlinePlayer), "Update")]
        static class TabletOnlinePlayerUpdatePatch
        {
            static void Postfix(TabletOnlinePlayer __instance)
            {
                var skinButton = __instance.voteKickButton.transform.parent.Find("SkinButton");
                if (skinButton)
                {
                    var button = skinButton.GetComponent<TabletButton>();
                    //button.OnClick = null;

                    var textLabel = skinButton.GetComponentInChildren<TabletTextLabel>();
                    textLabel.Term = labelSkinText;
                    textLabel.text = labelSkinText;
                    skinButton.GetComponent<TabletButton>().SetDisabled(true);

                    Character chara = __instance.lobbyPlayer?.characterInstance;

                    if (chara)
                    {
                        MissingSkinIcon missingSkin = chara.GetComponentInChildren<MissingSkinIcon>();
                        if (missingSkin)
                        {
                            skinButton.gameObject.SetActive(missingSkin.missing);
                            skinButton.GetComponent<TabletButton>().SetDisabled(false);

                            if (missingSkin.missing)
                            {
                                textLabel.Term = labelLoadSkinText;
                                textLabel.text = labelLoadSkinText;
                            }
                            else
                            {
                                if (missingSkin.hash.Length != 0)
                                {
                                    textLabel.Term = labelHideSkinText;
                                    textLabel.text = labelHideSkinText;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}