using UnityEngine;

public class Shotgun : Weapon
{
    [Header("Shotgun Settings")]

    [SerializeField, Min(1)]
    private int _MaxAmmo = 12;

    // Урон одного выстрела дробовика.
    [SerializeField, Min(0f)]
    private float _Damage = 50f;

    public override int MaxAmmo => Mathf.Max(1, _MaxAmmo);

    public override float Damage => Mathf.Max(0f, _Damage);
}