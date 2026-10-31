
using UnityEngine;

// Ракета использует Rigidbody для движения
// и SphereCollider для расчёта столкновений.
//
// IDamageable позволяет другим скриптам
// наносить ракете урон через общий интерфейс.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class EnemyRocketProjectile : MonoBehaviour, IDamageable
{
    [Header("Movement")]

    [SerializeField, Min(0.1f)]
    private float _Speed = 4f;

    // Максимальная скорость разворота ракеты
    // к цели в градусах в секунду.
    // При 0 ракета не сможет менять направление.
    [SerializeField, Min(0f)]
    private float _HomingTurnSpeed = 540f;

    // Дистанция, на которой ракета взрывается
    // рядом с игроком даже без прямого столкновения.
    [SerializeField, Min(0f)]
    private float _ProximityDistance = 0.35f;

    // Максимальная продолжительность жизни ракеты.
    [SerializeField, Min(0.1f)]
    private float _Lifetime = 8f;


    [Header("Health And Damage")]

    [SerializeField, Min(0f)]
    private float _Damage = 15f;

    // Ракету можно сбить, нанеся ей достаточно урона.
    [SerializeField, Min(0.1f)]
    private float _MaxHealth = 20f;


    [Header("Collision")]

    // Слои объектов, с которыми ракета
    // должна проверять столкновения.
    // Сюда должны входить Player и стены.
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

    // Цель, которую ракета преследует,
    // и противник, который её выпустил.
    private Transform _Target;
    private Transform _Owner;

    private Collider _TargetCollider;
    private PlayerHealth _TargetHealth;

    // Текущее направление полёта.
    // Оно постепенно изменяется при самонаведении.
    private Vector3 _Direction;

    private float _CurrentHealth;

    // Момент времени, когда ракета должна исчезнуть.
    private float _DeathTime;

    // Запрещает повторные взрывы и нанесение
    // урона несколько раз одной ракетой.
    private bool _Exploded;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Awake()
    {
        _Rigidbody = GetComponent<Rigidbody>();
        _SphereCollider = GetComponent<SphereCollider>();

        // Движением управляем самостоятельно
        // через MovePosition и MoveRotation,
        // поэтому отключаем динамическую физику
        // и стандартную гравитацию Rigidbody.
        _Rigidbody.isKinematic = true;
        _Rigidbody.useGravity = false;

        // Используем обычный Collider.
        // Столкновения при этом дополнительно
        // проверяем собственными Physics-запросами.
        _SphereCollider.isTrigger = false;

        _CurrentHealth = Mathf.Max(0.1f, _MaxHealth);

        // До получения цели ракета направлена вперёд.
        _Direction = transform.forward;

        // Вместо счётчика времени запоминаем
        // конкретный момент уничтожения ракеты.
        _DeathTime = Time.time + _Lifetime;
    }


    // ------------------------------------
    // ЗАПУСК РАКЕТЫ
    // ------------------------------------

    // Вызывается из EnemyRocketLauncher
    // сразу после создания ракеты через Instantiate.
    //
    // target — кого преследовать.
    // owner — кто выпустил ракету.
    public void Launch(Transform target, Transform owner)
    {
        _Target = target;
        _Owner = owner;

        if (_Target != null)
        {
            // Стараемся использовать CharacterController,
            // поскольку он наиболее точно описывает
            // физический объём нашего игрока.
            CharacterController controller =
                _Target.GetComponentInChildren<CharacterController>();

            if (controller != null)
            {
                _TargetCollider = controller;
            }
            else
            {
                // Если CharacterController отсутствует,
                // используем любой другой Collider цели.
                _TargetCollider =
                    _Target.GetComponentInChildren<Collider>();
            }


            // Получаем PlayerHealth, чтобы при попадании
            // передавать урон непосредственно игроку.
            _TargetHealth =
                _Target.GetComponentInChildren<PlayerHealth>();

            if (_TargetHealth == null)
            {
                Debug.LogWarning(
                    "Ракета: у цели не найден PlayerHealth!",
                    this
                );
            }


            // При запуске сразу направляем ракету
            // в сторону игрока.
            //
            // normalized превращает вектор направления
            // в вектор единичной длины, чтобы расстояние
            // до цели не влияло на скорость полёта.
            Vector3 direction =
                GetAimPosition() - transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                _Direction = direction.normalized;
            }
        }

        // Поворачиваем модель ракеты
        // в направлении её будущего полёта.
        if (_Direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(_Direction);
        }
    }


    // ------------------------------------
    // ТОЧКА ПРИЦЕЛИВАНИЯ
    // ------------------------------------

    private Vector3 GetAimPosition()
    {
        // Целимся в центр Collider игрока,
        // а не в точку его Transform,
        // которая обычно находится возле ног.
        if (_TargetCollider != null &&
            _TargetCollider.enabled)
        {
            return _TargetCollider.bounds.center;
        }

        // Запасной вариант, если Collider отсутствует.
        if (_Target != null)
        {
            return _Target.position + Vector3.up;
        }

        return transform.position + _Direction;
    }


    // ------------------------------------
    // ФИЗИКА ПОЛЁТА
    // ------------------------------------

    // FixedUpdate используется для движения
    // Rigidbody и физических проверок.
    private void FixedUpdate()
    {
        if (_Exploded)
        {
            return;
        }

        // Если время жизни закончилось,
        // взрываем ракету без урона игроку.
        if (Time.time >= _DeathTime)
        {
            Explode(_Rigidbody.position, false);
            return;
        }


        Vector3 startPosition = _Rigidbody.position;

        // Получаем мировой центр SphereCollider.
        // TransformVector учитывает локальное смещение
        // Collider относительно объекта ракеты.
        Vector3 sphereCenter =
            startPosition +
            transform.TransformVector(_SphereCollider.center);


        // Радиус SphereCollider задаётся локально.
        // Пересчитываем его с учётом масштаба объекта.
        //
        // Берём максимальную ось, чтобы проверка
        // не оказалась меньше физического объёма.
        Vector3 scale = transform.lossyScale;

        float largestScale = Mathf.Max(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z)
        );

        float radius =
            _SphereCollider.radius * largestScale;


        // ====================================
        // 1. ПРОВЕРКА ТЕКУЩЕГО ПОЛОЖЕНИЯ
        // ====================================

        // OverlapSphere ищет все Collider,
        // которые УЖЕ пересекаются со сферой ракеты.
        //
        // Это нужно, если ракета появилась внутри
        // препятствия или пересечение уже произошло.
        //
        // QueryTriggerInteraction.Ignore означает,
        // что Trigger-коллайдеры не учитываются.
        Collider[] overlaps = Physics.OverlapSphere(
            sphereCenter,
            radius,
            _CollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider col in overlaps)
        {
            // Игнорируем собственные коллайдеры
            // и коллайдеры противника-владельца.
            if (ShouldIgnore(col))
            {
                continue;
            }

            HitObject(col, sphereCenter);
            return;
        }


        // ====================================
        // 2. САМОНАВЕДЕНИЕ
        // ====================================

        // Корректируем направление полёта в сторону
        // текущего положения игрока.
        //
        // Ракета не телепортируется к цели —
        // меняется только вектор _Direction.
        UpdateHoming(startPosition);


        // ====================================
        // 3. ПРОВЕРКА БЛИЗОСТИ К ИГРОКУ
        // ====================================

        // Ракета может взорваться рядом с игроком,
        // даже если ещё не коснулась его Collider.
        if (IsNearPlayer(sphereCenter))
        {
            Explode(sphereCenter, true);
            return;
        }


        // Расстояние, которое ракета должна
        // пролететь за один физический шаг.
        float moveDistance =
            _Speed * Time.fixedDeltaTime;


        // ====================================
        // 4. ПРОВЕРКА ПУТИ ПЕРЕД ДВИЖЕНИЕМ
        // ====================================

        // SphereCastAll — это как Raycast,
        // только вместо тонкого луча мы
        // протягиваем сферу по направлению полёта.
        //
        // Такая проверка не даёт ракете
        // пролетать сквозь тонкие препятствия
        // между двумя физическими кадрами.
        //
        // Возвращает все найденные столкновения
        // на заданном отрезке движения.
        RaycastHit[] hits = Physics.SphereCastAll(
            sphereCenter,
            radius,
            _Direction,
            moveDistance,
            _CollisionLayers,
            QueryTriggerInteraction.Ignore
        );


        // Из всех обнаруженных объектов
        // будем выбирать ближайший по пути полёта.
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


        // Если впереди обнаружено препятствие,
        // ракета взрывается до перемещения.
        if (nearestCollider != null)
        {
            Vector3 hitPosition =
                sphereCenter + _Direction * nearestDistance;

            HitObject(nearestCollider, hitPosition);
            return;
        }


        // ====================================
        // 5. ПЕРЕМЕЩЕНИЕ РАКЕТЫ
        // ====================================

        // Если препятствий нет, перемещаем ракету
        // в рассчитанном направлении.
        Vector3 nextPosition =
            startPosition + _Direction * moveDistance;

        _Rigidbody.MovePosition(nextPosition);

        // Поворачиваем модель по направлению движения,
        // чтобы визуально ракета смотрела туда,
        // куда действительно летит.
        _Rigidbody.MoveRotation(
            Quaternion.LookRotation(_Direction)
        );


        // ====================================
        // 6. ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА
        // ====================================

        // После движения ещё раз проверяем,
        // не оказались ли достаточно близко к игроку.
        if (IsNearPlayer(nextPosition))
        {
            Explode(nextPosition, true);
        }
    }


    // ------------------------------------
    // САМОНАВЕДЕНИЕ
    // ------------------------------------

    private void UpdateHoming(Vector3 currentPosition)
    {
        if (_Target == null)
        {
            return;
        }

        // Вычисляем направление от ракеты к игроку.
        Vector3 desiredDirection =
            GetAimPosition() - currentPosition;

        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }


        // RotateTowards принимает максимальный
        // угол поворота в радианах.
        //
        // Поэтому переводим градусы в радианы
        // и учитываем длительность физического шага.
        float maxRadians =
            _HomingTurnSpeed *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;


        // Постепенно поворачиваем текущий вектор
        // направления в сторону игрока.
        //
        // Главное отличие от мгновенного наведения:
        // ракета не может развернуться быстрее
        // заданной HomingTurnSpeed.
        //
        // Последний аргумент 0f означает,
        // что длину вектора менять не нужно.
        _Direction = Vector3.RotateTowards(
            _Direction,
            desiredDirection.normalized,
            maxRadians,
            0f
        ).normalized;
    }


    // ------------------------------------
    // ПРОВЕРКА БЛИЗОСТИ К ИГРОКУ
    // ------------------------------------

    private bool IsNearPlayer(Vector3 rocketPosition)
    {
        if (_TargetCollider == null ||
            !_TargetCollider.enabled)
        {
            return false;
        }

        // ClosestPoint возвращает точку Collider
        // игрока, ближайшую к ракете.
        //
        // Это точнее, чем измерять расстояние
        // до центра персонажа: игрок имеет объём,
        // а не является одной точкой.
        Vector3 closestPoint =
            _TargetCollider.ClosestPoint(rocketPosition);

        float distance =
            Vector3.Distance(rocketPosition, closestPoint);

        return distance <= _ProximityDistance;
    }


    // ------------------------------------
    // ФИЛЬТРАЦИЯ СТОЛКНОВЕНИЙ
    // ------------------------------------

    private bool ShouldIgnore(Collider col)
    {
        if (col == null)
        {
            return true;
        }

        Transform hitTransform = col.transform;


        // Игнорируем собственный Collider ракеты
        // и Collider её дочерних объектов.
        if (hitTransform == transform ||
            hitTransform.IsChildOf(transform))
        {
            return true;
        }


        // Игнорируем противника, который выпустил
        // ракету, и все его дочерние объекты.
        //
        // Без этого ракета могла бы взорваться
        // сразу после появления возле своего владельца.
        if (_Owner != null &&
            (hitTransform == _Owner ||
             hitTransform.IsChildOf(_Owner)))
        {
            return true;
        }

        return false;
    }


    // ------------------------------------
    // ОБРАБОТКА ПОПАДАНИЯ
    // ------------------------------------

    private void HitObject(
        Collider hitCollider,
        Vector3 hitPosition
    )
    {
        if (_DebugHits)
        {
            Debug.Log(
                "Ракета попала в: " + hitCollider.name,
                this
            );
        }


        // Проверяем, принадлежит ли Collider игроку.
        //
        // Если попали именно в игрока —
        // взрыв должен нанести ему урон.
        //
        // При попадании в стену ракета тоже взорвётся,
        // но урон игроку не передастся.
        bool hitPlayer = _Target != null &&
            (hitCollider.transform == _Target ||
             hitCollider.transform.IsChildOf(_Target));

        Explode(hitPosition, hitPlayer);
    }


    // ------------------------------------
    // ПОЛУЧЕНИЕ УРОНА РАКЕТОЙ
    // ------------------------------------

    // Метод реализует интерфейс IDamageable.
    //
    // Благодаря этому система стрельбы игрока
    // может наносить урон ракете так же,
    // как и другим уничтожаемым объектам.
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
                "В ракету попали! HP = " + _CurrentHealth,
                this
            );
        }

        if (_CurrentHealth <= 0f)
        {
            // Сбитая ракета взрывается,
            // но игрок не получает урон.
            Explode(_Rigidbody.position, false);
        }
    }


    // ------------------------------------
    // ВЗРЫВ
    // ------------------------------------

    // damagePlayer определяет, нужно ли
    // при взрыве наносить урон игроку.
    //
    // Этот параметр зависит от причины взрыва:
    //
    // Попали в игрока — true.
    // Столкнулись со стеной — false.
    // Ракету сбили выстрелом — false.
    // Закончилось время жизни — false.
    private void Explode(
        Vector3 position,
        bool damagePlayer
    )
    {
        // Запрещаем повторный взрыв.
        //
        // Это особенно важно, потому что ракета
        // проверяет столкновения несколькими способами.
        if (_Exploded)
        {
            return;
        }

        _Exploded = true;


        // Наносим урон только при взрыве,
        // который был засчитан как попадание
        // или сближение с игроком.
        if (damagePlayer)
        {
            if (_TargetHealth != null)
            {
                // Вся логика получения урона
                // находится в PlayerHealth.
                _TargetHealth.TakeDamage(_Damage);

                if (_DebugHits)
                {
                    Debug.Log(
                        "Ракета нанесла игроку урон: " + _Damage,
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


        // Создаём визуальный эффект взрыва
        // как отдельный объект.
        //
        // Уничтожение самой ракеты не должно
        // немедленно уничтожать эффект взрыва.
        if (_ExplosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                _ExplosionPrefab,
                position,
                Quaternion.identity
            );

            Destroy(explosion, 3f);
        }


        // Воспроизводим звук в точке взрыва.
        // PlayClipAtPoint создаёт временный AudioSource,
        // поэтому звук не пропадёт при Destroy(ракеты).
        if (_ExplosionSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _ExplosionSound,
                position,
                _ExplosionVolume
            );
        }


        // Уничтожаем саму ракету.
        Destroy(gameObject);
    }
}
