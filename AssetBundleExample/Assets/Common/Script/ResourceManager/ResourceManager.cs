using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class ResourceManager : MonoBehaviour
{
    public static string AssetBundlePath
    {
        get
        {
            return Application.streamingAssetsPath + "/AssetBundles";
        }
    }

    public static string AssetInfoXMLPath
    {
        get
        {
            return Application.streamingAssetsPath + "/XML";
        }
    }

    public bool ForceAssetBundlesInEditor = false;

    private Dictionary<ID, AssetBundle> loadedBundles = new Dictionary<ID, AssetBundle>();

    private IEnumerator LoadBundlesCo(System.Action onComplete)
    {
        // Get bundle infos from the database
        AssetBundleInfo[] bundleInfos = Database.Instance.GetEntries<AssetBundleInfo>();

        // Load each bundle using the path in the bundle info
        foreach (AssetBundleInfo info in bundleInfos)
        {
            string url = "file://" + AssetBundlePath + "/" + info.Name + ".unity3d";

            Debug.Log("Loading bundle: " + url);

            WWW www = new WWW(url);
            yield return www;

            // Add the bundle to our collection of loaded bundles
            loadedBundles.Add(info.DatabaseID, www.assetBundle);
        }

        // Call the complete delegate
        if (onComplete != null)
        {
            onComplete();
        }
    }

    public T LoadAsset<T>(AssetInfo info) where T : UnityEngine.Object
    {
        // If we're not in the editor or we're forcing the editor to use bundles...
        if (!Application.isEditor || ForceAssetBundlesInEditor)
        {
            if (info.AssetBundleInfoRef != null)
            {
                // Get the bundle from our loaded bundles collection
                AssetBundle bundle;
                if (!loadedBundles.TryGetValue(info.AssetBundleInfoRef.Entry.DatabaseID, out bundle))
                {
                    throw new System.Exception("Asset bundle not found: " + info.AssetBundleInfoRef.Entry.Name);
                }

                // Async load the bundle and wait for the bundle request to finish
                AssetBundleRequest request = bundle.LoadAsync(info.Path, typeof(T));
                while (!request.isDone) { } // wait

                // If the asset is the correct type, cast it and return.
                if (request.asset is T)
                {
                    return request.asset as T;
                }
                else
                {
                    throw new System.Exception("Asset type mismatch");
                }
            }
            else
            {
                throw new System.Exception("Invalid Asset Bundle Info");
            }
        }
        else
        {
#if UNITY_EDITOR
            return EditorLoadAsset<T>(info);
#else
            throw new System.Exception("Trying use editor load outside of editor");
#endif
        }
    }

    public void LoadAssetAsync<T>(AssetInfo info, System.Action<T> onComplete) where T : UnityEngine.Object
    {
        if (!Application.isEditor || ForceAssetBundlesInEditor)
        {
            StartCoroutine(LoadAssetAsyncCo<T>(info, onComplete));
        }
        else
        {
#if UNITY_EDITOR
            T asset = EditorLoadAsset<T>(info);
            if (onComplete != null)
            {
                onComplete(asset);
            }
#else
            throw new System.Exception("Trying use editor load outside of editor");
#endif
        }
    }

    public void LoadBundlesAsync(System.Action onComplete)
    {
        StartCoroutine(LoadBundlesCo(onComplete));
    }

    private IEnumerator LoadAssetAsyncCo<T>(AssetInfo info, System.Action<T> onComplete) where T : UnityEngine.Object
    {
        if (info.AssetBundleInfoRef != null)
        {
            // Get the bundle from the loaded bundles collection
            AssetBundle bundle;
            if (!loadedBundles.TryGetValue(info.AssetBundleInfoRef.Entry.DatabaseID, out bundle))
            {
                throw new System.Exception("Asset bundle not found: " + info.AssetBundleInfoRef.Entry.Name);
            }

            // Async load the bundle.  Yield until it is loaded.
            AssetBundleRequest request = bundle.LoadAsync(info.Path, typeof(T));
            yield return request;

            // Except if there is no asset in the request
            if (request.asset == null)
            {
                throw new System.Exception("Asset missing from bundle.  Did you forget to rebuild?");
            }

            // Cast and return if the asset is the correc type.
            if (request.asset is T)
            {
                if (onComplete != null)
                {
                    onComplete(request.asset as T);
                }
            }
            else
            {
                throw new System.Exception("Asset type mismatch");
            }
        }
        else
        {
            throw new System.Exception("Invalid Asset Bundle Info");
        }
    }

#if UNITY_EDITOR
    private T EditorLoadAsset<T>(AssetInfo info) where T : UnityEngine.Object
    {
        return Resources.LoadAssetAtPath<T>(info.Path);
    }
#endif

#if UNITY_EDITOR
    // This function assumes an initialized database
    private static void ValidateAssetBundleAssetsInternal()
    {
        // Get all asset infos from the database and iterate over them
        AssetInfo[] assetInfos = Database.Instance.GetEntries<AssetInfo>();
        foreach (AssetInfo assetInfo in assetInfos)
        {
            // Load the object referenced by the asset info.  Warn if the
            // object fails to load
            Object asset = Resources.LoadAssetAtPath<Object>(assetInfo.Path);
            if (!asset)
            {
                Debug.LogWarning("Invalid Asset: " + assetInfo.Path + " in AssetInfo: " + assetInfo.DatabaseID.ToString());
            }
        }

        // Clean up
        Resources.UnloadUnusedAssets();
    }

	[MenuItem("Assets/Validate AssetBundle Assets")]
	private static void ValidateAssetBundleAssets()
	{
        Debug.Log("Validating assets...");

        Database.Instance.Clear();
        Database.Instance.ReadFiles(AssetInfoXMLPath);

        ValidateAssetBundleAssetsInternal();

        Database.Instance.Clear();

        Debug.Log("Finished validating assets.");
	}

    [MenuItem("Assets/Clear AssetBundles")]
    private static void ClearAssetBundles()
    {
        if (Directory.Exists(AssetBundlePath))
        {
            string[] existingAssetBundlePaths = Directory.GetFiles(AssetBundlePath);
            foreach (string bundlePath in existingAssetBundlePaths)
            {
                if (bundlePath.EndsWith(".unity3d"))
                {
                    Debug.Log("Deleting bundle: " + bundlePath);
                    File.Delete(bundlePath);
                }
            }
        }
        else
        {
            Directory.CreateDirectory(AssetBundlePath);
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/Build AssetBundles")]
    private static void BuildAssetBundles()
    {
        ClearAssetBundles();

        // You would probably want to parameterize this part
        Database.Instance.ReadFiles(AssetInfoXMLPath);

        // Validate asset infos
        ValidateAssetBundleAssetsInternal();

        // Get all asset infos and asset bundle infos from the database
        AssetInfo[] assetInfos = Database.Instance.GetEntries<AssetInfo>();
        AssetBundleInfo[] assetBundleInfos = Database.Instance.GetEntries<AssetBundleInfo>();

        // We need to build a list of all objects and object names (paths) for each
        // asset bundle.  Store these lists in two dictionaries, keyed to the
        // asset bundle info ID that each asset info references.
        Dictionary<ID, List<Object>> bundleObjects = new Dictionary<ID, List<Object>>();
        Dictionary<ID, List<string>> bundleNames = new Dictionary<ID, List<string>>();

        foreach (AssetInfo info in assetInfos)
        {
            List<Object> objectList;
            if (!bundleObjects.TryGetValue(info.AssetBundleInfoRef.Entry.DatabaseID, out objectList))
            {
                objectList = new List<Object>();
                bundleObjects.Add(info.AssetBundleInfoRef.Entry.DatabaseID, objectList);
            }

            List<string> nameList;
            if (!bundleNames.TryGetValue(info.AssetBundleInfoRef.Entry.DatabaseID, out nameList))
            {
                nameList = new List<string>();
                bundleNames.Add(info.AssetBundleInfoRef.Entry.DatabaseID, nameList);
            }

            UnityEngine.Object asset = Resources.LoadAssetAtPath<UnityEngine.Object>(info.Path);
            if (asset == null)
            {
                throw new System.Exception("Invalid asset: " + info.Path);
            }

            objectList.Add(asset);
            nameList.Add(info.Path);
        }

        // Build bundles
        foreach (AssetBundleInfo info in assetBundleInfos)
        {
            // Get the list of objects for this bundle
            List<Object> objectList;
            if (!bundleObjects.TryGetValue(info.DatabaseID, out objectList))
            {
                Debug.LogWarning("No objects for bundle: " + info.Name);

                continue;
            }

            // Get the list of names for this bundle
            List<string> nameList;
            if (!bundleNames.TryGetValue(info.DatabaseID, out nameList))
            {
                throw new System.Exception("No names generated for objects");
            }

            // Build the bundle using the two lists
            string path = AssetBundlePath + "/" + info.Name + ".unity3d";
            BuildAssetBundleOptions options = (BuildAssetBundleOptions.CollectDependencies | 
                                               BuildAssetBundleOptions.CompleteAssets |
                                               BuildAssetBundleOptions.DeterministicAssetBundle);
            BuildTarget target = BuildTarget.WebPlayer;
            if (!BuildPipeline.BuildAssetBundleExplicitAssetNames(objectList.ToArray(), 
                                                                  nameList.ToArray(), 
                                                                  path, 
                                                                  options,
                                                                  target))
            {
                throw new System.Exception("Unable to build asset bundle: " + info.Name);
            }

            AssetDatabase.SaveAssets();

            Debug.Log("Built bundle: " + info.Name);
        }
    }
#endif
}
