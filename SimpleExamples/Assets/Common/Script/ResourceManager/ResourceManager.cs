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

    public bool ForceAssetBundlesInEditor = false;

    private Dictionary<ID, AssetBundle> loadedBundles = new Dictionary<ID, AssetBundle>();

    public IEnumerator LoadBundles(Database database)
    {
        AssetBundleInfo[] bundleInfos = database.GetEntries<AssetBundleInfo>();
        
        foreach (AssetBundleInfo info in bundleInfos)
        {
            WWW www = new WWW("file://" + Application.streamingAssetsPath + "/AssetBundles/" + info.Name + ".unity3d");
            yield return www;

            loadedBundles.Add(info.DatabaseID, www.assetBundle);
        }
    }

    public T LoadAsset<T>(AssetInfo info) where T : UnityEngine.Object
    {
        if (!Application.isEditor || ForceAssetBundlesInEditor)
        {
            if (info.AssetBundleInfoRef != null)
            {
                AssetBundle bundle;
                if (!loadedBundles.TryGetValue(info.AssetBundleInfoRef.Entry.DatabaseID, out bundle))
                {
                    throw new System.Exception("Asset bundle not found: " + info.AssetBundleInfoRef.Entry.Name);
                }
                
                AssetBundleRequest request = bundle.LoadAsync(info.Path, typeof(T));
                while (!request.isDone) { } // wait
                
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

    private IEnumerator LoadAssetAsyncCo<T>(AssetInfo info, System.Action<T> onComplete) where T : UnityEngine.Object
    {
        if (info.AssetBundleInfoRef != null)
        {
            AssetBundle bundle;
            if (!loadedBundles.TryGetValue(info.AssetBundleInfoRef.Entry.DatabaseID, out bundle))
            {
                // Load bundle
                WWW www = new WWW("file://" + AssetBundlePath + "/" + info.AssetBundleInfoRef.Entry.Name + ".unity3d");
                yield return www;

                bundle = www.assetBundle;

                loadedBundles.Add(info.AssetBundleInfoRef.Entry.DatabaseID, bundle);
            }

            AssetBundleRequest request = bundle.LoadAsync(info.Path, typeof(T));
            yield return request;

            if (request.asset == null)
            {
                throw new System.Exception("Asset missing from bundle.  Did you forget to rebuild?");
            }

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
        Database.Instance.ReadFiles(Application.streamingAssetsPath + "/XML/");

        AssetInfo[] assetInfos = Database.Instance.GetEntries<AssetInfo>();
        AssetBundleInfo[] assetBundleInfos = Database.Instance.GetEntries<AssetBundleInfo>();

        // Collect objects and names
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
            List<Object> objectList;
            if (!bundleObjects.TryGetValue(info.DatabaseID, out objectList))
            {
                Debug.LogWarning("No objects for bundle: " + info.Name);

                continue;
            }

            List<string> nameList;
            if (!bundleNames.TryGetValue(info.DatabaseID, out nameList))
            {
                throw new System.Exception("No names generated for objects");
            }

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
