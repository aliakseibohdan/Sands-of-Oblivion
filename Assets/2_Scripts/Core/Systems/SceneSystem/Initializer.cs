using UnityEngine;

public class Initializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]

    public static void Execute()
    {
        Debug.Log("Loaded by the Persist Objects from the Initializer script");
        DontDestroyOnLoad(Instantiate(Resources.Load("PERSISTOBJECTS")));
    }
}
