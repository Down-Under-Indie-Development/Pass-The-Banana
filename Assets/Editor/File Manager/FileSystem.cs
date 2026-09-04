using UnityEngine;
using System.IO;
using UnityEditor;


namespace Utility
{
    public class FileSystem
    {
        [MenuItem("Assets/File System/Open Persistent Data Path")]
        private static void OpenPersistentPath()
        {
            string _persistentPath = Application.persistentDataPath;
            Application.OpenURL(_persistentPath);

        }

        [MenuItem("Assets/File System/Open Streaming Assets")]
        private static void OpenStreamingAssets()
        {
            string _streamingAssets = Application.streamingAssetsPath;

            // INFO: Create file if its missing (Or first time opening)
            string filePath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

            Application.OpenURL(_streamingAssets);

        }

    }
}