﻿using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Xml;
using System.Security.Cryptography;
using System.Linq;
using System.Reflection;

[assembly: AssemblyVersion("0.0.1.0")]
[assembly: AssemblyInformationalVersion("0.0.1.0")]
[assembly: AssemblyProductAttribute("UltimateOutfitSync")]

namespace UltimateOutfitSync
{

    [BepInPlugin("notfood.plugins.UltimateOutfitSync", "UltimateOutfitSync", "0.0.1.0")]
    public class UltimateOutfitMod : BaseUnityPlugin
    {
        const string CHARACTERS_FOLDER = "outfits";
        const string TEXTURES_FOLDER = "textures";
        const string METADATA = "metadata.svg";
        const string IMAGE = "override.png";

        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        
        internal static readonly Dictionary<string, int[]> CharacterOverrides = new Dictionary<string, int[]>();
        internal static readonly Dictionary<string, int[]> OutfitOverrides = new Dictionary<string, int[]>();

        internal static readonly Dictionary<string, Texture2D> HashCharacterOverrides = new Dictionary<string, Texture2D>();
        internal static readonly Dictionary<string, Dictionary<string, Sprite>> HashOutfitOverrides = new Dictionary<string, Dictionary<string, Sprite>>();

        internal static readonly Dictionary<string, Tuple<string, string>> HashToPaths = new Dictionary<string, Tuple<string, string>>();

        void Awake()
        {
            new Harmony("notfood.UltimateOutfit").PatchAll();
        }

        void Start() {
            ReadFromMetadata();
            ReadFromImage();
        }

        #region Metadata reading
        void ReadFromMetadata()
        {
            string path = Path.Combine(Paths.BepInExRootPath, CHARACTERS_FOLDER);

            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

            foreach(string directory in dirs) 
            {
                string metadataFile = Path.Combine(directory, METADATA);
                string imageFile = Path.Combine(directory, IMAGE);

                if (File.Exists(metadataFile))
                {
                    try 
                    {
                        using (XmlTextReader xmlReader = new XmlTextReader(metadataFile))
                        {
                            xmlReader.XmlResolver = null;
                            xmlReader.DtdProcessing = 0;
                            ReadMetadata(directory, xmlReader);
                        }
                    } catch (Exception e)
                    {
                        Debug.LogError($"Failed to override {directory}: \n{e}");
                    }
                }
            }
        }

