using UnityEngine;

/// <summary>
/// Интерфейс для всех интерактивных объектов в игре.
/// Используется выжившими и маньяком для взаимодействия с окружением.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Вызывается при взаимодействии с объектом.
    /// </summary>
    /// <param name="interactor">Персонаж, который взаимодействует с объектом.</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// Возвращает подсказку о действии, которое можно выполнить с объектом.
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Проверяет, может ли персонаж взаимодействовать с объектом в данный момент.
    /// </summary>
    bool CanInteract(GameObject interactor);
}
