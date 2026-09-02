# Архитектура и Руководство по Реализации: Island of Terror

## 1. Обзор Архитектуры

### 1.1. Структура Проекта
```
Assets/
├── Scripts/
│   ├── Core/                 # Основные системы
│   │   ├── GameEnums.cs      # Перечисления состояний
│   │   ├── GameManager.cs    # Центральный менеджер игры
│   │   ├── IInteractable.cs  # Интерфейс интерактивных объектов
│   │   └── ItemType.cs       # Типы предметов
│   ├── Characters/           # Базовые классы персонажей
│   │   └── BaseCharacter.cs  # Базовый класс для всех персонажей
│   ├── Maniac/               # Логика маньяка
│   │   └── ManiacController.cs
│   ├── Survivor/             # Логика выживших
│   │   ├── SurvivorHealth.cs
│   │   └── SurvivorInventory.cs
│   ├── Interactables/        # Интерактивные объекты
│   │   ├── BaseEscapeObjective.cs
│   │   ├── BoatEscape.cs
│   │   ├── ZiplineEscape.cs
│   │   └── BridgeRepair.cs
│   ├── UI/                   # Пользовательский интерфейс
│   └── Network/              # Сетевые компоненты
├── Prefabs/                  # Префабы
├── Scenes/                   # Сцены
└── Art/, Audio/, Settings/   # Ресурсы
```

### 1.2. Ключевые Паттерны Проектирования

#### State Machine (Конечный Автомат)
Используется для управления состояниями персонажей:
- `CharacterState` enum определяет все возможные состояния
- `BaseCharacter` реализует базовую логику переключения состояний
- Каждое состояние имеет уникальное поведение (Idle, Moving, Running, Interacting, etc.)

#### Interface Pattern (IInteractable)
Единый интерфейс для всех интерактивных объектов:
- `CanInteract()` - проверка возможности взаимодействия
- `Interact()` - выполнение действия
- `GetInteractionPrompt()` - получение подсказки для UI

#### NetworkVariable (Сетевая Синхронизация)
Все критические данные синхронизируются через NetworkVariables:
- Состояния здоровья и инвентаря
- Прогресс выполнения задач
- Состояние матча

---

## 2. Детальное Описание Компонентов

### 2.1. BaseCharacter.cs
**Назначение:** Базовый класс для всех управляемых персонажей.

**Основные возможности:**
- Управление перемещением через CharacterController
- Система состояний (State Machine)
- Сетевая синхронизация позиций и состояний
- Базовое взаимодействие с объектами

**Абстрактные методы (требуют реализации в наследниках):**
```csharp
protected abstract void InitializeInput();  // Инициализация ввода
protected abstract void HandleInput();      // Обработка ввода
protected abstract bool IsRunning();        // Проверка бега
public abstract void TakeDamage(...);       // Получение урона
```

**Пример расширения для PC:**
```csharp
public class SurvivorPC : BaseCharacter
{
    protected override void InitializeInput()
    {
        // Настройка Input System для PC
    }
    
    protected override void HandleInput()
    {
        moveDirection = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        ).normalized;
    }
    
    protected override bool IsRunning()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }
}
```

### 2.2. SurvivorHealth.cs
**Назначение:** Управление здоровьем и состояниями выжившего.

**Состояния здоровья:**
1. **Healthy** - полное здоровье, нормальная скорость
2. **Injured** - <50% здоровья, -15% скорости, следы крови
3. **Downed** - 0 здоровья, только ползание, требует помощи

**Ключевые методы:**
```csharp
[ServerRpc]
public void TakeDamageServerRpc(float damage, DamageSource source)
// Нанесение урона с проверкой порогов

[ServerRpc]
public void HealServerRpc(float amount)
// Лечение с переходом между состояниями

public bool CanHelpOthers()
// Проверка может ли выживший помогать другим
```

### 2.3. ManiacController.cs
**Назначение:** Уникальные способности маньяка.

