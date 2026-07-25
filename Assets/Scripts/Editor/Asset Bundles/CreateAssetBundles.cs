using UnityEditor;
using System;
using UnityEngine;
using System.IO;

public class CreateAssetBundles
{

    [MenuItem("Assets/Asset Bundles/Build All Asset Bundles")]
    private static void BuildAllAssetBundles()
    {
        string assetBundlePath = Path.Combine(Application.dataPath, "AssetBundles");
        if (!Directory.Exists(assetBundlePath)) Directory.CreateDirectory(assetBundlePath);

        try
        {
            BuildPipeline.BuildAssetBundles(assetBundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);


        }
        catch (SystemException e)
        {
            Debug.LogError($"{e}");
        }
    }

}