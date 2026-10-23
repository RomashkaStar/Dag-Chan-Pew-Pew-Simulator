
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class EnemyRocketProjectile : MonoBehaviour, IDamageable
{
    [Header("Movement")]

    [SerializeField, Min(0.1f)]
    private float _Speed = 4f;

    // Скорость самонаведения в градусах за секунду.
    // 0 = ракета не корректирует направление после запуска.
    [SerializeField, Min(0f)]
    private float _HomingTurnSpeed = 540f;

    // Ракета взрывается, если приблизилась
    // к поверхности коллайдера игрока на это расстояние.
    [SerializeField, Min(0f)]
    private float _ProximityDistance = 0.35f;

    [SerializeField, Min(0.1f)]
    private float _Lifetime = 8f;


    [Header("Health And Damage")]

    // Урон по игроку.
    [SerializeField, Min(0f)]
    private float _Damage = 15f;

    // Здоровье самой ракеты.
    [SerializeField, Min(0.1f)]
    private float _MaxHealth = 20f;


    [Header("Collision")]

    // Включи Player и слои препятствий.
    [SerializeField]
    private LayerMask _CollisionLayers = ~0;


    [Header("Explosion")]

    [SerializeField]
    private GameObject _ExplosionPrefab;

    [SerializeField]
    private AudioClip _ExplosionSound;

    [SerializeField, Range(0f, 1f)]
    private float _ExplosionVolume = 1f;


    [Header("Debug")]

    [SerializeField]
    private bool _DebugHits = false;


    private Rigidbody _Rigidbody;
    private SphereCollider _SphereCollider;

    private Transform _Target;
    private Transform _Owner;

    // Физический коллайдер игрока.
    private Collider _TargetCollider;

    // Здоровье игрока.
    private PlayerHealth _TargetHealth;

    private Vector3 _Direction;

    private float _CurrentHealth;
    private float _DeathTime;

    private bool _Exploded;


    private void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _SphereCollider = GetComponent<SphereCollider>();

        _Rigidbody.isKinematic = true;
        _Rigidbody.useGravity = false;

        // ВАЖНО: физический коллайдер ракеты
        // не должен быть Trigger.
        _SphereCollider.isTrigger = false;

        _CurrentHealth = Mathf.Max(0.1f, _MaxHealth);

        _Direction = transform.forward;

        _DeathTime = Time.time + _Lifetime;
    }


    // ------------------------------------------
    // ЗАПУСК РАКЕТЫ
    // ------------------------------------------

    public void Launch(Transform target, Transform owner)
    {
        _Target = target;
        _Owner = owner;

        if (_Target != null)
        {
            // Ищем настоящий CharacterController игрока.
            CharacterController controller =
                _Target.GetComponentInChildren<CharacterController>();

            // Если он существует, используем его.
            if (controller != null)
            {
                _TargetCollider = controller;
            }
            else
            {
                // Запасной вариант для других целей.
                _TargetCollider =
                    _Target.GetComponentInChildren<Collider>();
            }

            // Ищем компонент здоровья игрока.
            _TargetHealth =
                _Target.GetComponentInChildren<PlayerHealth>();

            if (_TargetHealth == null)
            {
                Debug.LogWarning(
                    "РАКЕТА: у игрока не найден PlayerHealth!",
                    this
                );
            }

            // Первоначально направляем ракету
            // в реальный центр игрока.
            Vector3 direction =
                GetAimPosition() - transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _Direction = direction.normalized;
            }
        }

        if (_Direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(_Direction);
        }
    }


    // ------------------------------------------
    // ТОЧКА ПРИЦЕЛИВАНИЯ
    // ------------------------------------------

    private Vector3 GetAimPosition()
    {
        // Центр реального коллайдера игрока.
        if (_TargetCollider != null &&
            _TargetCollider.enabled)
        {
            return _TargetCollider.bounds.center;
        }

        // Запасной вариант.
        if (_Target != null)
        {
            return _Target.position + Vector3.up;
        }

        return transform.position + _Direction;
    }


    // ------------------------------------------
    // ФИЗИЧЕСКОЕ ДВИЖЕНИЕ
    // ------------------------------------------

    private void FixedUpdate()
    {
        if (_Exploded)
        {
            return;
        }

        // Закончился срок жизни ракеты.
        if (Time.time >= _DeathTime)
        {
            Explode(_Rigidbody.position, false);
            return;
        }

        Vector3 startPosition = _Rigidbody.position;

        // Центр нашей физической сферы.
        Vector3 sphereCenter =
            startPosition +
            transform.TransformVector(_SphereCollider.center);

        // Учитываем масштаб ракеты.
        Vector3 scale = transform.lossyScale;

        float largestScale = Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );

        float radius =
            _SphereCollider.radius * largestScale;


        // 1. Проверяем, не находится ли ракета
        // уже внутри другого коллайдера.
        Collider[] overlaps = Physics.OverlapSphere(
            sphereCenter,
            radius,
            _CollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider col in overlaps)
        {
            if (ShouldIgnore(col))
            {
                continue;
            }

            HitObject(col, sphereCenter);
            return;
        }


        // 2. Корректируем направление полёта.
        UpdateHoming(startPosition);


        // 3. Проверяем близость к игроку.
        if (IsNearPlayer(sphereCenter))
        {
            Explode(sphereCenter, true);
            return;
        }


        float moveDistance =
            _Speed * Time.fixedDeltaTime;


        // 4. Проверяем весь путь движения сферы.
        RaycastHit[] hits = Physics.SphereCastAll(
            sphereCenter,
            radius,
            _Direction,
            moveDistance,
            _CollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        Collider nearestCollider = null;
        float nearestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnore(hit.collider))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestCollider = hit.collider;
            }
        }


        // Если на пути есть объект, взрываемся.
        if (nearestCollider != null)
        {
            Vector3 hitPosition =
                sphereCenter + _Direction * nearestDistance;

            HitObject(nearestCollider, hitPosition);
            return;
        }


        // 5. Если препятствий нет, летим дальше.
        Vector3 nextPosition =
            startPosition + _Direction * moveDistance;

        _Rigidbody.MovePosition(nextPosition);

        _Rigidbody.MoveRotation(
            Quaternion.LookRotation(_Direction)
        );


        // 6. Дополнительная проверка сближения
        // уже в следующей позиции.
        if (IsNearPlayer(nextPosition))
        {
            Explode(nextPosition, true);
        }
    }


    // ------------------------------------------
    // САМОНАВЕДЕНИЕ
    // ------------------------------------------

    private void UpdateHoming(Vector3 currentPosition)
    {
        if (_Target == null)
        {
            return;
        }

        Vector3 desiredDirection =
            GetAimPosition() - currentPosition;

        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float maxRadians =
            _HomingTurnSpeed *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        // Поворачиваемся к игроку,
        // но не быстрее заданной скорости.
        _Direction = Vector3.RotateTowards(
            _Direction,
            desiredDirection.normalized,
            maxRadians,
            0f
        ).normalized;
    }


    // ------------------------------------------
    // ПРОВЕРКА БЛИЗОСТИ К ИГРОКУ
    // ------------------------------------------

    private bool IsNearPlayer(Vector3 rocketPosition)
    {
        if (_TargetCollider == null ||
            !_TargetCollider.enabled)
        {
            return false;
        }

        // Находим ближайшую точку поверхности
        // коллайдера игрока к ракете.
        Vector3 closestPoint =
            _TargetCollider.ClosestPoint(rocketPosition);

        float distance =
            Vector3.Distance(rocketPosition, closestPoint);

        return distance <= _ProximityDistance;
    }


    // ------------------------------------------
    // ФИЛЬТРАЦИЯ СТОЛКНОВЕНИЙ
    // ------------------------------------------

    private bool ShouldIgnore(Collider col)
    {
        if (col == null)
        {
            return true;
        }

        Transform hitTransform = col.transform;

        // Игнорируем собственные коллайдеры.
        if (hitTransform == transform ||
            hitTransform.IsChildOf(transform))
        {
            return true;
        }

        // Игнорируем врага, запустившего ракету.
        if (_Owner != null &&
            (hitTransform == _Owner ||
             hitTransform.IsChildOf(_Owner)))
        {
            return true;
        }

        return false;
    }


    // ------------------------------------------
    // ПОПАДАНИЕ В ОБЪЕКТ
    // ------------------------------------------

    private void HitObject(
        Collider hitCollider,
        Vector3 hitPosition
    )
    {
        if (_DebugHits)
        {
            Debug.Log(
                "РАКЕТА ПОПАЛА В: " + hitCollider.name,
                this
            );
        }

        // Проверяем, принадлежит ли объект игроку.
        bool hitPlayer = _Target != null &&
            (hitCollider.transform == _Target ||
             hitCollider.transform.IsChildOf(_Target));

        Explode(hitPosition, hitPlayer);
    }


    // ------------------------------------------
    // ПОЛУЧЕНИЕ УРОНА ОТ ПУЛЬ
    // ------------------------------------------

    public void TakeDamage(float damage)
    {
        if (_Exploded || damage <= 0f)
        {
            return;
        }

        _CurrentHealth -= damage;

        if (_DebugHits)
        {
            Debug.Log(
                "В РАКЕТУ ПОПАЛИ! HP = " + _CurrentHealth,
                this
            );
        }

        if (_CurrentHealth <= 0f)
        {
            // Ракета сбита!
            // Урон игроку не наносим.
            Explode(_Rigidbody.position, false);
        }
    }


    // ------------------------------------------
    // ВЗРЫВ
    // ------------------------------------------

    private void Explode(
        Vector3 position,
        bool damagePlayer
    )
    {
        if (_Exploded)
        {
            return;
        }

        _Exploded = true;


        // Урон наносим только если ракета
        // действительно добралась до игрока.
        if (damagePlayer)
        {
            if (_TargetHealth != null)
            {
                _TargetHealth.TakeDamage(_Damage);

                if (_DebugHits)
                {
                    Debug.Log(
                        "РАКЕТА НАНЕСЛА ИГРОКУ УРОН: " + _Damage,
                        this
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "Ракета попала в игрока, но PlayerHealth не найден!",
                    this
                );
            }
        }


        // Визуальный взрыв.
        if (_ExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                _ExplosionPrefab,
                position,
                Quaternion.identity
            );

            Destroy(explosion, 3f);
        }


        // Звук взрыва.
        if (_ExplosionSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _ExplosionSound,
                position,
                _ExplosionVolume
            );
        }


        // Уничтожаем ракету.
        Destroy(gameObject);
    }
}
