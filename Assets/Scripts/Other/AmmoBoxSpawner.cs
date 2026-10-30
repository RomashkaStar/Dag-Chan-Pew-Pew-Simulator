
using UnityEngine;
using System.Collections;

public class AmmoBoxSpawner : MonoBehaviour
{
    [Header("Ammo Box")]

    // Префаб коробки с патронами,
    // который спавнер будет создавать.
    [SerializeField] private GameObject _AmmoBoxPrefab;


    [Header("Respawn Settings")]

    // Через сколько секунд после подбора
    // должна появиться новая коробка.
    [SerializeField, Min(0f)]
    private float _RespawnDelay = 40f;


    [Header("Spawn Position")]

    [SerializeField] private float _SpawnHeight = 0.5f;


    // Ссылка на КОНКРЕТНУЮ коробку,
    // которую создал этот спавнер.
    //
    // Если коробку уничтожили через Destroy(),
    // Unity будет считать эту ссылку равной null.
    private GameObject _CurrentAmmoBox;


    private void Start()
    {
        if (_AmmoBoxPrefab == null)
        {
            Debug.LogError(
                "AmmoBoxSpawner: не назначен AmmoBox Prefab!",
                this
            );

            return;
        }

        // Запускаем бесконечный цикл появления патронов.
        StartCoroutine(SpawnRoutine());
    }


    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // ==================================
            // СОЗДАЁМ КОРОБКУ ПАТРОНОВ
            // ==================================

            _CurrentAmmoBox = Instantiate(
                _AmmoBoxPrefab,
                transform.position + Vector3.up * _SpawnHeight,
                transform.rotation
            );


            // ==================================
            // ЖДЁМ, ПОКА КОРОБКУ ПОДБЕРУТ
            // ==================================

            // Пока коробка существует в сцене,
            // мы просто ждём следующий кадр.
            //
            // Благодаря этому спавнер НЕ создаёт
            // дополнительные коробки, пока
            // предыдущая ещё лежит на месте.
            while (_CurrentAmmoBox != null)
            {
                yield return null;
            }


            // ==================================
            // ОЖИДАНИЕ РЕСПАВНА
            // ==================================

            // Сюда попадём только после того,
            // как созданная коробка уничтожена.
            //
            // Например, AmmoPickup вызвал
            // Destroy(gameObject) после подбора.
            yield return new WaitForSeconds(
                _RespawnDelay
            );


            // Цикл начинается заново.
            // Появится новая коробка патронов.
        }
    }


    // Рисуем точку спавна в редакторе Unity.
    //
    // На саму игру это не влияет.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            0.3f
        );
    }
}
