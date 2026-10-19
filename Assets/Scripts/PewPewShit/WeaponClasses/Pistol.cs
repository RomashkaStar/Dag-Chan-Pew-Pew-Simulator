using UnityEngine;

public class Pistol : Weapon
{
    [Header ("Pistol Settings")]
    [SerializeField, Min (1)] private int _MaxAmmo = 52;
    public  override int MaxAmmo => Mathf.Max(1, _MaxAmmo);
}