**Способности:**
1. **Бензопила:**
   - Активация/деактивация через `ToggleChainsawServerRpc()`
   - Атака по области через `ChainsawAttackServerRpc()`
   - Увеличивает радиус террора с 20м до 30м

2. **Крюк-гарпун:**
   - Бросок на расстояние до 15м через `ThrowHookServerRpc()`
   - Притягивание выжившего со скоростью 8 м/с
   - Перезарядка 3 секунды

**Радиус террора:**
- Динамически меняется в зависимости от активности бензопилы
- Используется для звуковых предупреждений выжившим

### 2.4. SurvivorInventory.cs
**Назначение:** Управление предметами выжившего.

**Характеристики:**
- 2 слота для предметов
- Поддержка стака одинаковых предметов
- Сетевая синхронизация содержимого слотов

**Типы предметов:**
```csharp
enum ItemType
{
    None,           // Пусто
    IgnitionKey,    // Ключ для катера
    RopeCoil,       // Трос для зиплайна
    ToolBox,        // Инструменты для моста
    MedKit,         // Лечение (+50 HP)
    Flashlight      // Освещение
}
```

### 2.5. BaseEscapeObjective.cs
**Назначение:** Базовый класс для всех путей эвакуации.

**Общая логика:**
- Проверка требований (предметы, состояние здоровья)
- Прогресс-бар взаимодействия
- Возможность прерывания
- Сетевая синхронизация прогресса

**Наследники:**
- `BoatEscape` - побег на катере (требуется ключ)
- `ZiplineEscape` - зиплайн (требуется трос + QTE)
- `BridgeRepair` - ремонт моста (требуется ящик инструментов)

### 2.6. GameManager.cs
**Назначение:** Управление матчем.

**Функции:**
- Отсчет времени матча (15 минут)
- Подсчет сбежавших и устраненных выживших
- Определение победителя
- Спавн персонажей

**Условия победы:**
- **Выжившие:** Хотя бы 1 выживший сбежал ИЛИ время вышло
- **Маньяк:** Все выжившие устранены

---

## 3. Сетевая Архитектура

### 3.1. Модель Авторитарного Сервера
- Сервер является источником истины для всех игровых данных
- Клиенты отправляют запросы через ServerRpc
- Сервер рассылает обновления через NetworkVariables и ClientRpc

### 3.2. Синхронизация Данных
```csharp
// Пример NetworkVariable
private NetworkVariable<CharacterState> networkState = 
    new NetworkVariable<CharacterState>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );
```

### 3.3. RPC Вызовы
```csharp
// Запрос клиента к серверу
[ServerRpc(RequireOwnership = false)]
public void TakeDamageServerRpc(float damage, DamageSource source)

// Ответ сервера всем клиентам
[ClientRpc]
private void EndMatchClientRpc()
```

---

## 4. Система Управления

### 4.1. PC Управление
```
WASD          - Перемещение
Mouse         - Камера
Left Shift    - Бег
E             - Взаимодействие
1-2           - Использование предметов
ЛКМ           - Основное действие
ПКМ           - Прицеливание
Ctrl          - Приседание
```

### 4.2. Mobile Управление
- **Левый стик:** Перемещение (плавающий)
- **Правая зона:** Поворот камеры (динамическая)
- **Контекстные кнопки:** Появляются near объектов
- **Кнопки действий:** Атака, Бег, Предметы

### 4.3. Реализация для Mobile
```csharp
public class SurvivorMobile : BaseCharacter
{
    private Joystick movementJoystick;
    private VirtualButton interactButton;
    
    protected override void InitializeInput()
    {
        movementJoystick = FindObjectOfType<FloatingJoystick>();
        interactButton = GetComponent<VirtualButton>();
    }
    
    protected override void HandleInput()
    {
        moveDirection = new Vector3(
            movementJoystick.Horizontal,
            0,
            movementJoystick.Vertical
        ).normalized;
    }
}
```

---

