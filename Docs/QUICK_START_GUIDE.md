# 🚀 Quick Start Guide - Survival Horror Project

## ⚠️ ВАЖНО: Установка пакетов перед запуском

Этот проект использует **Unity Netcode for GameObjects** для сетевой игры. Пакеты устанавливаются автоматически при открытии проекта в Unity.

---

## 📦 Шаг 1: Открытие проекта в Unity

1. Запустите **Unity Hub**
2. Нажмите **Add** → **Add project from disk**
3. Выберите папку `/workspace`
4. Откройте проект в **Unity Editor**
5. Дождитесь автоматической установки пакетов (индикатор в правом нижнем углу)

Unity автоматически обнаружит файл `Packages/manifest.json` и установит:
- `com.unity.netcode.gameobjects@1.7.0`
- `com.unity.transport@1.4.0`

---

## ✅ Шаг 2: Проверка установки

После импорта пакетов:

1. Откройте **Console** (Window → General → Console)
2. Убедитесь, что нет ошибок компиляции

Если видите ошибки вида:
- `CS0234: The type or namespace name 'Netcode' does not exist`
- `CS0246: The type or namespace name 'NetworkBehaviour' could not be found`

**Решение:** Закройте Unity и откройте проект заново.

---

## 🎮 Шаг 3: Создание префабов

Следуйте инструкции в файле `Docs/Prefab_Creation_Guide.md` для создания префабов:

### Префабы выживших:
- `SurvivorPC_Prefab` (для PC)
- `SurvivorMobile_Prefab` (для Mobile)

### Префабы маньяка:
- `ManiacPC_Prefab` (для PC)
- `ManiacMobile_Prefab` (для Mobile)

### Требования к префабу:
1. Добавьте компонент `CharacterController`
2. Добавьте соответствующий контроллер (`SurvivorPC`, `SurvivorMobile`, etc.)
3. Добавьте компонент `NetworkObject`
4. Для выжившего добавьте `SurvivorHealth` и `SurvivorInventory`
5. Для маньяка добавьте `ManiacController`

---

## 🌐 Шаг 4: Настройка NetworkManager

1. Откройте сцену `Assets/Scenes/IslandMap.unity`
2. Найдите объект **NetworkManager** (или создайте пустой объект с этим именем)
3. Добавьте компонент **NetworkManager** из пакета Netcode
4. В секции **Prefabs** добавьте все 4 префаба персонажей
5. Настройте **Transport** (по умолчанию используется UnityTransport)

---

## 🕹️ Шаг 5: Тестирование

### Локальный мультиплеер тест:

1. В меню Unity: **Tools → Netcode for GameObjects → Test → Launch Multi-Instance Test**
2. Запустится несколько окон Unity
3. В первом окне нажмите **Host** (сервер + игрок)
4. В остальных окнах нажмите **Client**

### Ручной тест:

1. Создайте сборку (**File → Build Settings → Build**)
2. Запустите несколько экземпляров сборки
3. В одном запустите сервер, в остальных - клиенты

---

## 📱 Мобильная сборка

### Android:

1. **File → Build Settings → Switch Platform → Android**
2. **Player Settings**:
   - Minimum API Level: Android 7.0 (API Level 24)
   - Scripting Backend: IL2CPP
   - Target Architect: ARM64
3. **Build And Run**

### iOS:

1. **File → Build Settings → Switch Platform → iOS**
2. **Player Settings**:
   - Minimum iOS Version: 12.0
   - Scripting Backend: IL2CPP
   - Architecture: ARM64
3. **Build** и откройте в Xcode

---

## 🔧 Решение проблем

### Ошибка: `CS0234: The type or namespace name 'Netcode' does not exist`

**Причина:** Пакеты не установлены или не загружены

**Решение:**
1. Проверьте **Window → Package Manager**
2. Убедитесь, что пакеты `Netcode for GameObjects` и `Unity Transport` установлены
3. Перезапустите Unity Editor
4. Если не помогло - удалите папку `Library` и откройте проект заново

### Ошибка: `Package [com.unity.modules.physx] cannot be found`

**Причина:** PhysX встроен в Unity, отдельный пакет не нужен

**Решение:** Файл `manifest.json` уже настроен правильно, эта ошибка не должна возникать.

### Ошибка: `Platform name '' not supported`

**Причина:** Неправильные `.asmdef` файлы

**Решение:** Все `.asmdef` файлы были удалены из проекта. Unity будет использовать стандартную сборку.

---

## 📁 Структура скриптов

```
Assets/Scripts/
├── Characters/
│   └── BaseCharacter.cs           # Базовый класс персонажа
├── Controllers/
│   ├── SurvivorPC.cs              # Управление выжившим на PC
│   ├── SurvivorMobile.cs          # Управление выжившим на Mobile
│   ├── ManiacPC.cs                # Управление маньяком на PC
│   └── ManiacMobile.cs            # Управление маньяком на Mobile
├── Core/
│   ├── GameManager.cs             # Менеджер матча
│   ├── IInteractable.cs           # Интерфейс взаимодействий
│   ├── GameEnums.cs               # Перечисления
│   └── ItemType.cs                # Типы предметов
├── Input/
│   ├── PlayerInputHandler.cs      # Базовый класс ввода
│   ├── PCInputHandler.cs          # Ввод с клавиатуры/мыши
│   └── MobileInputHandler.cs      # Сенсорный ввод
├── Interactables/
│   ├── BaseEscapeObjective.cs     # База для путей эвакуации
│   ├── BoatEscape.cs              # Побег на катере
│   ├── ZiplineEscape.cs           # Зиплайн
│   └── BridgeRepair.cs            # Ремонт моста
├── Lobby/
│   └── LobbyManager.cs            # Система лобби
├── Maniac/
│   └── ManiacController.cs        # Способности маньяка
└── Survivor/
    ├── SurvivorHealth.cs          # Система здоровья
    └── SurvivorInventory.cs       # Инвентарь
```

---

## 🎯 Чеклист готовности

- [ ] Пакеты Unity Netcode установлены
- [ ] Нет ошибок компиляции в Console
- [ ] Префабы персонажей созданы
- [ ] NetworkManager настроен
- [ ] Префабы зарегистрированы в NetworkManager
- [ ] Тест локального мультиплеера работает
- [ ] Сборка для целевой платформы создана

---

## 📞 Поддержка

**Версии пакетов:**
- Unity Netcode for GameObjects: **1.7.0**
- Unity Transport: **1.4.0**
- Рекомендуемая версия Unity: **2021.3 LTS** или новее

**Полезные ссылки:**
- Документация Netcode: https://docs-multiplayer.unity3d.com/netcode/current/about/
- Примеры: https://github.com/Unity-Technologies/com.unity.netcode.gameobjects
