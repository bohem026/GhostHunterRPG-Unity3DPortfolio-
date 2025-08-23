using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AudioPlayerPoolManager : MonoBehaviour
{
    public static AudioPlayerPoolManager Instance { get; private set; }

    public static float SFX_VOLUME = 1f;
    public static float BGM_VOLUME = 0.5f;
    public static float TIMETICK_DURATION = 10f;

    public enum SFXPoolType
    { Player, Monster, UI, Count/*Length*/}
    public enum SFXType
    { Click, Slider, Confirm, Alert, Clear, Lose, Count/*Length*/}

    [Space(20)]
    [Header("POOL INFO")]
    [SerializeField] private GameObject audioPlayerPrefab;
    [SerializeField]
    [Range(10, 100)] private int MAX_SIZE = 10;

    [Space(20)]
    [Header("BASIC SOUND CLIP")]
    [SerializeField] private AudioClip bgmClip_TimeTick;
    [SerializeField] private AudioClip sfxClip_Click;
    [SerializeField] private AudioClip sfxClip_Slider;
    [SerializeField] private AudioClip sfxClip_Confirm;
    [SerializeField] private AudioClip sfxClip_Alert;
    [SerializeField] private AudioClip sfxClip_Clear;
    [SerializeField] private AudioClip sfxClip_Lose;

    AudioSource BGM;
    AudioSource BGMNext;
    AudioSource BGMTimeTick;
    List<AudioSource> PlayerSFXs;
    List<AudioSource> MonsterSFXs;
    List<AudioSource> UISFXs;

    bool isInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (!isInitialized) Init();
    }

    private void Init()
    {
        BGM = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();
        BGMNext = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();
        //BGMTimeTick = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();
        ResetBGMTimeTick();

        PlayerSFXs = new List<AudioSource>();
        MonsterSFXs = new List<AudioSource>();
        UISFXs = new List<AudioSource>();

        //Create primitive pool.
        for (int type = 0; type < (int)SFXPoolType.Count; type++)
        {
            for (int size = 0; size < MAX_SIZE / 2; size++)
            {
                int captured = type;
                SFXPoolType capturedType = (SFXPoolType)captured;

                CreateNewSFXSource(capturedType);
            }
        }

        isInitialized = true;
    }

    private AudioSource CreateNewSFXSource(SFXPoolType Type)
    {
        List<AudioSource> targetPool = GetSFXPoolByType(Type);
        if (targetPool == null) return null;

        if (targetPool.Count >= MAX_SIZE)
            return null;

        GameObject go = Instantiate(audioPlayerPrefab, transform);
        AudioSource source = go.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        targetPool.Add(source);

        return source;
    }

    private List<AudioSource> GetSFXPoolByType(SFXPoolType type)
    {
        List<AudioSource> targetPool = null;

        switch (type)
        {
            case SFXPoolType.Player:
                targetPool = PlayerSFXs;
                break;
            case SFXPoolType.Monster:
                targetPool = MonsterSFXs;
                break;
            case SFXPoolType.UI:
                targetPool = UISFXs;
                break;
            default:
                break;
        }

        return targetPool;
    }

    public void PlaySFXClipOnce
        (
        SFXPoolType Type,
        AudioClip clip,
        float volume = 1f
        )
    {
        AudioSource source = Get(Type);
        if (source == null) return;
        if (clip == null) return;

        source.clip = clip;
        source.volume = volume;
        source.loop = false;
        source.Play();

        StartCoroutine(Release(source));
    }

    public void PlayBGMClipLoop
        (
        AudioClip clip,
        float volume = 0.5f
        )
    {
        BGM.clip = clip;
        BGM.volume = volume;
        BGM.loop = true;
        BGM.Play();
    }

    public void PlayBGMTimeTickClipLoop()
    {
        ResetBGMTimeTick();
        StartCoroutine(ElapseBGMTimeTick());
    }

    IEnumerator ElapseBGMTimeTick()
    {
        BGMTimeTick.volume = BGM_VOLUME;
        BGMTimeTick.Play();

        yield return new WaitForSeconds(TIMETICK_DURATION);
        ResetBGMTimeTick();
    }

    public void MuteBGMTimeTickClip()
    {
        StopCoroutine(ElapseBGMTimeTick());
        ResetBGMTimeTick();
    }

    private void ResetBGMTimeTick()
    {
        if(!BGMTimeTick)
            BGMTimeTick = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();

        BGMTimeTick.clip = bgmClip_TimeTick;
        BGMTimeTick.volume = 0f;
        BGMTimeTick.loop = true;
        BGMTimeTick.Stop();
    }

    public void ControlBGMVolume(bool INC)
    {
        StartCoroutine(ElapseBGMVolume(INC));
    }

    IEnumerator ElapseBGMVolume(bool INC)
    {
        float delta = 0f;
        float currentVolume = BGM.volume;
        float duration = 3f;

        float finalVolume = 0f;

        if (INC)
        {
            if (BGM.volume >= BGM_VOLUME)
            {
                BGM.volume = BGM_VOLUME;
                yield break;
            }
            finalVolume = BGM_VOLUME;
        }
        else
        {
            if (BGM.volume <= 0f)
            {
                BGM.volume = 0f;
                yield break;
            }
            finalVolume = 0f;
        }

        while (delta < duration)
        {
            delta += Time.unscaledDeltaTime;
            float t = delta / duration;
            BGM.volume = Mathf.Lerp(currentVolume, finalVolume, t);

            yield return null;
        }

        BGM.volume = finalVolume;
    }

    public void PlayBGMClipCrossfade
        (
        AudioClip nextClip,
        float volume = 0.5f
        )
    {
        StartCoroutine(CrossfadeToNextClip(nextClip, volume));
    }

    IEnumerator CrossfadeToNextClip
        (
        AudioClip nextClip,
        float volume
        )
    {
        BGMNext.clip = nextClip;
        BGMNext.volume = 0f;
        BGMNext.loop = true;
        BGMNext.Play();

        float delta = 0f;
        float currentVolume = BGM.volume;
        float crossfadeDuration = 3f;

        while (delta < crossfadeDuration)
        {
            delta += Time.deltaTime;
            float t = delta / crossfadeDuration;

            BGM.volume = Mathf.Lerp(currentVolume, 0f, t);
            BGMNext.volume = Mathf.Lerp(0f, volume, t);

            yield return null;
        }

        //Swap to next BGM.
        var temp = BGM;
        BGM = BGMNext;
        BGMNext = temp;
    }

    public AudioSource BGMSource => BGM;
    public AudioSource BGMTimeTickSource => BGMTimeTick;

    public void PlaySFXClipOnceByType(SFXType Type)
    {
        AudioClip sfxClip = null;

        switch (Type)
        {
            case SFXType.Click:
                sfxClip = sfxClip_Click;
                break;
            case SFXType.Slider:
                sfxClip = sfxClip_Slider;
                break;
            case SFXType.Confirm:
                sfxClip = sfxClip_Confirm;
                break;
            case SFXType.Alert:
                sfxClip = sfxClip_Alert;
                break;
            case SFXType.Clear:
                sfxClip = sfxClip_Clear;
                break;
            case SFXType.Lose:
                sfxClip = sfxClip_Lose;
                break;
            default:
                break;
        }

        if (sfxClip)
        {
            PlaySFXClipOnce
            (AudioPlayerPoolManager.SFXPoolType.UI,
            sfxClip,
            SFX_VOLUME);
        }
    }

    private AudioSource Get(SFXPoolType Type)
    {
        List<AudioSource> targetPool = GetSFXPoolByType(Type);
        if (targetPool == null) return null;

        foreach (AudioSource src in targetPool)
        {
            if (!src.isPlaying) return src;
        }

        //Create another one if there's no available one.
        return CreateNewSFXSource(Type);
    }

    private IEnumerator Release(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);
        source.clip = null;
    }
}
