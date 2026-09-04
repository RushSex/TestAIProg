# Интеграция Unity Netcode for GameObjects

## 1. Установка пакета

### Через Package Manager:
1. Откройте `Window > Package Manager`
2. Нажмите `+` > `Add package by name`
3. Введите: `com.unity.netcode.gameobjects`
4. Версия: `1.7.0` или выше
5. Нажмите `Add`

### Или через manifest.json:
Откройте `Packages/manifest.json` и добавьте:
```json
{
  "dependencies": {
    "com.unity.netcode.gameobjects": "1.7.0",
    "com.unity.transport": "1.4.0",
    "com.unity.multiplayer.tools": "1.1.0"
  }
}
```

## 2. Настройка NetworkManager

### Создание префаба NetworkManager:
1. Создайте пустой GameObject: `GameObject > Create Empty`
2. Назовите его `NetworkManager`
3. Добавьте компонент `NetworkManager`
4. Настройте секцию `Network Transport`:
   - Выберите `UnityTransport` (рекомендуется)
   - Port: `7777`
   - Max Connections: `10` (для теста 1v4 + запас)

### Конфигурация для разных платформ:

#### PC (Host/Dedicated Server):
```csharp
// В NetworkManager
NetworkConfig config = new NetworkConfig
{
    ProtocolVersion = 1,
    NetworkTransport = unityTransport,
    PlayerPrefab = survivorPrefab,
    Prefabs = new NetworkPrefabs
    {
        Prefabs = new List<NetworkPrefab>
        {
            new NetworkPrefab { Source = survivorPrefab },
            new NetworkPrefab { Source = maniacPrefab },
            new NetworkPrefab { Source = boatPrefab },
            new NetworkPrefab { Source = ziplinePrefab },
            new NetworkPrefab { Source = bridgePrefab }
        }
    },
    TickRate = 30,
    ClientConnectionBufferTimeout = 10f,
    ConnectionApproval = true, // Для кастомной логики подключения
    EnableSceneManagement = true,
    ForceSamePrefabs = true
};
```

#### Mobile (Client Only):
- Отключите хостинг на мобильных устройствах
- Используйте `NetworkManager.Singleton.StartClient()` только

## 3. Регистрация префабов

Все сетевые объекты должны быть зарегистрированы в NetworkManager:

```csharp
// Пример регистрации в GameManager.cs
public override void OnNetworkSpawn()
{
    if (NetworkManager.Singleton == null)
    {
        Debug.LogError("NetworkManager не найден!");
        return;
    }
    
    // Регистрация префабов происходит автоматически через Inspector
    // Убедитесь, что все префабы имеют компонент NetworkObject
}
```

## 4. Компонент NetworkObject

Добавьте `NetworkObject` компонент ко всем сетевым объектам:
- Префабы персонажей (Survivor, Maniac)
- Объекты эвакуации (Boat, Zipline, Bridge)
- Сундуки с лутом
- Двери

### Настройка NetworkObject:
- **Scene Migration**: Включено (для загрузки сцен)
- **DontDestroyWithOwner**: Выключено (кроме менеджеров)
- **OwnerShipSection**: 
  - Для игроков: `OwnerShipMode` = Dynamic
  - Для объектов мира: `OwnerShipMode` = Static

## 5. NetworkVariable vs RPC

### Используйте NetworkVariable когда:
- Данные меняются часто (позиция, здоровье, состояние)
- Нужно автоматическое уведомление клиентов
- Пример: `health`, `characterState`, `escapeProgress`

### Используйте RPC когда:
- Происходит однократное действие
- Нужна мгновенная реакция
- Пример: `PlayAnimation()`, `SpawnBloodEffect()`, `TriggerQTE()`

## 6. Авторизация подключения

```csharp
// В GameManager.cs или кастомном NetworkManager
private void Start()
{
    NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
}

private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, 
                          NetworkManager.ConnectionApprovalResponse response)
{
    string playerId = Encoding.ASCII.GetString(request.Payload);
    
    response.Approved = true;
    response.CreatePlayerObject = true;
    response.PlayerPrefabHash = GetPlayerPrefabHash(request);
    response.Position = GetSpawnPosition();
    response.Rotation = Quaternion.identity;
}

private ulong GetPlayerPrefabHash(NetworkManager.ConnectionApprovalRequest request)
{
    // Логика выбора префаба (Выживший или Маньяк)
    // Возвращает hash префаба из NetworkPrefabs
    return NetworkManager.Singleton.NetworkConfig.PlayerPrefab.GetComponent<NetworkObject>().GlobalObjectIdHash;
}
```

## 7. Тестирование сети

### Локальный тест (Multiple Clients):
1. Откройте `Netcode > Test > Launch Multi-Instance Test`
2. Укажите количество клиентов (например, 5)
3. Нажмите `Launch`
4. Первый клиент будет Host, остальные - Clients

### Параметры запуска:
```bash
# Host
-ma 127.0.0.1 -p 7777 -h

# Client 1
-ma 127.0.0.1 -p 7777 -c

# Client 2
-ma 127.0.0.1 -p 7777 -c
```

## 8. Оптимизация для мобильных устройств

