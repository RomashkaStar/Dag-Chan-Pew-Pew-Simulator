
using UnityEngine;
using System.Collections;

public class EnemyRocketLauncher : MonoBehaviour
{
    [Header("Rocket")]

    // Prefab ракеты.
    [SerializeField]
    private EnemyRocketProjectile _RocketPrefab;

    // Место появления ракеты.
    [SerializeField]
    private Transform _RocketSpawnPoint;


    [Header("Launch Timing")]

    // Задержка между началом звука
    // и настоящим вылетом ракеты.
    [SerializeField, Min(0f)]
    private float _LaunchDelay = 0.13f;


    [Header("Launch Sounds")]

    // Звук подготовки атаки.
    // В твоём случае — двойное ГАВ-ГАВ.
    [SerializeField]
    private AudioClip _LaunchSound;

    [SerializeField, Range(0f, 1f)]
    private float _LaunchVolume = 1f;

    // Необязательный звук самого запуска ракеты.
    [SerializeField]
    private AudioClip _RocketReleaseSound;


    [Header("Launch Effects")]

    // Дополнительный эффект выстрела.
    [SerializeField]
    private GameObject _MuzzleFlashPrefab;


    // Идёт ли сейчас подготовка выстрела?
    private bool _IsPreparingShot;

    // Для EnemyCombatAI.
    public float LaunchDelay =>
        Mathf.Max(0f, _LaunchDelay);


    // ------------------------------------
    // ЗАПРОС НА ВЫСТРЕЛ
    // ------------------------------------

    public bool FireAt(Transform target)
    {
        // Не начинаем второй выстрел,
        // пока первый ещё готовится.
        if (_IsPreparingShot)
        {
            return false;
        }

        // Проверяем необходимые ссылки.
        if (_RocketPrefab == null ||
            _RocketSpawnPoint == null ||
            target == null)
        {
            return false;
        }

        Enemy owner = GetComponentInParent<Enemy>();

        // Мёртвый противник не стреляет.
        if (owner != null && owner.IsDead)
        {
            return false;
        }

        // Запускаем звук предупреждения.
        //
        // Метод Enemy проверит, не играет ли
        // сейчас более приоритетный звук ранения.
        if (owner != null)
        {
            if (!owner.TryPlayAttackCue(
                _LaunchSound,
                _LaunchVolume))
            {
                return false;
            }
        }
        else if (_LaunchSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _LaunchSound,
                _RocketSpawnPoint.position,
                _LaunchVolume
            );
        }

        // Блокируем повторные запросы.
        _IsPreparingShot = true;

        // Запоминаем здоровье в момент
        // начала подготовки выстрела.
        float healthBeforeShot =
            owner != null ? owner.CurrentHealth : 0f;

        // Запускаем корутину с задержкой.
        StartCoroutine(
            FireSequence(target, owner, healthBeforeShot)
        );

        // true означает, что выстрел принят
        // в обработку, а не что ракета уже создана.
        return true;
    }


    // ------------------------------------
    // ПОСЛЕДОВАТЕЛЬНОСТЬ ВЫСТРЕЛА
    // ------------------------------------

    private IEnumerator FireSequence(
        Transform target,
        Enemy owner,
        float healthBeforeShot
    )
    {
        // Ждём окончания подготовки.
        // В это время звучит ГАВ-ГАВ.
        if (_LaunchDelay > 0f)
        {
            yield return new WaitForSeconds(_LaunchDelay);
        }

        // Если игрок исчез или противник умер,
        // отменяем запуск ракеты.
        if (target == null ||
            (owner != null && owner.IsDead))
        {
            _IsPreparingShot = false;
            yield break;
        }

        // Дополнительная механика:
        // если враг получил урон во время
        // подготовки, отменяем этот выстрел.
        if (owner != null &&
            owner.CurrentHealth < healthBeforeShot)
        {
            _IsPreparingShot = false;
            yield break;
        }


        // ------------------------------------
        // НАСТОЯЩИЙ ЗАПУСК РАКЕТЫ
        // ------------------------------------

        EnemyRocketProjectile rocket = Instantiate(
            _RocketPrefab,
            _RocketSpawnPoint.position,
            _RocketSpawnPoint.rotation
        );

        // Передаём ракете игрока и владельца.
        Transform ownerTransform =
            owner != null ? owner.transform : transform.root;

        rocket.Launch(target, ownerTransform);


        // ВАЖНО!
        // Показываем стреляющий спрайт
        // именно в момент запуска ракеты,
        // а не во время гавканья.
        if (owner != null)
        {
            owner.PlayFireAnimation();
        }


        // Необязательный звук вылета ракеты.
        if (_RocketReleaseSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _RocketReleaseSound,
                _RocketSpawnPoint.position
            );
        }


        // Дополнительная вспышка выстрела.
        if (_MuzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(
                _MuzzleFlashPrefab,
                _RocketSpawnPoint.position,
                _RocketSpawnPoint.rotation
            );

            Destroy(flash, 1f);
        }


        // Подготовка завершена.
        _IsPreparingShot = false;
    }


    private void OnDisable()
    {
        // Если компонент отключат,
        // незавершённые корутины остановятся.
        StopAllCoroutines();

        _IsPreparingShot = false;
    }
}
