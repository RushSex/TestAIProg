using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Менеджер лобби и матчмейкинга.
/// Обрабатывает создание комнат, подключение игроков и распределение ролей.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    [Header("Lobby Settings")]
    [SerializeField] private int maxPlayers = 5; // 1 маньяк + 4 выживших
    [SerializeField] private float matchStartDelay = 10f;
    
    [Header("Spawn Points")]
    [SerializeField] private Transform[] survivorSpawnPoints;
    [SerializeField] private Transform[] maniacSpawnPoints;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject survivorPrefabPC;
    [SerializeField] private GameObject survivorPrefabMobile;
    [SerializeField] private GameObject maniacPrefabPC;
    [SerializeField] private GameObject maniacPrefabMobile;
    
    public static LobbyManager Instance { get; private set; }
    
    // Состояние лобби
    private NetworkVariable<int> connectedPlayers = new NetworkVariable<int>(0);
    private NetworkVariable<bool> lobbyReady = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> matchStarted = new NetworkVariable<bool>(false);
    
    // Список игроков в лобби
    private System.Collections.Generic.Dictionary<ulong, PlayerInfo> playerList = 
        new System.Collections.Generic.Dictionary<ulong, PlayerInfo>();
    
    private class PlayerInfo
    {
        public ulong ClientId;
        public string PlayerName;
        public bool IsMobile;
        public CharacterRole AssignedRole;
    }
    
    public enum CharacterRole
    {
        None,
        Survivor,
        Maniac
    }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        
        DontDestroyOnLoad(gameObject);
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            connectedPlayers.OnValueChanged += OnPlayerCountChanged;
            lobbyReady.OnValueChanged += OnLobbyReadyChanged;
        }
    }
    
    /// <summary>
    /// Запрос на присоединение к лобби.
    /// Вызывается клиентом при подключении.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void JoinLobbyServerRpc(string playerName, bool isMobile, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        
        ulong clientId = rpcParams.Receive.SenderClientId;
        
        // Проверка максимального количества игроков
        if (connectedPlayers.Value >= maxPlayers)
        {
            RejectPlayerClientRpc(clientId, "Lobby is full");
            return;
        }
        
        // Добавление игрока в список
        PlayerInfo info = new PlayerInfo
        {
            ClientId = clientId,
            PlayerName = playerName,
            IsMobile = isMobile,
            AssignedRole = CharacterRole.None
        };
        
        playerList[clientId] = info;
        connectedPlayers.Value++;
        
        Debug.Log($"Игрок {playerName} присоединился к лобби. Всего: {connectedPlayers.Value}/{maxPlayers}");
        
        // Уведомление всех клиентов об обновлении списка
        UpdatePlayerListClientRpc();
        
        // Проверка готовности лобби
        if (connectedPlayers.Value >= 2) // Минимум 2 игрока для начала
        {
            lobbyReady.Value = true;
        }
    }
    
    /// <summary>
    /// Запрос на начало матча.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void StartMatchServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (matchStarted.Value) return;
        
        // Проверка минимального количества игроков
        if (connectedPlayers.Value < 2)
        {
            Debug.LogWarning("Недостаточно игроков для начала матча");
            return;
        }
        
        // Распределение ролей
        AssignRoles();
        
        // Задержка перед началом
        Invoke(nameof(ExecuteMatchStart), matchStartDelay);
        
        // Уведомление о начале отсчета
        MatchStartingClientRpc(matchStartDelay);
    }
    
    private void AssignRoles()
    {
        if (playerList.Count == 0) return;
        
        // Случайный выбор маньяка
        var players = new System.Collections.Generic.List<ulong>(playerList.Keys);
        int maniacIndex = Random.Range(0, players.Count);
        ulong maniacId = players[maniacIndex];
        
        playerList[maniacId].AssignedRole = CharacterRole.Maniac;
        
        // Остальные - выжившие
        for (int i = 0; i < players.Count; i++)
        {
            if (i != maniacIndex)
            {
                playerList[players[i]].AssignedRole = CharacterRole.Survivor;
            }
        }
        
        Debug.Log($"Роль маньяка назначена игроку {playerList[maniacId].PlayerName}");
    }
    
    private void ExecuteMatchStart()
    {
        if (!IsServer) return;
        
        matchStarted.Value = true;
        
        // Спавн игроков с назначенными ролями
        foreach (var kvp in playerList)
        {
            SpawnPlayer(kvp.Value);
        }
        
        // Запуск игровой логики
        GameManager.Instance.StartMatch();
        
        MatchStartedClientRpc();
    }
    
    private void SpawnPlayer(PlayerInfo player)
    {
        if (player.AssignedRole == CharacterRole.None) return;
        
        // Выбор префаба в зависимости от платформы
        GameObject prefab = GetPrefabForPlayer(player);
        
        // Выбор точки спавна
        Vector3 spawnPosition;
        Quaternion spawnRotation = Quaternion.identity;
        
        if (player.AssignedRole == CharacterRole.Maniac)
        {
            if (maniacSpawnPoints.Length > 0)
            {
                Transform spawnPoint = maniacSpawnPoints[Random.Range(0, maniacSpawnPoints.Length)];
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
            }
            else
            {
                spawnPosition = Vector3.zero;
            }
        }
        else
        {
            if (survivorSpawnPoints.Length > 0)
            {
                // Распределение выживших по разным точкам
                int spawnIndex = System.Array.FindIndex(
                    playerList.Values.ToArray(), 
                    p => p.AssignedRole == CharacterRole.Survivor
                );
                spawnIndex %= survivorSpawnPoints.Length;
                
                Transform spawnPoint = survivorSpawnPoints[spawnIndex];
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
            }
            else
            {
                spawnPosition = new Vector3(Random.Range(-50f, 50f), 1f, Random.Range(-50f, 50f));
            }
        }
        
        // Спавн через NetworkManager
        GameObject playerObject = Instantiate(prefab, spawnPosition, spawnRotation);
        NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
        
        // Передача владения клиенту
        networkObject.SpawnAsPlayerObject(player.ClientId);
    }
    
    private GameObject GetPrefabForPlayer(PlayerInfo player)
    {
        bool isMobile = player.IsMobile;
        
        if (player.AssignedRole == CharacterRole.Maniac)
        {
            return isMobile ? maniacPrefabMobile : maniacPrefabPC;
        }
        else
        {
            return isMobile ? survivorPrefabMobile : survivorPrefabPC;
        }
    }
    
    private void OnPlayerCountChanged(int previous, int current)
    {
        if (IsServer)
        {
            Debug.Log($"Количество игроков изменилось: {previous} -> {current}");
        }
    }
    
    private void OnLobbyReadyChanged(bool previous, bool current)
    {
        if (current && IsServer)
        {
            Debug.Log("Лобби готово к началу матча");
            LobbyReadyClientRpc();
        }
    }
    
    [ClientRpc]
    private void UpdatePlayerListClientRpc()
    {
        // Обновление UI списка игроков
        Debug.Log("Обновление списка игроков в лобби");
    }
    
    [ClientRpc]
    private void LobbyReadyClientRpc()
    {
        Debug.Log("Лобби готово! Ожидание команды старта...");
    }
    
    [ClientRpc]
    private void MatchStartingClientRpc(float delay)
    {
        Debug.Log($"Матч начнется через {delay} секунд...");
        // Запуск таймера в UI
    }
    
    [ClientRpc]
    private void MatchStartedClientRpc()
    {
        Debug.Log("Матч начался!");
        // Активация игрового UI, скрытие лобби
    }
    
    [ClientRpc]
    private void RejectPlayerClientRpc(ulong clientId, string reason)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.LogError($"В доступе отказано: {reason}");
            // Показать сообщение игроку
        }
    }
    
    /// <summary>
    /// Отключение игрока от лобби.
    /// </summary>
    public void LeaveLobby()
    {
        if (NetworkManager.Singleton.IsConnectedClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
