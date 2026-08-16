using System.IO;
using System;
using Utility;
using UnityEngine;

namespace GPG214.Project.One.Streaming
{
    public class FileLoader : Singleton<FileLoader>
    {

        // INFO: Read all bytes of an image
        public byte[] ReadFile(string fileLocation)
        {
            if (!Directory.Exists(Application.streamingAssetsPath)) Directory.CreateDirectory(Application.streamingAssetsPath);
            string filePath = Path.Combine(Application.streamingAssetsPath, fileLocation);

            // GUARD: Prevent Nulls
            if (!File.Exists(filePath)) { Debug.LogWarning($"File doesn't exist! ({filePath})"); return null; }

            byte[] imageBytes = File.ReadAllBytes(filePath);
            return imageBytes;

        }

        // INFO: Convert image into Unity
        public T LoadFile<T>(string fileLocation)
        {
            byte[] imageBytes = ReadFile(fileLocation);
            if (imageBytes == null) return GetErrorTexture<T>();

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);

            switch (typeof(T))
            {
                // INFO: Sprite
                case Type t when t == typeof(Sprite):
                    return (T)(object)Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

                // INFO: Texture/2D
                case Type t when t == typeof(Texture2D) || t == typeof(Texture):
                    return (T)(object)texture;

                default:
                    Debug.LogWarning($"Unsupported type: {typeof(T)}"); return GetErrorTexture<T>();
            }
        }

        // INFO: Error texture if file not found 
        // TODO: Change it to a baked image?
        private T GetErrorTexture<T>()
        {
            Texture2D magenta = new Texture2D(1, 1);
            magenta.SetPixel(0, 0, Color.magenta);
            magenta.Apply();

            switch (typeof(T))
            {
                case Type t when t == typeof(Sprite):
                    return (T)(object)Sprite.Create(magenta, new Rect(0, 0, 1, 1), Vector2.zero);
                case Type t when t == typeof(Texture2D) || t == typeof(Texture):
                    return (T)(object)magenta;


            }


            return default;
        }
    }
}