        public static void ReadMetadata(string directory, XmlReader xmlReader, byte[] textureBytes = null)
        {
            var coordinates = new List<SpriteCoordinates>();

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlReader.Name == "rect")
                    {
                        if (ReadCoordinatesFromMetadata(xmlReader, out SpriteCoordinates coords))
                        {
                            coordinates.Add(coords);
                        }
                    }
                    else if (xmlReader.Name == "image")
                    {
                        ReadTextureFromMetadata(xmlReader, directory, coordinates, textureBytes);

                        coordinates.Clear();
                    }
                }             
            }
        }

        struct SpriteCoordinates
        {
            public string id;
            public float width, height, x, y;
            public float offsetX, offsetY;
        }

        static bool ReadCoordinatesFromMetadata(XmlReader xmlReader, out SpriteCoordinates coords)
        {
            coords = new SpriteCoordinates() {
                id = xmlReader.GetAttribute("id")
            };
            return true
                && float.TryParse(xmlReader.GetAttribute("width"), out coords.width)
                && float.TryParse(xmlReader.GetAttribute("height"), out coords.height)
                && float.TryParse(xmlReader.GetAttribute("x"), out coords.x)
                && float.TryParse(xmlReader.GetAttribute("y"), out coords.y)
                && float.TryParse(xmlReader.GetAttribute("offset-x"), out coords.offsetX)
                && float.TryParse(xmlReader.GetAttribute("offset-y"), out coords.offsetY);
        }

        static void ReadTextureFromMetadata(XmlReader xmlReader, string directory, List<SpriteCoordinates> coordinates, byte[] textureBytes = null)
        {
            string animal = Path.GetFileName(directory);
            string textureName = animal;
            string textureFullPath = "";

            if (textureBytes == null)
            {
                string texturePath = xmlReader.GetAttribute("xlink:href");
                if (texturePath == null)
                {
                    Debug.LogError($"[UltimateOutfit] Error on {directory}: Texture \"{texturePath}\" is null");
                    return;
                }
                textureFullPath = Path.Combine(directory, texturePath);
                if (!File.Exists(textureFullPath))
                {
                    Debug.LogError($"[UltimateOutfit] Error on {directory}: Texture \"{texturePath}\" not found");
                    return;
                }
                textureName = Path.GetFileNameWithoutExtension(textureFullPath);
                textureBytes = File.ReadAllBytes(textureFullPath);
            }

            Dictionary<string, Sprite> current = new Dictionary<string, Sprite>();
            Texture2D texture = LoadTexture(textureName, textureBytes, out int[] hash);

            int.TryParse(xmlReader.GetAttribute("pixelsPerUnit"), out int pixelsPerUnit);

            foreach (var c in coordinates)
            {
                string key = c.id;
                Rect area = new Rect(c.x, texture.height - c.height - c.y, c.width, c.height);
                Vector2 pivot = new Vector2(c.offsetX, c.offsetY);

                RegisterSpriteOverride(current, key, texture, area, pivot, pixelsPerUnit);
            }


            Debug.Log("OutfitOverrides animal: " + animal);
            OutfitOverrides.Add(animal, hash);
            HashOutfitOverrides.Add(IntsToStringHash(hash), current);
            if (!textureFullPath.NullOrEmpty())
            {
                HashToPaths.Add(IntsToStringHash(hash), new Tuple<string, string>(Path.Combine(directory, METADATA), textureFullPath));
            }
        }
        #endregion

        #region Image reading
        void ReadFromImage()
        {
            string path = Path.Combine(Paths.BepInExRootPath, TEXTURES_FOLDER);

            string[] files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);

            foreach(string file in files)
            {
                try {
                    Texture2D texture = LoadTexture(Path.GetFileNameWithoutExtension(file), File.ReadAllBytes(file), out int[] hash);
                    RegisterTextureOverride(texture.name, texture, hash);
                }
                catch(Exception e)
                {
                    Debug.LogError($"Failed to override {file}: \n{e}");
                }
            }
        }
        #endregion

        public static string IntsToStringHash(int[] hash)
        {
            byte[] byteHash = new byte[hash.Length * sizeof(int)];
            Buffer.BlockCopy(hash, 0, byteHash, 0, byteHash.Length);
            return BitConverter.ToString(byteHash).Replace("-", "");
        }

        public static int[] BytesToInts(byte[] bytes)
        {
            // Create a packed int array.
            int[] ints = new int[bytes.Length / 4];
            for(int i = 0; i < ints.Length; i++)
            {
                ints[i] = BitConverter.ToInt32(bytes, i * 4);
            }
            // Pack the bytes into ints.
            return ints;
        }

        public static byte[] IntsToBytes(int[] ints)
        {
            // Create a packed int array.
            byte[] bytes = new byte[ints.Length * 4];

            Buffer.BlockCopy(ints, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static void ReplaceOutfit(Outfit skinOutfit, Dictionary<string, Sprite> dic)
        {

            for (int i = 0; i < skinOutfit.outputSprites.Length; i++)
            {
                var sprite = skinOutfit.outputSprites[i];

                if (sprite == null) continue;

                string index = sprite.name;

                var replacement = dic.Keys.FirstOrDefault(k => index.EndsWith(k, StringComparison.OrdinalIgnoreCase));

                if (replacement != null)
                {
                    skinOutfit.outputSprites[i] = dic[replacement];
                }
                else
                {
                    Debug.LogWarning($"Missing replacement for {skinOutfit.name} {index}");
                }
            }

            skinOutfit.hueShift = 0f;
            skinOutfit.saturationShift = 0f;
            skinOutfit.valueShift = 0f;
            skinOutfit.contrastShift = 1f;
            skinOutfit.colorize = false;
        }

        #region Sprite overriding
        static Texture2D LoadTexture(string name, byte[] data, out int[] hash)
        {
            // Generate the hash
            SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(data);
            //hash = BitConverter.ToString(hashBytes).Replace("-", "");
            Debug.Log("hashBytes: " + hashBytes.Length + " bytes: " + hashBytes);

            hash = BytesToInts(hashBytes);

            Debug.Log("hashInt: " + hash.Length + " bytes: " + hash);
            
            Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.LoadImage(data);
            texture.name = name;
            Debug.Log("texture.name: " + texture.name);

            return texture;
        }

        static void RegisterSpriteOverride(Dictionary<string, Sprite> current, string key, Texture2D texture, Rect area, Vector2 pivot, int pixelsPerUnit)
        {
            Sprite sprite = Sprite.Create(texture, area, pivot, pixelsPerUnit, 1, SpriteMeshType.FullRect);
            sprite.name = key;

            current.Add(sprite.name, sprite);
        }

        void RegisterTextureOverride(string fileName, Texture2D texture, int[] hash)
        {
            CharacterOverrides.Add(fileName, hash);
            HashCharacterOverrides.Add(IntsToStringHash(hash), texture);
            texture.name = fileName;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        internal static Sprite SpriteFromCache(Sprite sprite, Texture2D texture)
        {
            if (sprite == null)
            {
                return null;
            }

            if (spriteCache.TryGetValue(sprite.name, out Sprite replacement))
            {
                return replacement;
            }

            replacement = Sprite.Create(texture,
                sprite.rect,
                new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height),
                sprite.pixelsPerUnit,
                1,
                SpriteMeshType.Tight,
                sprite.border,
                false);

            Vector2[] vertices = sprite.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (vertices[i] * sprite.pixelsPerUnit) + sprite.pivot;
                vertices[i].x = Mathf.Clamp(vertices[i].x, 0f, sprite.rect.width);
                vertices[i].y = Mathf.Clamp(vertices[i].y, 0f, sprite.rect.height);
            }
            replacement.OverrideGeometry(vertices, sprite.triangles);
            replacement.name = sprite.name;
            spriteCache[sprite.name] = replacement;

            return replacement;
        }
        #endregion
    }
}
