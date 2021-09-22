using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Xml;

namespace UltimateOutfit
{

    [BepInPlugin("notfood.plugins.UltimateOutfit", "UltimateOutfit", "1.0.0.0")]
    public class UltimateOutfitMod : BaseUnityPlugin
    {
        const string CHARACTERS_FOLDER = "outfits";
        const string TEXTURES_FOLDER = "textures";
        const string METADATA = "metadata.svg";
        const string IMAGE = "override.png";

        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        
        internal static readonly Dictionary<string, Texture2D> CharacterOverrides = new Dictionary<string, Texture2D>();
        internal static readonly Dictionary<string, Dictionary<string, Sprite>> OutfitOverrides = new Dictionary<string, Dictionary<string, Sprite>>();

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
                            ReadMetadata(directory, xmlReader);
                        }
                    } catch (Exception e)
                    {
                        Debug.LogError($"Failed to override {directory}: \n{e}");
                    }
                }
            }
        }

        void ReadMetadata(string directory, XmlReader xmlReader)
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
                        ReadTextureFromMetadata(xmlReader, directory, coordinates);

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

        bool ReadCoordinatesFromMetadata(XmlReader xmlReader, out SpriteCoordinates coords)
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

        void ReadTextureFromMetadata(XmlReader xmlReader, string directory, List<SpriteCoordinates> coordinates)
        {
            string animal = Path.GetFileName(directory);

            int.TryParse(xmlReader.GetAttribute("pixelsPerUnit"), out int pixelsPerUnit);

            string texturePath = xmlReader.GetAttribute("xlink:href");
            if (texturePath == null)
            {
                Debug.LogError($"[UltimateOutfit] Error on {directory}: Texture \"{texturePath}\" is null");
                return;
            }
            string textureFullPath = Path.Combine(directory, texturePath);
            if (!File.Exists(textureFullPath))
            {
                Debug.LogError($"[UltimateOutfit] Error on {directory}: Texture \"{texturePath}\" not found");
                return;
            }

            Dictionary<string, Sprite> current = new Dictionary<string, Sprite>();
            Texture2D texture = LoadTexture(textureFullPath);
            foreach(var c in coordinates)
            {
                string key = c.id;
                Rect area = new Rect(c.x, texture.height - c.height - c.y, c.width, c.height);
                Vector2 pivot = new Vector2(c.offsetX, c.offsetY);

                RegisterSpriteOverride(current, key, texture, area, pivot, pixelsPerUnit);
            }
            OutfitOverrides.Add(animal, current);
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
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    Texture2D texture = LoadTexture(file);
                    RegisterTextureOverride(fileName, texture);
                }
                catch(Exception e)
                {
                    Debug.LogError($"Failed to override {file}: \n{e}");
                }
            }
        }
        #endregion

        #region Sprite overriding
        Texture2D LoadTexture(string path)
        {
            byte[] data = File.ReadAllBytes(path);

            Texture2D texture = new Texture2D(1,1, TextureFormat.ARGB32, false);
            texture.LoadImage(data);
            texture.name = Path.GetFileNameWithoutExtension(path);

            return texture;
        }

        void RegisterSpriteOverride(Dictionary<string, Sprite> current, string key, Texture2D texture, Rect area, Vector2 pivot, int pixelsPerUnit)
        {
            Sprite sprite = Sprite.Create(texture, area, pivot, pixelsPerUnit, 1, SpriteMeshType.FullRect);
            sprite.name = key;

            current.Add(sprite.name, sprite);
        }

        void RegisterTextureOverride(string fileName, Texture2D texture)
        {
            CharacterOverrides.Add(fileName, texture);
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
                sprite.pivot/sprite.pixelsPerUnit,
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