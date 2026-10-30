using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Header("Music Folder")]

    // Папка с MP3 внутри StreamingAssets.
    public string musicFolderName = "PlayersMusic";


    [Header("Music Settings")]

    // Громкость музыки.
    [Range(0f, 1f)]
    public float musicVolume = 0.2f;


    [Header("Input System")]
    [SerializeField] private PlayerInput _PlayerInput;

    // Действие переключения музыки.
    private InputAction _MusicSwitchAction;


    // Источник воспроизведения музыки.
    private AudioSource _AudioSource;

    // Список треков, которые ещё не проигрывались.
    private List<string> _RemainingTracks = new List<string>();

    // Последняя проигранная композиция.
    // Нужна, чтобы один и тот же трек
    // не играл два раза подряд.
    private string _LastTrack;


    private void Awake()
    {
        _AudioSource = GetComponent<AudioSource>();

        // Музыку запускает только наш скрипт.
        _AudioSource.playOnAwake = false;

        // Зацикливать один трек не нужно.
        _AudioSource.loop = false;

        // Музыка не зависит от положения игрока.
        _AudioSource.spatialBlend = 0f;

        _AudioSource.volume = musicVolume;

        // Музыка должна останавливаться
        // вместе с остальными звуками при смерти.
        _AudioSource.ignoreListenerPause = false;
    }



    private void Start()
    {
        // MusicManager находится отдельно от игрока,
        // поэтому ищем PlayerInput во всей сцене.
        if (_PlayerInput == null)
        {
            _PlayerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (_PlayerInput != null)
        {
            // Получаем MusicSwitch из нашего
            // существующего DagChanControls.
            _MusicSwitchAction =
                _PlayerInput.actions.FindAction("Player/MusicSwitch");
        }

        if (_MusicSwitchAction == null)
        {
            Debug.LogWarning(
                "MusicManager: действие Player/MusicSwitch не найдено!",
                this
            );
        }

        // Запускаем случайное воспроизведение,
        // как и раньше.
        StartCoroutine(PlayMusicLoop());
    }


    private void Update()
    {
        // Если действие не найдено,
        // обрабатывать нажатия нечего.
        if (_MusicSwitchAction == null)
        {
            return;
        }

        // Проверяем нажатие MusicSwitch.
        if (_MusicSwitchAction.WasPressedThisFrame())
        {
            SkipMusic();
        }
    }


    // Переключение на следующую случайную песню.
    private void SkipMusic()
    {
        // Если сейчас нет музыки или игра
        // находится на паузе из-за смерти,
        // ничего не делаем.
        if (_AudioSource == null ||
            !_AudioSource.isPlaying ||
            AudioListener.pause)
        {
            return;
        }

        // Останавливаем текущую песню.
        _AudioSource.Stop();

        // Дополнительно запускать новую
        // композицию здесь НЕ нужно!
        //
        // Наша корутина PlayMusicLoop()
        // сама увидит, что музыка закончилась,
        // и выберет следующую случайную.
    }




    // ------------------------------------
    // ПОЛУЧЕНИЕ СПИСКА МУЗЫКИ
    // ------------------------------------

    private void RefreshTrackList()
    {
        _RemainingTracks.Clear();

        string folderPath = Path.Combine(
            Application.streamingAssetsPath,
            musicFolderName
        );

        // Проверяем существование папки.
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning(
                "MusicManager: папка не найдена: " +
                folderPath
            );

            return;
        }

        // Получаем все файлы внутри папки.
        string[] files = Directory.GetFiles(folderPath);

        foreach (string file in files)
        {
            // Добавляем только MP3.
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
    // БЕСКОНЕЧНОЕ ВОСПРОИЗВЕДЕНИЕ
    // ------------------------------------

    private IEnumerator PlayMusicLoop()
    {
        while (true)
        {
            // Если все композиции проиграны,
            // заново получаем полный список.
            if (_RemainingTracks.Count == 0)
            {
                RefreshTrackList();

                // Если музыки вообще нет,
                // ждём и пробуем снова.
                if (_RemainingTracks.Count == 0)
                {
                    yield return new WaitForSecondsRealtime(5f);
                    continue;
                }
            }


            // Выбираем случайный трек.
            int randomIndex = UnityEngine.Random.Range(
                0,
                _RemainingTracks.Count
            );


            // Не допускаем повторения последнего
            // трека на границе двух циклов.
            if (_RemainingTracks.Count > 1 &&
                _RemainingTracks[randomIndex] == _LastTrack)
            {
                randomIndex =
                    (randomIndex + 1) % _RemainingTracks.Count;
            }


            string trackPath = _RemainingTracks[randomIndex];

            // Удаляем выбранный трек из списка
            // ещё не проигранных композиций.
            _RemainingTracks.RemoveAt(randomIndex);

            _LastTrack = trackPath;


            // Превращаем путь в файловый URI.
            // Работает и с пробелами, и с кириллицей.
            string fileUri = new Uri(trackPath).AbsoluteUri;


            // Загружаем MP3.
            using (UnityWebRequest request =
                UnityWebRequestMultimedia.GetAudioClip(
                    fileUri,
                    AudioType.MPEG))
            {
                yield return request.SendWebRequest();


                // Если загрузка не удалась,
                // пропускаем этот файл.
                if (request.result !=
                    UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(
                        "MusicManager: ошибка загрузки " +
                        trackPath + "\n" + request.error
                    );

                    continue;
                }


                AudioClip clip =
                    DownloadHandlerAudioClip.GetContent(request);

                if (clip == null)
                {
                    continue;
                }


                // Назначаем и запускаем композицию.
                _AudioSource.clip = clip;
                _AudioSource.volume = musicVolume;

                _AudioSource.Play();

                Debug.Log(
                    "Сейчас играет: " +
                    Path.GetFileName(trackPath)
                );


                // Ждём окончания композиции.
                //
                // AudioListener.pause учитываем,
                // чтобы во время смерти игра
                // не переключала треки.
                while (_AudioSource.isPlaying ||
                       AudioListener.pause)
                {
                    yield return null;
                }


                // Очищаем старую композицию.
                _AudioSource.Stop();
                _AudioSource.clip = null;

                Destroy(clip);
            }

            // Цикл продолжается.
            // Выбираем следующую композицию.
        }
    }
}
