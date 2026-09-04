using UnityEngine;

/// <summary>
/// Перечисление состояний здоровья выжившего.
/// </summary>
public enum SurvivorHealthState
{
    Healthy,    // Полное здоровье, нормальная скорость
    Injured,    // Ранен, замедлен, оставляет следы крови
    Downed      // Обездвижен, может только ползти
}

/// <summary>
/// Перечисление состояний персонажа (используется для State Machine).
/// </summary>
public enum CharacterState
{
    Idle,           // Бездействие
    Moving,         // Перемещение
    Running,        // Бег
    Interacting,    // Взаимодействие с объектом
    Repairing,      // Починка объекта
    Hooked,         // Захвачен крюком маньяка
    Attacking,      // Атака (для маньяка)
    UsingItem,      // Использование предмета
    Dead            // Мертв (для выжившего)
}
