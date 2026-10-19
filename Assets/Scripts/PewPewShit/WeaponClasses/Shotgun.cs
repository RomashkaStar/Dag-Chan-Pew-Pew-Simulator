using UnityEngine;

public class Shotgun : Weapon
{
    [Header ("Shotgun Settings")]
    [SerializeField, Min (1)] private int _MaxAmmo = 24;
    public  override int MaxAmmo => Mathf.Max(1, _MaxAmmo);
}
