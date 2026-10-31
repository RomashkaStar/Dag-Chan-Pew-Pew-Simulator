
using UnityEngine;

// Для зоны лестницы обязательно нужен BoxCollider.
[RequireComponent(typeof(BoxCollider))]
public class LadderZone : MonoBehaviour
{
    [Header("Лестница")]

    [SerializeField] private float _ClimbSpeed = 3f;

    // Даём GO_DagChan возможность прочитать
    // скорость подъёма, но не изменять её напрямую.
    public float ClimbSpeed => _ClimbSpeed;


    private void Reset()
    {
        // Reset вызывается при добавлении компонента
        // или выборе Reset в Inspector.
        //
        // Настраиваем Collider как Trigger:
        // игрок сможет проходить через зону,
        // а Unity будет сообщать о входе и выходе.
        GetComponent<BoxCollider>().isTrigger = true;
    }


    // ------------------------------------
    // ВХОД НА ЛЕСТНИЦУ
    // ------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        // Unity вызывает метод, когда другой Collider
        // входит в триггер лестницы.
        //
        // GetComponentInParent ищет GO_DagChan
        // на объекте вошедшего Collider и его родителях.
        GO_DagChan player =
            other.GetComponentInParent<GO_DagChan>();

        if (player != null)
        {
            // Передаём ссылку на текущую лестницу.
            // Само перемещение выполняет GO_DagChan,
            // а не этот скрипт.
            player.EnterLadder(this);
        }
    }


    // ------------------------------------
    // НАХОЖДЕНИЕ НА ЛЕСТНИЦЕ
    // ------------------------------------

    private void OnTriggerStay(Collider other)
    {
        // Вызывается во время физических обновлений,
        // пока Collider остаётся внутри Trigger.
        //
        // Дополнительно подтверждаем, что игрок
        // находится на лестнице. Метод EnterLadder
        // сам игнорирует повторную передачу
        // одной и той же лестницы.
        GO_DagChan player =
            other.GetComponentInParent<GO_DagChan>();

        if (player != null)
        {
            player.EnterLadder(this);
        }
    }


    // ------------------------------------
    // ВЫХОД С ЛЕСТНИЦЫ
    // ------------------------------------

    private void OnTriggerExit(Collider other)
    {
        // Unity вызывает метод, когда Collider
        // покидает область триггера.
        GO_DagChan player =
            other.GetComponentInParent<GO_DagChan>();

        if (player != null)
        {
            // Сообщаем игроку, что он покинул лестницу.
            // GO_DagChan сбросит CurrentLadder
            // и вернётся к обычной гравитации.
            player.ExitLadder(this);
        }
    }
}
