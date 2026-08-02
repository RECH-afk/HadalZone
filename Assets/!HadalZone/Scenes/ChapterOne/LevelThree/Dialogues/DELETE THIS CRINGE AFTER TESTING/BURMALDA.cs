using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BURMALDA : MonoBehaviour
{
    [Header("Настройки песен")]
    [SerializeField] private List<AudioClip> songList = new List<AudioClip>(); // Список песен
    [SerializeField] private bool shuffleSongs = true; // Перемешивать песни
    [SerializeField] private bool playOnStart = true; // Начать воспроизведение при старте

    [Header("Настройки переключения песен")]
    [SerializeField] private bool changeSongRandomly = true; // Переключать песни случайно
    [SerializeField] private float minSongChangeInterval = 10f; // Мин интервал между сменами песен
    [SerializeField] private float maxSongChangeInterval = 30f; // Макс интервал между сменами песен
    [SerializeField] private bool changeOnSongEnd = true; // Менять когда песня закончилась

    [Header("Настройки Pitch")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    [Header("Настройки времени Pitch")]
    [SerializeField] private float minChangeInterval = 0.5f;
    [SerializeField] private float maxChangeInterval = 2.0f;

    [Header("Настройки перехода Pitch")]
    [SerializeField] private bool useSmoothTransition = false;
    [SerializeField] private float transitionDuration = 0.5f;

    private AudioSource audioSource;
    private Coroutine pitchCoroutine;
    private Coroutine songChangeCoroutine;
    private List<AudioClip> remainingSongs = new List<AudioClip>();
    private bool isPlaying = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Проверяем наличие песен
        if (songList.Count == 0)
        {
            Debug.LogWarning("BURMALDA: Список песен пуст! Добавьте аудио в список.");
            return;
        }

        // Создаем копию списка для перемешивания
        remainingSongs = new List<AudioClip>(songList);

        if (playOnStart)
        {
            PlayRandomSong();
        }
    }

    void Update()
    {
        // Проверяем, закончилась ли песня
        if (changeOnSongEnd && isPlaying && !audioSource.isPlaying && audioSource.clip != null)
        {
            PlayRandomSong();
        }
    }

    // Воспроизвести случайную песню
    public void PlayRandomSong()
    {
        if (songList.Count == 0) return;

        AudioClip nextSong = GetNextSong();

        if (nextSong != null)
        {
            audioSource.clip = nextSong;
            audioSource.Play();
            isPlaying = true;

            // Запускаем изменение pitch для новой песни
            StartPitchChanging();

            // Запускаем таймер для смены песни (если включено)
            if (changeSongRandomly)
            {
                StartSongChangeTimer();
            }

        }
    }

    // Получить следующую песню
    private AudioClip GetNextSong()
    {
        if (shuffleSongs)
        {
            // Перемешанный режим
            if (remainingSongs.Count == 0)
            {
                // Если все песни сыграны, обновляем список
                remainingSongs = new List<AudioClip>(songList);
            }

            // Выбираем случайную песню
            int randomIndex = Random.Range(0, remainingSongs.Count);
            AudioClip selectedSong = remainingSongs[randomIndex];
            remainingSongs.RemoveAt(randomIndex);
            return selectedSong;
        }
        else
        {
            // Последовательный режим
            if (remainingSongs.Count == 0)
            {
                remainingSongs = new List<AudioClip>(songList);
            }

            AudioClip selectedSong = remainingSongs[0];
            remainingSongs.RemoveAt(0);
            return selectedSong;
        }
    }

    // Запустить таймер для смены песни
    private void StartSongChangeTimer()
    {
        if (songChangeCoroutine != null)
        {
            StopCoroutine(songChangeCoroutine);
        }
        songChangeCoroutine = StartCoroutine(SongChangeRoutine());
    }

    IEnumerator SongChangeRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minSongChangeInterval, maxSongChangeInterval);
            yield return new WaitForSeconds(waitTime);

            if (audioSource.isPlaying)
            {
                PlayRandomSong();
            }
        }
    }

    void StartPitchChanging()
    {
        if (pitchCoroutine != null)
        {
            StopCoroutine(pitchCoroutine);
        }
        pitchCoroutine = StartCoroutine(ChangePitchRoutine());
    }

    IEnumerator ChangePitchRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minChangeInterval, maxChangeInterval);
            yield return new WaitForSeconds(waitTime);

            float targetPitch = Random.Range(minPitch, maxPitch);

            if (useSmoothTransition)
            {
                yield return StartCoroutine(SmoothPitchChange(targetPitch, transitionDuration));
            }
            else
            {
                audioSource.pitch = targetPitch;
            }
        }
    }

    IEnumerator SmoothPitchChange(float targetPitch, float duration)
    {
        float startPitch = audioSource.pitch;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float smoothT = t * t * (3f - 2f * t);
            audioSource.pitch = Mathf.Lerp(startPitch, targetPitch, smoothT);
            yield return null;
        }
        audioSource.pitch = targetPitch;
    }

    public void RestartPitchChanging()
    {
        StartPitchChanging();
    }

    public void StopPitchChanging()
    {
        if (pitchCoroutine != null)
        {
            StopCoroutine(pitchCoroutine);
            pitchCoroutine = null;
        }
    }

    public void SetPitchRange(float newMin, float newMax)
    {
        minPitch = Mathf.Min(newMin, newMax);
        maxPitch = Mathf.Max(newMin, newMax);
    }

    public void SetChangeInterval(float newMinInterval, float newMaxInterval)
    {
        minChangeInterval = Mathf.Min(newMinInterval, newMaxInterval);
        maxChangeInterval = Mathf.Max(newMinInterval, newMaxInterval);
    }

    public void NextSong()
    {
        PlayRandomSong();
    }

    public void StopMusic()
    {
        audioSource.Stop();
        isPlaying = false;
        if (songChangeCoroutine != null)
        {
            StopCoroutine(songChangeCoroutine);
            songChangeCoroutine = null;
        }
        StopPitchChanging();
    }

    public void ResumeMusic()
    {
        if (audioSource.clip != null)
        {
            audioSource.Play();
            isPlaying = true;
            StartPitchChanging();
            if (changeSongRandomly)
            {
                StartSongChangeTimer();
            }
        }
        else
        {
            PlayRandomSong();
        }
    }

    public void AddSong(AudioClip newSong)
    {
        if (newSong != null && !songList.Contains(newSong))
        {
            songList.Add(newSong);
            remainingSongs.Add(newSong);
        }
    }

    public void RemoveSong(AudioClip songToRemove)
    {
        if (songList.Contains(songToRemove))
        {
            songList.Remove(songToRemove);
            remainingSongs.Remove(songToRemove);

            if (audioSource.clip == songToRemove)
            {
                PlayRandomSong();
            }
        }
    }

    public List<AudioClip> GetSongList()
    {
        return new List<AudioClip>(songList);
    }

    public AudioClip GetCurrentSong()
    {
        return audioSource.clip;
    }

    public bool IsPlaying()
    {
        return isPlaying && audioSource.isPlaying;
    }
}