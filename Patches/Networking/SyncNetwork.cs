using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Xml;
using Unity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UltimateOutfitSync
{
    class SyncNetwork
    {
        public static readonly short magicNumberMsgRequestCustomSkin = (short)(NetMsgTypes.msgCount + 1 + 1125);
        public static readonly short magicNumberMsgSendMetadata = (short)(NetMsgTypes.msgCount + 2 + 1125);
        public static readonly short magicNumberMsgSendPNGChunck = (short)(NetMsgTypes.msgCount + 3 + 1125);
        public static readonly short magicNumberMsgRequestNextPNGChunck = (short)(NetMsgTypes.msgCount + 4 + 1125);

        public static int chunkSize = 50000;

        public static Dictionary<string, List<byte>> tempBytes = new Dictionary<string, List<byte>>();
        public static Dictionary<string, byte[]> tempMeta = new Dictionary<string, byte[]>();


        public static void RequestSkinLoad(LobbyPlayer sourceLobbyPlayer, LobbyPlayer targetLobbyPlayer)
        {
            var missing = targetLobbyPlayer.characterInstance?.GetComponentInChildren<MissingSkinIcon>();

            

            if (missing && missing.hash.Length != 0)
            {
                var req = new MsgRequestCustomSkin();

                req.sourceNetId = sourceLobbyPlayer.netid;
                req.targetNetId = targetLobbyPlayer.netid;
                req.hash = missing.hash;

                NetworkManager.singleton.client.Send(magicNumberMsgRequestCustomSkin, req);
            }
        }

        static void SendChunk(string pngPath, NetworkMessage msg, MessageBaseTarget req, int[] hash, int offset)
        {
            Debug.Log("Chuncking PNG path: " + pngPath);

            FileStream fileStream = new FileStream(pngPath, FileMode.Open);
            byte[] chunk = new byte[chunkSize];
            fileStream.Seek(offset, SeekOrigin.Begin);
            int csize;
            if ((csize = fileStream.Read(chunk, 0, chunk.Length)) != 0)
            {

                //send png chunk
                var metaReq = new MsgSendPNGChunk
                {
                    targetNetId = req.sourceNetId,
                    sourceNetId = req.targetNetId,
                    size = csize,
                    offset = offset,
                    hash = hash,
                    data = chunk
                };
                msg.conn.Send(magicNumberMsgSendPNGChunck, metaReq);
                msg.conn.FlushChannels();
            }

            fileStream.Close();

            Debug.Log("Chuncking PNG chunks: " + csize + " offset: " + offset + " path: " + pngPath);
        }

        static void forwardMessage(NetworkMessage msg)
        {
            MessageBaseTarget req = msg.ReadMessage<MessageBaseTarget>();
            Debug.Log("forwardMessage: " + req.targetNetId);


            GameObject go = ClientScene.FindLocalObject(new NetworkInstanceId(req.targetNetId));
            LobbyPlayer lp = go?.GetComponent<LobbyPlayer>();
            Debug.Log("forwardMessage: " + req.targetNetId + " lp: " + lp);

            if (NetworkServer.active && lp != null)
            {
                req.murks = msg.reader.ReadBytes(msg.reader.Length - (int) msg.reader.Position);
                lp.connectionToClient.Send(msg.msgType, req);
                return;
            }
        }

        static void onRequestCustomSkin(NetworkMessage msg)
        {
            MsgRequestCustomSkin req = msg.ReadMessage<MsgRequestCustomSkin>();
            Debug.Log("onRequestCustomSkin recv: " + UltimateOutfitMod.IntsToStringHash(req.hash));
            GameObject go = ClientScene.FindLocalObject(new NetworkInstanceId((uint)req.targetNetId));
            LobbyPlayer lp = go?.GetComponent<LobbyPlayer>();

            Debug.Log("onRequestCustomSkin lp: " + lp);


            if (UltimateOutfitMod.HashToPaths.TryGetValue(UltimateOutfitMod.IntsToStringHash(req.hash), out Tuple<string, string> paths))
            {
                string pngPath = paths.Item2;

                if (!pngPath.NullOrEmpty())
                {

                    SendChunk(pngPath, msg, req, req.hash, 0);
                    //TODO: Just chuck it online ... / Check online dir
                    /*
                    byte[] currentSceneThumbnailBytes = File.ReadAllBytes(pngPath);
                    GameSparksQuery gameSparksQuery = GameSparksManager.Instance.CreateQuery(true);
                    gameSparksQuery.UploadLevelThumbnail(UltimateOutfitMod.IntsToStringHash(req.hash), currentSceneThumbnailBytes);
                    gameSparksQuery.FinishListeners = (UnityEngine.Events.UnityAction<GameSparksQuery>)new UnityEngine.Events.UnityAction<GameSparksQuery>(delegate (GameSparksQuery tq)
                    {
                        Debug.Log("Thumbnail successfully uploaded: " + string.Join("\n", tq.ResultData.Keys));
                        //ThumbnailGenerator.ThumbnailLoaded();
                    });*/

                }
                else
                {
                    // can't go on without png
                    Debug.LogError("No png paths for: " + UltimateOutfitMod.IntsToStringHash(req.hash));
                    return;
                }

                string metaPath = paths.Item1;
                if (!metaPath.NullOrEmpty())
                {
                    Debug.Log("Sending metadata " + metaPath + " for: " + UltimateOutfitMod.IntsToStringHash(req.hash));

                    //send metadata.svg
                    var metaReq = new MsgSendCustomMetadata();
                    metaReq.targetNetId = req.sourceNetId;
                    metaReq.sourceNetId = req.targetNetId;
                    metaReq.hash = req.hash;
                    metaReq.metadata = File.ReadAllBytes(metaPath);
                    msg.conn.Send(magicNumberMsgSendMetadata, metaReq);
                }
                else
                {
                    Debug.Log("No metadata path for: " + UltimateOutfitMod.IntsToStringHash(req.hash));
                }
            }
        }

        static void onMetadata(NetworkMessage msg)
        {
            MsgSendCustomMetadata req = msg.ReadMessage<MsgSendCustomMetadata>();
            Debug.Log("MsgSendCustomMetadata recv: " + req.metadata.Length + " " + req.hash);
            tempMeta.Add(UltimateOutfitMod.IntsToStringHash(req.hash), req.metadata);
        }

        static void onPNGChunck(NetworkMessage msg)
        {
            MsgSendPNGChunk req = msg.ReadMessage<MsgSendPNGChunk>();
            Debug.Log("MsgSendPNGChunk recv: offset :" + req.offset + " size: " + req.size + " " + req.data.Length + " " + req.hash);

            string hash = UltimateOutfitMod.IntsToStringHash(req.hash);

            if (!tempBytes.TryGetValue(hash, out List<byte> bl))
            {
                bl = new List<byte>();
                tempBytes.Add(hash, bl);
            }

            Debug.Log("MsgSendPNGChunk recv: bl.COunt " + bl.Count + "offset :" + req.offset + " size: " + req.size + " " + req.data.Length + " " + req.hash);
            if (bl.Count != req.offset)
            {
                Debug.LogError("onPNGChunck: miss match in count ...");
            }

            bl.AddRange(req.data);

            
            SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(bl.ToArray());

            Debug.Log("dataBytes: " + bl.ToArray().Length + " bytes: " + BitConverter.ToString(hashBytes).Replace("-", ""));

            if (hash == BitConverter.ToString(hashBytes).Replace("-", ""))
            {
                Debug.Log("PNG transfer complete");
                if (tempMeta.TryGetValue(hash, out byte[] metaBytes))
                {
                    Debug.Log("PNG transfer complete, have metadata ... Loading skins");
                    MemoryStream memory = new MemoryStream(metaBytes, 0, metaBytes.Length);
                    XmlTextReader xmlReader = new XmlTextReader(memory);
                    xmlReader.XmlResolver = null;
                    xmlReader.DtdProcessing = 0;
                    UltimateOutfitMod.ReadMetadata(hash, xmlReader, bl.ToArray());

                    //Refresh outfits
                    foreach(MissingSkinIcon ic in Resources.FindObjectsOfTypeAll<MissingSkinIcon>()){
                        if(UltimateOutfitMod.IntsToStringHash(ic.hash) == hash)
                        {
                            Character chard = ic.gameObject.GetComponentInParent<Character>();
                            chard.SetOutfitsFromArray(NetworkOutfitPatch.addHashToOutfitArray(chard.GetOutfitsAsArray(), req.hash));
                        }
                    }

                    //Save files locally

                }
            }
            else
            {
                var metaReq = new MsgRequestNextPNGChunk
                {
                    targetNetId = req.sourceNetId,
                    sourceNetId = req.targetNetId,
                    offset = req.offset + req.size,
                    hash = req.hash
                };
                msg.conn.Send(magicNumberMsgRequestNextPNGChunck, metaReq);
            }
        }

        static void onNextPNGChunck(NetworkMessage msg)
        {
            MsgRequestNextPNGChunk req = msg.ReadMessage<MsgRequestNextPNGChunk>();
            Debug.Log("onNextPNGChunck recv: offset :" + req.offset + " " + req.hash);
            if (UltimateOutfitMod.HashToPaths.TryGetValue(UltimateOutfitMod.IntsToStringHash(req.hash), out Tuple<string, string> paths))
            {
                string pngPath = paths.Item2;

                if (!pngPath.NullOrEmpty())
                {
                    SendChunk(pngPath, msg, req, req.hash, req.offset);
                }
            }
        }

        [HarmonyPatch(typeof(LobbyManager), "Connect")]
        static class LobbyManagerConnectPatch
        {
            static void Postfix(LobbyManager __instance)
            {
                __instance.client.RegisterHandler(magicNumberMsgRequestCustomSkin, new NetworkMessageDelegate(onRequestCustomSkin));
                __instance.client.RegisterHandler(magicNumberMsgSendMetadata, new NetworkMessageDelegate(onMetadata));
                __instance.client.RegisterHandler(magicNumberMsgSendPNGChunck, new NetworkMessageDelegate(onPNGChunck));
                __instance.client.RegisterHandler(magicNumberMsgRequestNextPNGChunck, new NetworkMessageDelegate(onNextPNGChunck));
                if (NetworkServer.active)
                {
                    NetworkServer.RegisterHandler(magicNumberMsgRequestCustomSkin, new NetworkMessageDelegate(forwardMessage));
                    NetworkServer.RegisterHandler(magicNumberMsgSendMetadata, new NetworkMessageDelegate(forwardMessage));
                    NetworkServer.RegisterHandler(magicNumberMsgSendPNGChunck, new NetworkMessageDelegate(forwardMessage));
                    NetworkServer.RegisterHandler(magicNumberMsgRequestNextPNGChunck, new NetworkMessageDelegate(forwardMessage));
                }
            }
        }
    }
}