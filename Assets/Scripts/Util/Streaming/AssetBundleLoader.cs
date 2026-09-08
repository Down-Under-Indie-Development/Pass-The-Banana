using System;
using Utility;
using UnityEngine;
using System.IO;
using System.Collections;

namespace GPG214.Project.One.Streaming
{
    public class AssetBundleLoader : Singleton<AssetBundleLoader>
    {
        private string assetBundlePath = Path.Combine(Application.dataPath, "AssetBundles");

        public IEnumerator LoadAssetBundle(string assetBundleName, Action<AssetBundle> onLoaded)
        {
            string fullAssetBundleLocation = Path.Combine(assetBundlePath, assetBundleName);

            // GUARD: Prevent nulls
            if (!File.Exists(fullAssetBundleLocation))
            {
                Debug.LogWarning($"File cannot be found at ({fullAssetBundleLocation})");
                yield break;

            }

            // INFO: Load asset bundle asynchronously
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(fullAssetBundleLocation);
            yield return request;

            Debug.Log($"Loaded Asset bundle ({assetBundleName})");
            onLoaded?.Invoke(request.assetBundle);
        }

    }
}