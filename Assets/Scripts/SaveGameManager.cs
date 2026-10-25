using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class SaveGameManager : MonoBehaviour
{
    private const string NewGameScene = "Assets/Scenes/Dirtmouth.unity";
    private const string MenuScene = "Assets/Scenes/MainMenu.unity";
    private static SaveGameManager instance;
    private SaveRepository repository;
    private PlayerController player;
    private SaveSlotData current;
    private int activeSlot = -1;
    private bool restoring;
    private bool loading;
    private float snapshotTimer;
    private float saveTimer;

    public static SaveGameManager Instance
    {
        get
        {
            if (instance == null)
                new GameObject(nameof(SaveGameManager)).AddComponent<SaveGameManager>();
            return instance;
        }
    }

    public bool MantisLordsDefeated => current != null && current.mantisLordsDefeated;
    public void CompleteMantisLords()
    {
        if (current == null) return;
        current.mantisLordsDefeated = true;
        SaveCurrentGame();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() { _ = Instance; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        repository = new SaveRepository(Path.Combine(Application.persistentDataPath, "Saves"));
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public SaveSlotStatus ReadSlot(int slot, out SaveSlotData data) => repository.Read(slot, out data);

    public bool OpenSlot(int slot)
    {
        if (loading)
            return false;
        SaveSlotStatus status = repository.Read(slot, out var saved);
        if (status == SaveSlotStatus.Corrupt)
        {
            Debug.LogError("存档已损坏，请用 Clear Save 清除这个槽位后重新开始。");
            return false;
        }

        string scenePath = status == SaveSlotStatus.Ready ? saved.scenePath : NewGameScene;
        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"存档场景无法加载：{scenePath}");
            return false;
        }

        current = saved ?? new SaveSlotData { slot = slot, scenePath = scenePath };
        activeSlot = slot;
        restoring = true;
        return LoadScene(scenePath);
    }

    public bool DeleteSlot(int slot)
    {
        if (loading)
            return false;
        try
        {
            repository.Delete(slot);
            if (activeSlot == slot)
            {
                activeSlot = -1;
                current = null;
            }
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            Debug.LogError($"清除存档失败：{error.Message}");
            return false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        loading = false;
        player = null;
        foreach (var candidate in FindObjectsByType<PlayerController>())
        {
            if (candidate.gameObject.scene == scene)
            {
                RegisterPlayer(candidate);
                break;
            }
        }
    }

    public void RegisterPlayer(PlayerController candidate)
    {
        if (player == candidate)
            return;
        player = candidate;
        if (candidate.GetComponent<PauseMenuController>() == null)
            candidate.gameObject.AddComponent<PauseMenuController>();
        snapshotTimer = saveTimer = 0f;
        if (activeSlot < 0 || current == null)
            return;

        if (current.hasSoul) player.GetComponent<PlayerSoul>()?.Set(current.soul);

        if (restoring && current.hasPosition && current.scenePath == player.gameObject.scene.path)
        {
            var position = new Vector3(current.x, current.y, current.z);
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            Physics2D.SyncTransforms();
        }
        else
        {
            // 新游戏先记录场景设计的出生点，不必等玩家移动后才有存档。
            RecordPosition();
            SaveCurrentGame();
        }
        restoring = false;
    }

    private void Update()
    {
        if (loading || player == null)
            return;

        if (activeSlot >= 0 && current != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health == null || health.CurrentHealth > 0)
                current.playSeconds += Time.deltaTime;
            snapshotTimer += Time.deltaTime;
            saveTimer += Time.deltaTime;
            if (snapshotTimer >= 0.5f)
            {
                snapshotTimer = 0f;
                if (player.CanSave)
                    RecordPosition();
            }
            if (GameOptions.AutoSave && saveTimer >= 5f)
            {
                saveTimer = 0f;
                SaveCurrentGame();
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (keyboard.f5Key.wasPressedThisFrame)
        {
            if (SaveCurrentGame())
                Debug.Log("存档已保存。");
        }
    }

    private void RecordPosition()
    {
        Vector3 position = player.transform.position;
        current.scenePath = player.gameObject.scene.path;
        current.x = position.x;
        current.y = position.y;
        current.z = position.z;
        current.hasPosition = true;
    }

    public bool SaveCurrentGame()
    {
        if (activeSlot < 0 || current == null || player == null || loading)
            return false;
        if (player.CanSave)
            RecordPosition();
        if (!current.hasPosition)
            return false;
        try
        {
            var soul = player.GetComponent<PlayerSoul>();
            if (soul != null) { current.hasSoul = true; current.soul = soul.Current; }
            current.savedUtc = DateTime.UtcNow.ToString("O");
            repository.Write(current);
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
            error is ArgumentException)
        {
            Debug.LogError($"保存失败，原存档将保留：{error.Message}");
            return false;
        }
    }

    public void ReturnToMenu() => TryReturnToMenu();

    public bool TryReturnToMenu()
    {
        if (loading || (activeSlot >= 0 && !SaveCurrentGame()))
            return false;
        restoring = false;
        return LoadScene(MenuScene);
    }

    public void Respawn()
    {
        if (loading)
            return;
        if (current != null) { current.hasSoul = true; current.soul = 0; }
        // 死亡不记录当前位置，回到最近的安全位置，生命由新场景初始化。
        restoring = activeSlot >= 0 && current != null && current.hasPosition;
        LoadScene(restoring ? current.scenePath : SceneManager.GetActiveScene().path);
    }

    public bool TravelToScene(string scenePath)
    {
        if (loading || !Application.CanStreamedLevelBeLoaded(scenePath)) return false;
        if (activeSlot >= 0 && current != null && !SaveCurrentGame()) return false;
        restoring = false;
        return LoadScene(scenePath);
    }

    private bool LoadScene(string path)
    {
        if (!Application.CanStreamedLevelBeLoaded(path))
        {
            Debug.LogError($"场景不在构建列表中：{path}");
            return false;
        }
        loading = true;
        Time.timeScale = 1f;
        try
        {
            SceneManager.LoadSceneAsync(path);
            return true;
        }
        catch (Exception error)
        {
            loading = false;
            Debug.LogError($"加载场景失败：{error.Message}");
            return false;
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveCurrentGame();
    }

    private void OnApplicationQuit() => SaveCurrentGame();

    private void OnDestroy()
    {
        if (instance != this)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
