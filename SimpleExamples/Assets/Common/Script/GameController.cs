using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
    private void Awake()
    {
        Database.Instance.ReadFiles(Application.streamingAssetsPath + "/XML/");
    }
}
