using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Центральный менеджер игры.
/// Управляет состоянием матча, подсчетом очков, условиями победы.
/// </summary>
public class GameManager : NetworkBehaviour
{
    [Header("Match Settings")]
    [SerializeField] private int maxSurvivors = 4;
    [SerializeField] private float matchTimeLimit = 900f; // 15 минут в секундах
    
    [Header("References")]
    [SerializeField] private Transform[] survivorSpawnPoints;
    [SerializeField] private Transform maniacSpawnPoint;
    
    // Singleton instance
    private static GameManager instance;
    public static GameManager Instance => instance;
    
    // Состояние матча
    private NetworkVariable<int> networkGameState = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<float> networkMatchTime = new NetworkVariable<float>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<int> networkEscapedCount = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<int> networkEliminatedCount = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    // Состояния игры
    public enum GameState
    {
        Waiting,      // Ожидание игроков
        Playing,      // Матч идет
        SurvivorsWin, // Победа выживших
        ManiacWin     // Победа маньяка
    }
    
    public GameState CurrentGameState => (GameState)networkGameState.Value;
    public float MatchTime => networkMatchTime.Value;
    public int EscapedCount => networkEscapedCount.Value;
    public int EliminatedCount => networkEliminatedCount.Value;
    
    private float matchTimer;
    private bool isMatchActive = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        if (IsServer)
        {
            networkGameState.Value = (int)GameState.Waiting;
            networkMatchTime.Value = matchTimeLimit;
            networkEscapedCount.Value = 0;
            networkEliminatedCount.Value = 0;
            
            matchTimer = matchTimeLimit;
        }
        
        networkGameState.OnValueChanged += OnGameStateChanged;
    }
    
    private void Update()
    {
        if (!IsServer || !isMatchActive) return;
        
        // Отсчет времени матча
        matchTimer -= Time.deltaTime;
        networkMatchTime.Value = matchTimer;
        
        if (matchTimer <= 0)
        {
            EndMatch(GameState.SurvivorsWin); // Время вышло - победа выживших
        }
    }
    
    /// <summary>
    /// Начало матча.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void StartMatchServerRpc()
    {
        if (!IsServer) return;
        
        // Спавн персонажей
        SpawnCharacters();
        
        networkGameState.Value = (int)GameState.Playing;
        isMatchActive = true;
        
        Debug.Log("Матч начался!");
    }
    
    /// <summary>
    /// Спавн персонажей на стартовых позициях.
    /// </summary>
    private void SpawnCharacters()
    {
        // Спавн выживших
        // В реальной игре нужно использовать NetworkObject.Spawn()
        for (int i = 0; i < maxSurvivors && i < survivorSpawnPoints.Length; i++)
        {
            Transform spawnPoint = survivorSpawnPoints[i];
            // NetworkObject survivor = Instantiate(survivorPrefab, spawnPoint.position, spawnPoint.rotation);
            // survivor.Spawn();
        }
        
        // Спавн маньяка
        if (maniacSpawnPoint != null)
        {
            // NetworkObject maniac = Instantiate(maniacPrefab, maniacSpawnPoint.position, maniacSpawnPoint.rotation);
            // maniac.Spawn();
        }
    }
    
    /// <summary>
    /// Вызовется когда выживший устранен маньяком.
    /// </summary>
    public void OnSurvivorEliminated(GameObject survivor)
    {
        if (!IsServer) return;
        
        networkEliminatedCount.Value++;
        
        Debug.Log($"Выживший устранен! Осталось: {maxSurvivors - networkEliminatedCount.Value}");
        
        // Проверка условия победы маньяка
        int remainingSurvivors = maxSurvivors - networkEliminatedCount.Value - networkEscapedCount.Value;
        
        if (remainingSurvivors <= 0)
        {
            EndMatch(GameState.ManiacWin);
        }
    }
    
    /// <summary>
    /// Вызовется когда выживший сбежал через один из путей эвакуации.
    /// </summary>
    public void OnSurvivorsEscaped(int escapePathId)
    {
        if (!IsServer) return;
        
        networkEscapedCount.Value++;
        
        Debug.Log($"Выживший сбежал через путь {escapePathId}! Всего сбежало: {networkEscapedCount.Value}");
        
        // Проверка условия победы выживших
        // Если хотя бы один выживший сбежал - это уже частичная победа
        // В некоторых реализациях требуется чтобы сбежали все выжившие
        
        // Для данной реализации: если сбежал хотя бы 1 выживший - победа выживших
        if (networkEscapedCount.Value >= 1)
        {
            EndMatch(GameState.SurvivorsWin);
        }
    }
    
    /// <summary>
    /// Вызовется когда объект эвакуации завершен.
    /// </summary>
    public void OnObjectiveCompleted(BaseEscapeObjective objective)
    {
        if (!IsServer) return;
        
        Debug.Log($"Объект эвакуации завершен: {objective.ObjectiveName}");
        
        // Можно добавить дополнительные эффекты или уведомления
    }
    
    /// <summary>
    /// Завершение матча.
    /// </summary>
    private void EndMatch(GameState result)
    {
        if (!IsServer) return;
        
        isMatchActive = false;
        networkGameState.Value = (int)result;
        
        string winner = result == GameState.SurvivorsWin ? "Выжившие" : "Маньяк";
        Debug.Log($"Матч завершен! Победили: {winner}");
        
        // Уведомление всех клиентов о результате
        Invoke(nameof(EndMatchClientRpc), 0.5f);
    }
    
    [ClientRpc]
    private void EndMatchClientRpc()
    {
        // Показ UI результатов на всех клиентах
        Debug.Log("Показать экран результатов");
        // UIManager.Instance.ShowMatchResults(CurrentGameState);
    }
    
    private void OnGameStateChanged(int previous, int current)
    {
        if (!IsOwner) return;
        
        GameState newState = (GameState)current;
        
        switch (newState)
        {
            case GameState.Waiting:
                Debug.Log("Ожидание игроков...");
                break;
                
            case GameState.Playing:
                Debug.Log("Матч начался!");
                isMatchActive = true;
                break;
                
            case GameState.SurvivorsWin:
                Debug.Log("Выжившие победили!");
                isMatchActive = false;
                break;
                
            case GameState.ManiacWin:
                Debug.Log("Маньяк победил!");
                isMatchActive = false;
                break;
        }
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        networkGameState.OnValueChanged -= OnGameStateChanged;
        
        if (instance == this)
        {
            instance = null;
        }
    }
}
