# Руководство по созданию префабов персонажей и объектов

## 1. Префабы персонажей

### Survivor Prefab (Выживший)

#### Структура префаба:
```
Survivor_Prefab (GameObject)
├── NetworkObject (компонент)
├── CharacterController (компонент)
├── BaseCharacter (скрипт)
├── SurvivorHealth (скрипт)
├── SurvivorInventory (скрипт)
├── PCInputHandler / MobileInputHandler (скрипт)
├── SurvivorPC / SurvivorMobile (скрипт)
├── Model
│   └── [3D модель персонажа]
├── CameraPivot
│   └── Main Camera
└── BloodTrailParticleSystem
```

#### Настройка компонентов:

**NetworkObject:**
- Scene Migration: ✓
- DontDestroyWithOwner: ✗
- OwnerShipMode: Dynamic

**CharacterController:**
- Height: 2.0
- Radius: 0.4
- Slope Limit: 45
- Step Offset: 0.3

**BaseCharacter:**
- Move Speed: 5.0
- Sprint Multiplier: 1.6
- Look Sensitivity: 2.0

**SurvivorHealth:**
- Max Health: 100
- Bleed Effect: BloodTrailParticleSystem

---

### Maniac Prefab (Маньяк)

#### Структура префаба:
```
Maniac_Prefab (GameObject)
├── NetworkObject (компонент)
├── CharacterController (компонент)
├── BaseCharacter (скрипт)
├── ManiacController (скрипт)
├── PCInputHandler / MobileInputHandler (скрипт)
├── ManiacPC / ManiacMobile (скрипт)
├── Model
│   └── [3D модель маньяка]
│       ├── ChainsawEffect
│       └── HookProjectile
├── CameraPivot
│   └── Main Camera
└── TerrorRadiusAudioSource
```

#### Настройка компонентов:

**ManiacController:**
- Chainsaw Damage: 100
- Hook Range: 15.0
- Hook Pull Speed: 8.0
- Chainsaw Cooldown: 5.0
- Terror Radius: 20.0

---

## 2. Префабы объектов эвакуации

### Boat Escape Prefab (Катер)

```
BoatEscape_Prefab
├── NetworkObject
├── BoatEscape (скрипт)
├── BoxCollider (триггер)
├── EngineSound (AudioSource)
├── StartPrompt (UI Canvas - World Space)
└── ProgressBar (UI Slider)
```

**Настройка BoatEscape:**
- Interaction Distance: 3.0
- Required Item: IgnitionKey
- Escape Time: 15.0
- Escape Point: Transform (у катера)

---

### Zipline Escape Prefab (Зиплайн)

```
ZiplineEscape_Prefab
├── NetworkObject
├── ZiplineEscape (скрипт)
├── BoxCollider (триггер)
├── CableLine (LineRenderer)
├── StartPlatform
├── EndPlatform
├── AttachmentPoint (Transform)
└── ProgressIndicator (UI)
```

**Настройка ZiplineEscape:**
- Interaction Distance: 2.0
- Required Item: RopeSpool
- QTE Difficulty: Medium
- Success Angle Range: 30°

---

### Bridge Repair Prefab (Мост)

```
BridgeRepair_Prefab
├── NetworkObject
├── BridgeRepair (скрипт)
├── BoxCollider (триггер)
├── BrokenBridgeModel
├── RepairedBridgeModel (скрыт изначально)
├── ToolBoxSpawnPoint (Transform)
└── StageProgressIndicators (3x UI)
```

**Настройка BridgeRepair:**
- Interaction Distance: 2.5
- Required Item: ToolBox
- Repair Stages: 3
- Stage Time: 10.0 каждый

---

## 3. Префабы предметов (Items)

### Ignition Key (Ключ зажигания)

```
IgnitionKey_Prefab
├── NetworkObject
├── ItemPickup (скрипт)
├── MeshFilter/MeshRenderer
├── BoxCollider (триггер)
└── GlowEffect (Light + ParticleSystem)
```

**ItemPickup:**
- Item Type: KeyItem
- Slot Required: 1
- Respawn Time: 0 (одноразовый)

---

### Rope Spool (Бухта троса)

