
using UnityEngine;

// —писок направлений, с которых игрок может видеть врага.
public enum EnemyViewDirection
{
    Front,
    Back,
    Left,
    Right
}

public abstract class Enemy : MonoBehaviour
{
    [Header("Front Sprites")]

    // ќбычный спрайт спереди.
    [SerializeField] protected Sprite _FrontSprite;

    // —прайт шага спереди.
    [SerializeField] protected Sprite _FrontStepSprite;


    [Header("Back Sprites")]

    [SerializeField] protected Sprite _BackSprite;
    [SerializeField] protected Sprite _BackStepSprite;


    [Header("Left Sprites")]

    [SerializeField] protected Sprite _LeftSprite;
    [SerializeField] protected Sprite _LeftStepSprite;


    [Header("Right Sprites")]

    [SerializeField] protected Sprite _RightSprite;
    [SerializeField] protected Sprite _RightStepSprite;


    [Header("Walking Animation")]

    // ѕродолжительность одного кадра ходьбы.
    [SerializeField] protected float _FrameTime = 0.15f;

    // ћинимальна€ скорость, при которой
    // считаем, что враг движетс€.
    [SerializeField] protected float _MovementThreshold = 0.05f;


    // –азрешаем другим скриптам читать настройки.
    // Ќо напр€мую мен€ть их они не смогут.
    public float FrameTime => Mathf.Max(0.01f, _FrameTime);

    public float MovementThreshold =>
        Mathf.Max(0f, _MovementThreshold);


    // Ётот метод возвращает нужный спрайт.
    //
    // direction Ч с какой стороны игрок видит врага.
    // isStep Ч нужно ли показать кадр шага.
    public Sprite GetSprite(
        EnemyViewDirection direction,
        bool isStep
    )
    {
        Sprite idleSprite = null;
        Sprite stepSprite = null;


        // ¬ыбираем пару спрайтов
        // в зависимости от направлени€.
        switch (direction)
        {
            case EnemyViewDirection.Front:
                idleSprite = _FrontSprite;
                stepSprite = _FrontStepSprite;
                break;

            case EnemyViewDirection.Back:
                idleSprite = _BackSprite;
                stepSprite = _BackStepSprite;
                break;

            case EnemyViewDirection.Left:
                idleSprite = _LeftSprite;
                stepSprite = _LeftStepSprite;
                break;

            case EnemyViewDirection.Right:
                idleSprite = _RightSprite;
                stepSprite = _RightStepSprite;
                break;
        }


        // ≈сли сейчас кадр шага
        // и соответствующий спрайт существует,
        // возвращаем его.
        if (isStep && stepSprite != null)
        {
            return stepSprite;
        }

        // »наче возвращаем обычный спрайт.
        return idleSprite;
    }
}
