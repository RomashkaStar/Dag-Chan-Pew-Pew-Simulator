using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmmoPickUp : MonoBehaviour
{
    [Header("AmmoPickup")]
    [SerializeField] private int _AmmoAmount = 10;

    private void OnTriggerEnter(Collider other) {
        CharacterController player = other.GetComponentInParent<CharacterController>();

        //Если Character Controller не найден, то значит объекта коснулся не игрок.
        if (player == null) {
            return;
        }

        //Ищем WeaponShooter на игроке
        WeaponShooter shooter = player.GetComponentInChildren<WeaponShooter>(true);
        if (shooter == null) {
            return;
        }

        if (shooter.TryPickUpAmmo(_AmmoAmount)) {
            Destroy(gameObject);
        }
    }
}
