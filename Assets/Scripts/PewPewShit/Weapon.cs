using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [Header("Sprites")]

    // Обычный спрайт оружия.
    [SerializeField] protected Sprite _IdleSprite;

    // Спрайты анимации выстрела.
    [SerializeField] protected Sprite[] _FireSprites;

    // Продолжительность одного кадра анимации.
    [SerializeField] protected float _FrameTime = 0.05f;


    [Header("Audio")]

    // Звуковой файл выстрела.
    [SerializeField] protected AudioClip _FireSound;


    // Позволяем другим скриптам получать
    // данные выбранного оружия.
    public Sprite IdleSprite => _IdleSprite;

    public Sprite[] FireSprites => _FireSprites;

    public float FrameTime => _FrameTime;

    // Теперь можно запрашивать ещё и звук.
    public AudioClip FireSound => _FireSound;
}