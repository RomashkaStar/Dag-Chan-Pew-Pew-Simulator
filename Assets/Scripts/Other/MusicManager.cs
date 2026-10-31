
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// Автоматически требует AudioSource на объекте.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Music Folder")]

    // Папка с MP3 внутри Assets/StreamingAssets.
    public string musicFolderName = "PlayerMusic";


    [Header("Music Settings")]

    [Range(0f, 1f)]
    public float musicVolume = 0.2f;


    [Header("Input System")]

    [SerializeField] private PlayerInput _PlayerInput;

    private InputAction _MusicSwitchAction;


    private AudioSource _AudioSource;

    // Список ещё не проигранных треков.
    // Выбранный трек удаляется из списка,
    // поэтому музыка не повторяется до нового круга.
    private List<string> _RemainingTracks = new List<string>();

    // Нужен, чтобы последний трек предыдущего
    // круга не повторился в начале следующего.
    private string _LastTrack;


    // ------------------------------------
    // ИНИЦИАЛИЗАЦИЯ
    // ------------------------------------

    private void Awake()
    {
        _AudioSource = GetComponent<AudioSource>();

        // Музыкой полностью управляет наш скрипт.
        // AudioSource не должен запускать или
        // зацикливать композиции самостоятельно.
        _AudioSource.playOnAwake = false;
        _AudioSource.loop = false;

        // 2D-звук: громкость музыки не зависит
        // от положения игрока относительно объекта.
        _AudioSource.spatialBlend = 0f;

        _AudioSource.volume = musicVolume;

        // При смерти игрока AudioListener.pause = true.
        // Музыка должна останавливаться вместе
        // с остальными обычными звуками.
        _AudioSource.ignoreListenerPause = false;
    }


    private void Start()
    {
        // MusicManager находится отдельно от игрока.
        // Если PlayerInput не назначен в Inspector,
        // ищем его автоматически в сцене.
        if (_PlayerInput == null)
        {
            _PlayerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (_PlayerInput != null &&
            _PlayerInput.actions != null)
        {
            // Получаем действие MusicSwitch из
            // существующей Action Map игрока.
            _MusicSwitchAction =
                _PlayerInput.actions.FindAction(
                    "Player/MusicSwitch"
                );
        }

        if (_MusicSwitchAction == null)
        {
            Debug.LogWarning(
                "MusicManager: не найден Player/MusicSwitch!",
                this
            );
        }

        // Запускаем одну корутину, которая будет
        // управлять всем плейлистом до конца игры.
        StartCoroutine(PlayMusicLoop());
    }


    // ------------------------------------
    // ПЕРЕКЛЮЧЕНИЕ МУЗЫКИ
    // ------------------------------------

    private void Update()
    {
        if (_MusicSwitchAction == null)
        {
            return;
        }

        if (_MusicSwitchAction.WasPressedThisFrame())
        {
            SkipMusic();
        }
    }


    private void SkipMusic()
    {
        // Во время смерти не разрешаем
        // переключать композиции.
        if (AudioListener.pause)
        {
            return;
        }

        if (_AudioSource == null ||
            !_AudioSource.isPlaying)
        {
            return;
        }

        // Мы только останавливаем текущий трек.
        // PlayMusicLoop сам заметит, что музыка
        // закончилась, и выберет следующую.
        _AudioSource.Stop();
    }


    // ------------------------------------
    // ПОИСК МУЗЫКИ
    // ------------------------------------

    private void RefreshTrackList()
    {
        _RemainingTracks.Clear();

        // Формируем путь к папке с MP3.
        // Application.streamingAssetsPath работает
        // и в редакторе, и в собранной игре.
        string folderPath = Path.Combine(
            Application.streamingAssetsPath,
            musicFolderName
        );

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning(
                "MusicManager: папка не найдена: " +
                folderPath
            );

            return;
        }

        string[] files = Directory.GetFiles(folderPath);

        // Сохраняем пути только к MP3.
        // Сами аудиофайлы пока не загружаем.
        foreach (string file in files)
        {
            if (Path.GetExtension(file).Equals(
                ".mp3",
                StringComparison.OrdinalIgnoreCase))
            {
                _RemainingTracks.Add(file);
            }
        }

        Debug.Log(
            "MusicManager: найдено треков: " +
            _RemainingTracks.Count
        );
    }


    // ------------------------------------
    // СЛУЧАЙНЫЙ ПЛЕЙЛИСТ
    // ------------------------------------

    private IEnumerator PlayMusicLoop()
    {
        // Бесконечный цикл, который не блокирует
        // игру благодаря yield return внутри.
        while (true)
        {
            // Если несыгранные треки закончились,
            // начинаем новый круг плейлиста.
            if (_RemainingTracks.Count == 0)
            {
                RefreshTrackList();

                // Если музыки нет, периодически
                // проверяем, не появились ли файлы.
                if (_RemainingTracks.Count == 0)
                {
                    yield return new WaitForSecondsRealtime(5f);
                    continue;
                }
            }


            // Выбираем случайный индекс из списка.
            int randomIndex = UnityEngine.Random.Range(
                0,
                _RemainingTracks.Count
            );

            // Защита от повторения одной песни
            // на границе двух кругов плейлиста.
            if (_RemainingTracks.Count > 1 &&
                _RemainingTracks[randomIndex] == _LastTrack)
            {
                randomIndex =
                    (randomIndex + 1) % _RemainingTracks.Count;
            }


            string trackPath = _RemainingTracks[randomIndex];

            // Убираем выбранный трек из списка.
            // Пока список не опустеет, он больше
            // не сможет выпасть случайно.
            _RemainingTracks.RemoveAt(randomIndex);

            _LastTrack = trackPath;


            // UnityWebRequest работает с URI.
            // Так корректно обрабатываются пути
            // с пробелами и русскими символами.
            string fileUri =
                new Uri(trackPath).AbsoluteUri;


            // ====================================
            // ЗАГРУЗКА MP3
            // ====================================

            // Создаём запрос для загрузки MP3.
            // using автоматически освободит ресурсы
            // запроса после завершения блока.
            using (UnityWebRequest request =
                UnityWebRequestMultimedia.GetAudioClip(
                    fileUri,
                    AudioType.MPEG))
            {
                DownloadHandlerAudioClip downloadHandler =
                    request.downloadHandler as DownloadHandlerAudioClip;

                if (downloadHandler != null)
                {
                    // Потоковый AudioClip позволяет
                    // не хранить всю распакованную
                    // композицию в оперативной памяти.
                    downloadHandler.streamAudio = true;
                }


                // Асинхронно загружаем MP3.
                // Корутина ждёт завершения запроса,
                // но остальная игра продолжает работать.
                yield return request.SendWebRequest();


                if (request.result !=
                    UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(
                        "MusicManager: ошибка загрузки " +
                        trackPath +
                        "\n" + request.error
                    );

                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }


                // Получаем AudioClip из обработчика загрузки.
                // Теперь это готовый объект, который
                // можно передать в AudioSource.
                AudioClip clip =
                    DownloadHandlerAudioClip.GetContent(request);

                if (clip == null)
                {
                    Debug.LogWarning(
                        "MusicManager: не удалось получить AudioClip: " +
                        trackPath
                    );

                    continue;
                }


                // ====================================
                // ВОСПРОИЗВЕДЕНИЕ
                // ====================================

                _AudioSource.clip = clip;
                _AudioSource.volume = musicVolume;

                _AudioSource.Play();

                Debug.Log(
                    "Сейчас играет: " +
                    Path.GetFileName(trackPath)
                );


                // Ждём, пока композиция играет.
                //
                // MusicSwitch вызывает Stop(), поэтому
                // цикл завершится и начнётся новый трек.
                //
                // AudioListener.pause не позволяет
                // переключить музыку во время смерти.
                while (_AudioSource.isPlaying ||
                       AudioListener.pause)
                {
                    yield return null;
                }


                // ====================================
                // ОЧИСТКА
                // ====================================

                // После завершения отвязываем клип
                // и уничтожаем загруженный AudioClip,
                // чтобы не накапливать музыку в памяти.
                _AudioSource.Stop();
                _AudioSource.clip = null;

                Destroy(clip);
            }

            // Возвращаемся к началу while(true)
            // и выбираем следующую случайную песню.
        }
    }
}
