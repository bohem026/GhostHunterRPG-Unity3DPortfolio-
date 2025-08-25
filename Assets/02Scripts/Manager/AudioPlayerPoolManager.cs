using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM 및 SFX 재생을 관리하는 오디오 플레이어 풀 매니저.
/// - SFX 풀: Player/Monster/UI 타입별 AudioSource 리스트를 관리
/// - BGM: 기본, 크로스페이드, 타임틱 보조 트랙 관리
/// - 퍼포먼스: 재사용 가능한 AudioSource 풀로 할당/GC 비용 최소화
/// </summary>
public class AudioPlayerPoolManager : MonoBehaviour
{
    public static AudioPlayerPoolManager Instance { get; private set; }

    public static float SFX_VOLUME = 1f;
    public static float BGM_VOLUME = 0.5f;
    public static float TIMETICK_DURATION = 10f;

    public enum SFXPoolType { Player, Monster, UI, Count/*Length*/ }
    public enum SFXType { Click, Slider, Confirm, Alert, Clear, Lose, Count/*Length*/ }

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

    /// <summary>
    /// 싱글톤 초기화 및 파괴 방지 설정, 초기 풀 세팅(Init) 시작.
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (!isInitialized) Init();
    }

    /// <summary>
    /// BGM/SFX 오디오 소스 생성 및 SFX 풀 초기화.
    /// </summary>
    private void Init()
    {
        BGM = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();
        BGMNext = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();
        ResetBGMTimeTick();

        PlayerSFXs = new List<AudioSource>();
        MonsterSFXs = new List<AudioSource>();
        UISFXs = new List<AudioSource>();

        // 초기 풀 생성(절반 용량만 미리 할당)
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

    /// <summary>
    /// 지정된 타입의 SFX 소스를 새로 생성하고 풀에 추가.
    /// </summary>
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

    /// <summary>
    /// SFX 타입에 해당하는 풀(List<AudioSource>)을 반환.
    /// </summary>
    private List<AudioSource> GetSFXPoolByType(SFXPoolType type)
    {
        List<AudioSource> targetPool = null;

        switch (type)
        {
            case SFXPoolType.Player: targetPool = PlayerSFXs; break;
            case SFXPoolType.Monster: targetPool = MonsterSFXs; break;
            case SFXPoolType.UI: targetPool = UISFXs; break;
            default: break;
        }

        return targetPool;
    }

    /// <summary>
    /// 단일 SFX 클립을 재생하고, 재생 종료 후 릴리스 코루틴 실행.
    /// </summary>
    public void PlaySFXClipOnce(
        SFXPoolType Type,
        AudioClip clip,
        float volume = 1f)
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

    /// <summary>
    /// BGM 클립을 루프 재생.
    /// </summary>
    public void PlayBGMClipLoop(
        AudioClip clip,
        float volume = 0.5f)
    {
        BGM.clip = clip;
        BGM.volume = volume;
        BGM.loop = true;
        BGM.Play();
    }

    /// <summary>
    /// 타임틱 BGM을 재설정 후 일정 시간 재생.
    /// </summary>
    public void PlayBGMTimeTickClipLoop()
    {
        ResetBGMTimeTick();
        StartCoroutine(ElapseBGMTimeTick());
    }

    /// <summary>
    /// 타임틱 BGM 재생을 중지하고 상태 초기화.
    /// </summary>
    public void MuteBGMTimeTickClip()
    {
        StopCoroutine(ElapseBGMTimeTick());
        ResetBGMTimeTick();
    }

    /// <summary>
    /// 타임틱 오디오 소스를 준비 상태로 리셋.
    /// </summary>
    private void ResetBGMTimeTick()
    {
        if (!BGMTimeTick)
            BGMTimeTick = Instantiate(audioPlayerPrefab, transform).GetComponent<AudioSource>();

        BGMTimeTick.clip = bgmClip_TimeTick;
        BGMTimeTick.volume = 0f;
        BGMTimeTick.loop = true;
        BGMTimeTick.Stop();
    }

    /// <summary>
    /// BGM 볼륨을 일정 시간에 걸쳐 증가/감소.
    /// </summary>
    public void ControlBGMVolume(bool INC)
    {
        StartCoroutine(ElapseBGMVolume(INC));
    }

    /// <summary>
    /// 현재 BGM에서 다음 BGM으로 크로스페이드.
    /// </summary>
    public void PlayBGMClipCrossfade(
        AudioClip nextClip,
        float volume = 0.5f)
    {
        StartCoroutine(CrossfadeToNextClip(nextClip, volume));
    }

    public AudioSource BGMSource => BGM;
    public AudioSource BGMTimeTickSource => BGMTimeTick;

    /// <summary>
    /// 사전 정의된 UI SFX 타입으로 효과음을 재생.
    /// </summary>
    public void PlaySFXClipOnceByType(SFXType Type)
    {
        AudioClip sfxClip = null;

        switch (Type)
        {
            case SFXType.Click: sfxClip = sfxClip_Click; break;
            case SFXType.Slider: sfxClip = sfxClip_Slider; break;
            case SFXType.Confirm: sfxClip = sfxClip_Confirm; break;
            case SFXType.Alert: sfxClip = sfxClip_Alert; break;
            case SFXType.Clear: sfxClip = sfxClip_Clear; break;
            case SFXType.Lose: sfxClip = sfxClip_Lose; break;
            default: break;
        }

        if (sfxClip != null)
            PlaySFXClipOnce(SFXPoolType.UI, sfxClip, SFX_VOLUME);
    }

    /// <summary>
    /// 지정 타입의 SFX 풀에서 사용 가능한 AudioSource를 반환(없으면 새로 생성).
    /// </summary>
    private AudioSource Get(SFXPoolType Type)
    {
        List<AudioSource> targetPool = GetSFXPoolByType(Type);
        if (targetPool == null) return null;

        foreach (AudioSource src in targetPool)
        {
            if (!src.isPlaying) return src;
        }

        // 풀에 사용 가능한 소스가 없으면 새로 생성
        return CreateNewSFXSource(Type);
    }

    /// <summary>
    /// 타임틱 BGM을 지정된 시간(TIMETICK_DURATION) 동안 재생.
    /// </summary>
    IEnumerator ElapseBGMTimeTick()
    {
        BGMTimeTick.volume = BGM_VOLUME;
        BGMTimeTick.Play();

        yield return new WaitForSeconds(TIMETICK_DURATION);
        ResetBGMTimeTick();
    }

    /// <summary>
    /// BGM 볼륨을 3초 동안 선형 보간하여 증감.
    /// </summary>
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

    /// <summary>
    /// 3초 동안 현재 BGM을 줄이고 다음 BGM을 올리며 전환.
    /// </summary>
    IEnumerator CrossfadeToNextClip(
        AudioClip nextClip,
        float volume)
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

        // 소스 스왑
        var temp = BGM;
        BGM = BGMNext;
        BGMNext = temp;
    }

    /// <summary>
    /// 지정 AudioSource의 재생이 끝날 때까지 대기 후 클립 참조를 해제.
    /// </summary>
    IEnumerator Release(AudioSource source)
    {
        yield return new WaitWhile(() => source.isPlaying);
        source.clip = null;
    }
}
