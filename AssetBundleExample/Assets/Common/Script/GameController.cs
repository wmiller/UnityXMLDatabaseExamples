using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
    public event System.Action OnFinishedLoading; 

    private void Awake()
    {
        Database.Instance.ReadFiles(Application.streamingAssetsPath + "/XML/");
        GetComponent<ResourceManager>().LoadBundlesAsync(() => OnFinishedLoading());
    }
}