## 5. Оптимизация для Мобильных Платформ

### 5.1. Графика
- **LOD Groups:** 3 уровня детализации для всех моделей
- **Occlusion Culling:** Предварительный расчет для зданий
- **Lightmaps:** Запеченное освещение для статики
- **Texture Compression:** ASTC 4x4 для мобильных

### 5.2. Производительность
- **Target FPS:** 30 FPS на мобильных, 60 FPS на PC
- **Draw Calls:** <100 на мобильных через батчинг
- **Polygon Count:** <50K треугольников в кадре

### 5.3. Память
- **Texture Size:** Max 1024x1024 для мобильных
- **Audio:** OGG Vorbis, моно для SFX
- **Object Pooling:** Для частиц и эффектов

---

## 6. Этапы Реализации

### Фаза 1: Базовый Прототип (Недели 1-2)
1. ✅ Создать структуру проекта
2. ✅ Реализовать `BaseCharacter` с перемещением
3. ✅ Добавить `IInteractable` интерфейс
4. ✅ Создать один путь эвакуации (Катер)
5. ⬜ Тестирование локального мультиплеера

### Фаза 2: Сетевая Инфраструктура (Недели 3-5)
1. ⬜ Интеграция Unity Netcode for GameObjects
2. ⬜ Синхронизация перемещений персонажей
3. ⬜ Реализация `GameManager` для матча
4. ⬜ Система лобби и подключение игроков
5. ⬜ Тестирование клиент-сервер взаимодействия

### Фаза 3: Полировка Механик (Недели 6-9)
1. ⬜ Все три пути эвакуации (Катер, Зиплайн, Мост)
2. ⬜ Полная система здоровья выживших
3. ⬜ Способности маньяка (Бензопила, Крюк)
4. ⬜ Инвентарь и предметы
5. ⬜ Spawn лута на карте

### Фаза 4: Контент и Оптимизация (Недели 10-13)
1. ⬜ Создание карты острова
2. ⬜ Моделирование зданий и декора
3. ⬜ Настройка освещения и пост-эффектов
4. ⬜ Оптимизация под мобильные платформы
5. ⬜ Звуковое оформление

### Фаза 5: Тестирование и Релиз (Недели 14-16)
1. ⬜ Закрытое бета-тестирование
2. ⬜ Балансировка геймплея
3. ⬜ Исправление багов
4. ⬜ Подготовка к релизу в магазинах

---

## 7. Расширяемость

### Добавление Нового Персонажа
```csharp
// 1. Создать наследника BaseCharacter
public class NewSurvivor : BaseCharacter
{
    // Уникальная способность
    public void SpecialAbility()
    {
        // ...
    }
    
    // Переопределить методы ввода
    protected override void HandleInput() { }
    protected override bool IsRunning() { }
}

// 2. Добавить уникальный компонент
public class NewSurvivorAbility : NetworkBehaviour
{
    // Логика способности
}
```

### Добавление Нового Объекта Эвакуации
```csharp
// 1. Создать наследника BaseEscapeObjective
public class TunnelEscape : BaseEscapeObjective
{
    protected override void CompleteObjective()
    {
        base.CompleteObjective();
        // Уникальная логика завершения
    }
    
    public override string GetInteractionPrompt()
    {
        return "Открыть тоннель";
    }
}
```

---

## 8. Заключение

Данная архитектура обеспечивает:
- ✅ **Модульность:** Легкое добавление нового контента
- ✅ **Сетевую надежность:** Авторитарный сервер предотвращает читы
- ✅ **Кроссплатформенность:** Единая кодовая база для PC и Mobile
- ✅ **Производительность:** Оптимизация под слабые устройства
- ✅ **Расширяемость:** Паттерны позволяют быстро добавлять механики

**Следующие шаги:**
1. Интегрировать Unity Netcode for GameObjects
2. Реализовать конкретные классы управления для PC/Mobile
3. Создать префабы персонажей и объектов
4. Настроить сцену с островом
