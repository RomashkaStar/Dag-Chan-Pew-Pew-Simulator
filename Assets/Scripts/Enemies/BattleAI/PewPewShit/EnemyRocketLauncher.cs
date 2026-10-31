
using UnityEngine;
using System.Collections;

public class EnemyRocketLauncher : MonoBehaviour
{
    [Header("Rocket")]

    // Префаб ракеты, которую будем создавать.
    [SerializeField]
    private EnemyRocketProjectile _RocketPrefab;

    // Точка появления ракеты.
    [SerializeField]
    private Transform _RocketSpawnPoint;


    [Header("Launch Timing")]

    // Задержка между началом подготовки атаки
    // и фактическим запуском ракеты.
    [SerializeField, Min(0f)]
    private float _LaunchDelay = 0.13f;


    [Header("Launch Sounds")]

    // Звук подготовки атаки (наш двойной гав-гав).
    [SerializeField]
    private AudioClip _LaunchSound;

    [SerializeField, Range(0f, 1f)]
    private float _LaunchVolume = 1f;

    // Отдельный звук вылета ракеты.
    [SerializeField]
    private AudioClip _RocketReleaseSound;


    [Header("Launch Effects")]

    // Визуальная вспышка при запуске ракеты.
    [SerializeField]
    private GameObject _MuzzleFlashPrefab;


    // Флаг подготовки атаки.
    // Не позволяет запускать несколько ракет
    // одновременно во время задержки выстрела.
    private bool _IsPreparingShot;

    // Публичное свойство для EnemyCombatAI.
    // Позволяет прочитать задержку выстрела,
    // но не изменять её напрямую.
    public float LaunchDelay =>
        Mathf.Max(0f, _LaunchDelay);


    // ------------------------------------
    // ПОДГОТОВКА ВЫСТРЕЛА
    // ------------------------------------

    // Метод вызывается EnemyCombatAI,
    // когда противник решает атаковать игрока.
    //
    // Возвращает true, если атака НАЧАЛА
    // подготавливаться, а не если ракета
    // уже была выпущена.
    public bool FireAt(Transform target)
    {
        // Пока готовится предыдущий выстрел,
        // новую атаку начинать запрещено.
        if (_IsPreparingShot)
        {
            return false;
        }

        // Проверяем наличие необходимых объектов.
        if (_RocketPrefab == null ||
            _RocketSpawnPoint == null ||
            target == null)
        {
            return false;
        }


        // Находим Enemy на родительском объекте.
        // Он отвечает за здоровье, состояние смерти,
        // звуки и анимации нашего противника.
        Enemy owner = GetComponentInParent<Enemy>();

        if (owner != null && owner.IsDead)
        {
            return false;
        }


        // Просим Enemy воспроизвести звук подготовки.
        //
        // TryPlayAttackCue возвращает bool, поэтому
        // Enemy сам может разрешить или запретить
        // начало этой атаки.
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
            // Запасной вариант на случай, если
            // Launcher используется без компонента Enemy.
            AudioSource.PlayClipAtPoint(
                _LaunchSound,
                _RocketSpawnPoint.position,
                _LaunchVolume
            );
        }


        // Блокируем повторный запуск атаки,
        // пока корутина не закончит подготовку.
        _IsPreparingShot = true;


        // Запоминаем здоровье врага перед выстрелом.
        // Позже сравним его с текущим здоровьем:
        // если враг получил урон во время подготовки,
        // выстрел будет отменён.
        float healthBeforeShot =
            owner != null ? owner.CurrentHealth : 0f;


        // Запускаем корутину, которая подождёт
        // нужное время и создаст ракету.
        //
        // Передаём в неё цель, владельца
        // и здоровье владельца перед атакой.
        StartCoroutine(
            FireSequence(target, owner, healthBeforeShot)
        );


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
        // Ждём завершения подготовки атаки.
        //
        // WaitForSeconds зависит от Time.timeScale,
        // поэтому при остановке игрового времени
        // подготовка выстрела тоже остановится.
        if (_LaunchDelay > 0f)
        {
            yield return new WaitForSeconds(_LaunchDelay);
        }


        // Пока мы ждали, игрок мог исчезнуть,
        // а противник мог погибнуть.
        // В таком случае отменяем запуск.
        if (target == null ||
            (owner != null && owner.IsDead))
        {
            _IsPreparingShot = false;

            // yield break полностью завершает корутину.
            yield break;
        }


        // Если враг получил урон во время подготовки,
        // отменяем выстрел.
        //
        // Сравниваем текущее здоровье с тем,
        // которое сохранили в FireAt().
        //
        // Таким образом попадание по противнику
        // может прервать его атаку.
        if (owner != null &&
            owner.CurrentHealth < healthBeforeShot)
        {
            _IsPreparingShot = false;
            yield break;
        }


        // ------------------------------------
        // СОЗДАНИЕ РАКЕТЫ
        // ------------------------------------

        // Instantiate создаёт экземпляр префаба
        // в позиции и с поворотом RocketSpawnPoint.
        //
        // Важно: создание ракеты ещё не означает,
        // что она получила цель и начала двигаться.
        EnemyRocketProjectile rocket = Instantiate(
            _RocketPrefab,
            _RocketSpawnPoint.position,
            _RocketSpawnPoint.rotation
        );


        // Определяем владельца ракеты.
        // Обычно это Enemy, выпустивший снаряд.
        Transform ownerTransform =
            owner != null ? owner.transform : transform.root;


        // Передаём ракете цель и владельца.
        //
        // Дальнейшее движение, наведение,
        // столкновения и взрыв обрабатываются
        // уже самим EnemyRocketProjectile.
        rocket.Launch(target, ownerTransform);


        // Запускаем анимацию выстрела противника.
        // Ракета уже создана, поэтому анимация
        // совпадает с моментом её запуска.
        if (owner != null)
        {
            owner.PlayFireAnimation();
        }


        // Воспроизводим отдельный звук вылета.
        // PlayClipAtPoint создаёт временный
        // пространственный источник звука.
        if (_RocketReleaseSound != null)
        {
            AudioSource.PlayClipAtPoint(
                _RocketReleaseSound,
                _RocketSpawnPoint.position
            );
        }


        // Создаём вспышку у дула.
        // Её уничтожаем через секунду,
        // чтобы временные эффекты не оставались
        // в сцене после каждого выстрела.
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
        // Теперь FireAt() снова может принять
        // запрос на следующий выстрел.
        _IsPreparingShot = false;
    }


    private void OnDisable()
    {
        // Если компонент отключили во время
        // подготовки атаки, останавливаем корутину.
        //
        // Сбрасываем флаг, чтобы после повторного
        // включения враг не остался навсегда
        // в состоянии подготовки выстрела.
        StopAllCoroutines();

        _IsPreparingShot = false;
    }
}