### Network Config для Mobile:
```csharp
NetworkConfig mobileConfig = new NetworkConfig
{
    TickRate = 20, // Снижено с 30 для экономии батареи
    NetworkTransport = unityTransport,
    RpcHashSize = 4, // Уменьшенный размер хеша
    EnableMessagePackSerializer = false // Используем JSON для совместимости
};

// Unity Transport настройки
UnityTransport transport = GetComponent<UnityTransport>();
transport.MaxPacketSize = 6000; // Меньше пакет для мобильных сетей
transport.MaxConnectAttempts = 5;
transport.ConnectTimeoutMS = 10000;
```

### Приоритизация трафика:
```csharp
// Важные данные отправляются надежно
NetworkVariable<int> health = new NetworkVariable<int>(
    writePerm: NetworkVariableWritePermission.Server,
    readPerm: NetworkVariableReadPermission.Everyone,
    sendType: NetworkDelivery.ReliableSequenced
);

// Позиция отправляется ненадежно но часто
NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(
    sendType: NetworkDelivery.UnreliableSequenced
);
```

## 9. Обработка разрывов соединения

```csharp
private void OnEnable()
{
    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    NetworkManager.Singleton.OnServerLostCallback += OnServerLost;
}

private void OnClientDisconnect(ulong clientId)
{
    Debug.Log($"Клиент {clientId} отключился");
    // Очистка ресурсов, удаление объекта игрока
}

private void OnServerLost()
{
    Debug.Log("Соединение с сервером потеряно");
    // Показать UI реконнекта или вернуть в меню
}

public void AttemptReconnect()
{
    StartCoroutine(ReconnectCoroutine());
}

private IEnumerator ReconnectCoroutine()
{
    int attempts = 0;
    while (attempts < 3)
    {
        yield return new WaitForSeconds(2f);
        NetworkManager.Singleton.StartClient();
        
        if (NetworkManager.Singleton.IsConnectedClient)
            yield break;
            
        attempts++;
    }
    // Вернуть в меню после неудачных попыток
}
```

## 10. Безопасность и античит

### Серверная валидация:
```csharp
[ServerRpc(RequireOwnership = false)]
private void RequestInteractionServerRpc(ulong interactorId, ServerRpcParams rpcParams = default)
{
    // Проверка дистанции
    float distance = Vector3.Distance(
        transform.position, 
        NetworkManager.Singleton.SpawnManager.GetSpawnedObjects()[interactorId].transform.position
    );
    
    if (distance > maxInteractionDistance)
    {
        Debug.LogWarning("Попытка взаимодействия на слишком большой дистанции!");
        return; // Игнорируем запрос
    }
    
    // Проверка кулдауна
    if (Time.time - lastInteractionTime < interactionCooldown)
        return;
    
    lastInteractionTime = Time.time;
    
    // Выполнение действия
    Interact(interactorId);
}
```

### Rate Limiting для RPC:
```csharp
private Dictionary<ulong, Queue<float>> clientRpcTimestamps = new Dictionary<ulong, Queue<float>>();
private const float MAX_RPC_PER_SECOND = 10f;

private bool ValidateRpcRate(ulong clientId)
{
    if (!clientRpcTimestamps.ContainsKey(clientId))
        clientRpcTimestamps[clientId] = new Queue<float>();
    
    var timestamps = clientRpcTimestamps[clientId];
    float currentTime = Time.time;
    
    // Удаляем старые записи (> 1 секунды)
    while (timestamps.Count > 0 && timestamps.Peek() < currentTime - 1f)
        timestamps.Dequeue();
    
    // Проверяем лимит
    if (timestamps.Count >= MAX_RPC_PER_SECOND)
        return false;
    
    timestamps.Enqueue(currentTime);
    return true;
}
```

## 11. Деплой на платформы

### PC Build:
1. `File > Build Settings > PC, Mac & Linux Standalone`
2. Target Platform: Windows/Mac/Linux
3. Architecture: x86_64
4. Включить `Dedicated Server` опцию для серверной сборки

### Android Build:
1. `File > Build Settings > Android`
2. Switch Platform
3. Player Settings:
   - Minimum API Level: Android 7.0 (API 24)
   - Target API Level: Highest installed
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64
4. Internet Permission: Включено

### iOS Build:
1. `File > Build Settings > iOS`
2. Switch Platform
3. Player Settings:
   - Minimum iOS Version: 12.0
   - Architecture: ARM64
   - Require ARKit: Отключено
4. После сборки открыть в Xcode и настроить signing

## 12. Мониторинг и логирование

```csharp
public class NetworkStats : MonoBehaviour
{
    [SerializeField] private Text statsText;
    
    private void Update()
    {
        if (!NetworkManager.Singleton.IsConnectedClient) return;
        
        NetworkStats stats = NetworkManager.Singleton.NetworkMetrics;
        
        statsText.text = $"RTT: {stats.RttAverage:F2}ms\n" +
                        $"Sent: {stats.BytesSentPerSecond / 1024:F1} KB/s\n" +
                        $"Received: {stats.BytesReceivedPerSecond / 1024:F1} KB/s\n" +
                        $"Messages: {stats.MessagesSentPerSecond + stats.MessagesReceivedPerSecond}/s";
    }
}
```

Этот гайд обеспечит правильную интеграцию Netcode for GameObjects и подготовку проекта к кроссплатформенному мультиплееру.
