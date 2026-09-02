using UnityEngine;

/// <summary>
/// Перечисление типов предметов в инвентаре.
/// </summary>
public enum ItemType
{
    None,
    IgnitionKey,      // Ключ зажигания для катера
    RopeCoil,         // Бухта троса для зиплайна
    ToolBox,          // Ящик с инструментами для моста
    MedKit,           // Аптечка для лечения
    Flashlight        // Фонарик для освещения
}

/// <summary>
/// Система инвентаря выжившего.
/// Управляет предметами, их использованием и передачей другим игрокам.
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemType itemType;
    public int quantity = 1;
    
    public bool IsEmpty => itemType == ItemType.None;
    
    public void Clear()
    {
        itemType = ItemType.None;
        quantity = 0;
    }
    
    public void SetItem(ItemType type, int qty = 1)
    {
        itemType = type;
        quantity = qty;
    }
}
