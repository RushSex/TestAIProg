using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Система инвентаря выжившего.
/// Управляет предметами: ключ зажигания, бухта троса, ящик с инструментами.
/// </summary>
public class SurvivorInventory : NetworkBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 2; // Максимальное количество слотов
    
    [Header("Starting Items")]
    [SerializeField] private ItemType[] startingItems;
    
    private InventorySlot[] inventorySlots;
    
    // NetworkVariables для синхронизации инвентаря
    private NetworkVariable<int> networkSlot0Type = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<int> networkSlot0Qty = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<int> networkSlot1Type = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    private NetworkVariable<int> networkSlot1Qty = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
    
    public bool HasIgnitionKey => HasItem(ItemType.IgnitionKey);
    public bool HasRopeCoil => HasItem(ItemType.RopeCoil);
    public bool HasToolBox => HasItem(ItemType.ToolBox);
    
    protected override void Awake()
    {
        base.Awake();
        inventorySlots = new InventorySlot[maxSlots];
        
        for (int i = 0; i < maxSlots; i++)
        {
            inventorySlots[i] = new InventorySlot();
        }
    }
    
    private void Start()
    {
        if (IsServer)
        {
            // Выдача стартовых предметов если указаны
            if (startingItems != null)
            {
                foreach (ItemType item in startingItems)
                {
                    if (item != ItemType.None)
                    {
                        AddItem(item);
                    }
                }
            }
            
            UpdateNetworkVariables();
        }
        
        // Подписка на изменения сетевых переменных
        SubscribeToNetworkChanges();
    }
    
    /// <summary>
    /// Добавление предмета в инвентарь.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int itemTypeInt)
    {
        if (!IsServer) return;
        
        ItemType itemType = (ItemType)itemTypeInt;
        
        // Проверка есть ли уже такой предмет (для стака)
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i].itemType == itemType)
            {
                inventorySlots[i].quantity++;
                UpdateNetworkVariables();
                return;
            }
        }
        
        // Поиск пустого слота
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i].IsEmpty)
            {
                inventorySlots[i].SetItem(itemType);
                UpdateNetworkVariables();
                return;
            }
        }
        
        // Инвентарь полон
        Debug.LogWarning("Инвентарь полон! Нельзя добавить предмет.");
    }
    
    /// <summary>
    /// Удаление предмета из инвентаря.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RemoveItemServerRpc(int itemTypeInt)
    {
        if (!IsServer) return;
        
        ItemType itemType = (ItemType)itemTypeInt;
        
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i].itemType == itemType)
            {
                inventorySlots[i].quantity--;
                
                if (inventorySlots[i].quantity <= 0)
                {
                    inventorySlots[i].Clear();
                }
                
                UpdateNetworkVariables();
                return;
            }
        }
    }
    
    /// <summary>
    /// Проверка наличия предмета в инвентаре.
    /// </summary>
    public bool HasItem(ItemType itemType)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (inventorySlots[i].itemType == itemType && inventorySlots[i].quantity > 0)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Публичный метод для удаления предмета (вызывается из BaseEscapeObjective).
    /// </summary>
    public void RemoveItem(ItemType itemType)
    {
        if (IsServer)
        {
            RemoveItemServerRpc((int)itemType);
        }
    }
    
    /// <summary>
    /// Использование предмета из инвентаря.
    /// </summary>
    public void UseItem(ItemType itemType)
    {
        if (!HasItem(itemType)) return;
        
        switch (itemType)
        {
            case ItemType.MedKit:
                UseMedKit();
                break;
            case ItemType.Flashlight:
                ToggleFlashlight();
                break;
        }
    }
    
    private void UseMedKit()
    {
        SurvivorHealth health = GetComponent<SurvivorHealth>();
        if (health != null)
        {
            health.HealServerRpc(50f); // Лечение на 50 единиц
            
            if (IsServer)
            {
                RemoveItemServerRpc((int)ItemType.MedKit);
            }
        }
    }
    
    private void ToggleFlashlight()
    {
        // Логика включения/выключения фонарика
        Light flashlight = GetComponentInChildren<Light>();
        if (flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
    
    private void UpdateNetworkVariables()
    {
        if (!IsServer) return;
        
        networkSlot0Type.Value = (int)inventorySlots[0].itemType;
        networkSlot0Qty.Value = inventorySlots[0].quantity;
        
        if (maxSlots > 1)
        {
            networkSlot1Type.Value = (int)inventorySlots[1].itemType;
            networkSlot1Qty.Value = inventorySlots[1].quantity;
        }
    }
    
    private void SubscribeToNetworkChanges()
    {
        networkSlot0Type.OnValueChanged += (prev, curr) => UpdateLocalInventory(0, curr, networkSlot0Qty.Value);
        networkSlot0Qty.OnValueChanged += (prev, curr) => UpdateLocalInventory(0, networkSlot0Type.Value, curr);
        
        if (maxSlots > 1)
        {
            networkSlot1Type.OnValueChanged += (prev, curr) => UpdateLocalInventory(1, curr, networkSlot1Qty.Value);
            networkSlot1Qty.OnValueChanged += (prev, curr) => UpdateLocalInventory(1, networkSlot1Type.Value, curr);
        }
    }
    
    private void UpdateLocalInventory(int slotIndex, int typeInt, int quantity)
    {
        if (slotIndex >= maxSlots) return;
        
        inventorySlots[slotIndex].SetItem((ItemType)typeInt, quantity);
        
        if (quantity == 0)
        {
            inventorySlots[slotIndex].Clear();
        }
    }
}
