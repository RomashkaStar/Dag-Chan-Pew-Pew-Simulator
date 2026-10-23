
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Pickup")]

    // Количество патронов, которое даёт коробка.
    [SerializeField] private int _AmmoAmount = 10;


    [Header("Audio")]

    // Звук успешного подбора.
    [SerializeField] private AudioClip _PickupSound;

    // Громкость звука.
    [SerializeField, Range(0f, 1f)]
    private float _PickupVolume = 1f;


    private void OnTriggerEnter(Collider other)
    {
        // Ищем CharacterController игрока.
        CharacterController player =
            other.GetComponentInParent<CharacterController>();

        if (player == null)
        {
            return;
        }

        // Ищем систему оружия игрока.
        WeaponShooter shooter =
            player.GetComponentInChildren<WeaponShooter>(true);

        if (shooter == null)
        {
            return;
        }

        // Пытаемся добавить патроны.
        // Если патроны уже на максимуме,
        // метод вернёт false.
        if (shooter.TryPickUpAmmo(_AmmoAmount))
        {
            // Патроны успешно подобраны!

            // Проверяем, есть ли звуковой файл.
            if (_PickupSound != null)
            {
                // Воспроизводим звук в позиции коробки.
                AudioSource.PlayClipAtPoint(
                    _PickupSound,
                    transform.position,
                    _PickupVolume
                );
            }

            // Уничтожаем коробку.
            Destroy(gameObject);
        }
    }
}
