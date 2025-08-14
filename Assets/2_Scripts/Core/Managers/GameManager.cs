using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SoundManager _soundManager;
    [SerializeField] private Backpack _backpack;

    public SoundManager SoundManager => _soundManager;
    public Backpack Backpack => _backpack;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        DontDestroyOnLoad(gameObject);
    }
}