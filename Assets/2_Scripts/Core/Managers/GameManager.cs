using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SoundManager _soundManager;
    [SerializeField] private BackpackSystem _backpackSystem;

    public SoundManager SoundManager => _soundManager;
    public BackpackSystem BackpackSystem => _backpackSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        DontDestroyOnLoad(gameObject);
    }
}