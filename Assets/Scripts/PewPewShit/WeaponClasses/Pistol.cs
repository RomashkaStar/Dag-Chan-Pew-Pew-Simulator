using UnityEngine;

public class Pistol : Weapon
{
    [Header("Pistol Settings")]

    [SerializeField, Min(1)]
    private int _MaxAmmo = 24;

    // Урон одного выстрела пистолета.
    [SerializeField, Min(0f)]
    private float _Damage = 20f;

    public override int MaxAmmo => Mathf.Max(1, _MaxAmmo);

    public override float Damage => Mathf.Max(0f, _Damage);
}