```
RopeSpool_Prefab
├── NetworkObject
├── ItemPickup (скрипт)
├── MeshFilter/MeshRenderer
├── BoxCollider (триггер)
└── GlowEffect
```

**ItemPickup:**
- Item Type: RopeSpool
- Slot Required: 1

---

### Tool Box (Ящик инструментов)

```
ToolBox_Prefab
├── NetworkObject
├── ItemPickup (скрипт)
├── MeshFilter/MeshRenderer
├── BoxCollider (триггер)
└── GlowEffect
```

**ItemPickup:**
- Item Type: ToolBox
- Slot Required: 1

---

## 4. Префабы лута (Loot Containers)

### Chest (Сундук)

```
Chest_Prefab
├── NetworkObject
├── LootContainer (скрипт)
├── Interactable (скрипт)
├── BoxCollider
├── ClosedModel
├── OpenModel (скрыт)
└── LootTable (ScriptableObject reference)
```

**LootContainer:**
- Possible Items: [IgnitionKey, RopeSpool, ToolBox, MedKit]
- Key Drop Chance: 25%
- Already Looter: NetworkVariable<bool>

---

### Building Spawn Points (Точки спавна лута в зданиях)

```
LootSpawnPoint_Prefab
├── NetworkObject
├── LootSpawner (скрипт)
└── DebugVisual (сфера радиусом 0.5м)
```

**LootSpawner:**
- Spawn Delay: 0 (спавн при старте матча)
- Possible Items: [Random loot table]

---

## 5. Инструкция по созданию префаба в Unity

### Шаг 1: Создание базового GameObject
1. В иерархии: `Right Click > Create Empty`
2. Назвать префаб (например, "Survivor_Prefab")
3. Перетащить в папку `Assets/Prefabs/Characters/`

### Шаг 2: Добавление сетевых компонентов
1. Добавить компонент `NetworkObject`
2. Настроить параметры (см. выше)

### Шаг 3: Добавление физических компонентов
1. Добавить `CharacterController`
2. Настроить размеры коллайдера

### Шаг 4: Добавление скриптов
1. Перетащить скрипты из `Assets/Scripts/`
2. Настроить публичные поля в Inspector

### Шаг 5: Создание модели персонажа
1. Импортировать 3D модель (.fbx, .obj)
2. Перетащить как дочерний объект
3. Настроить материалы и анимации

### Шаг 6: Настройка камеры
1. Создать пустой GameObject "CameraPivot"
2. Поместить на уровне головы персонажа
3. Добавить Main Camera как дочерний
4. Настроить Field of View (60-75°)

### Шаг 7: Сохранение префаба
1. Выбрать корневой GameObject
2. `Right Click > Prefab > Create Prefab`
3. Заменить существующий или создать новый

---

## 6. Регистрация префабов в NetworkManager

1. Выбрать GameObject с `NetworkManager`
2. В Inspector найти поле `NetworkConfig > Prefabs`
3. Нажать `+` для добавления нового префаба
4. Перетащить префаб в поле `Source`
5. Повторить для всех префабов:
   - Survivor_Prefab_PC
   - Survivor_Prefab_Mobile
   - Maniac_Prefab_PC
   - Maniac_Prefab_Mobile
   - BoatEscape_Prefab
   - ZiplineEscape_Prefab
   - BridgeRepair_Prefab
   - IgnitionKey_Prefab
   - RopeSpool_Prefab
   - ToolBox_Prefab

---

## 7. Оптимизация префабов для мобильных устройств

### LOD Groups
1. Добавить компонент `LODGroup` к модели
2. Создать 3 уровня детализации:
   - LOD0: 100% полигонов (0-20м)
   - LOD1: 50% полигонов (20-50м)
   - LOD2: 25% полигонов (50м+)
3. Настроить переходы между уровнями

### Упрощенные коллайдеры
- Использовать примитивные коллайдеры вместо MeshCollider
- Комбинировать несколько простых коллайдеров

### Оптимизация материалов
- Использовать атласы текстур
- Минимизировать количество материалов
- Использовать мобильные шейдеры

### Particle Systems
- Ограничить максимальное количество частиц
- Использовать простые текстуры частиц
- Отключить сложные симуляции

---

Это руководство обеспечит правильное создание и настройку всех необходимых префабов для проекта.
