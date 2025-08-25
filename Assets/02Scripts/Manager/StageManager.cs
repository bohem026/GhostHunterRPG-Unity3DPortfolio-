using Cinemachine;
using System.Collections;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    // --- Stage Timings & Limits ---
    private const float DELAY_ENTRY = 5f;
    private const float DURATION_INSTRUCT = 3f;
    private const float DURATION_INTERMISSION = 5f;
    public const int MAX_WAVE = 3;
    public const int MAX_WAVE_ATOM = 3;
    public const float DURATION_WAVE = 90f; // wave 당 제한 시간(참조용)

    // --- Singleton ---
    public static StageManager Inst;

    [Space(20)]
    [Header("ASSET")]
    [SerializeField] private StageSO asset;

    [Space(20)]
    [Header("PRODUCTION")]
    [SerializeField] private CinemachineVirtualCamera[] vcams_Intro;
    [SerializeField] private bool IsIntermissionExist;

    // --- Components ---
    private PlayerController _PlyCtrl;
    private SphereTriggerZone currentSphereZone;

    // --- Runtime State ---
    public int CurrentWave { get; private set; }
    public int CurrentWaveAtom { get; set; }
    public float StagePlayTime { get; private set; }
    public int KillCount { get; set; }

    // --- Flags ---
    public bool IsIntroProductionFinished { get; private set; }
    public bool IsWaveInProgress { get; set; }
    public bool IsPaused { get; set; }
    public bool IsEndOfStage { get; set; }
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 싱글톤 설정.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// BGM 세팅, 초기화/연출 코루틴 시작.
    /// </summary>
    private void Start()
    {
        // TimeTick BGM 음소거 후 웨이브 BGM 재생
        AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();
        AudioPlayerPoolManager.Instance.PlayBGMClipLoop(
            asset.bgm_Wave1, AudioPlayerPoolManager.BGM_VOLUME);

        if (isInitialized) return;

        StartCoroutine(Init());
        StartCoroutine(LockCameraTemporarily());
        StartCoroutine(ProductIntro());
    }

    /// <summary>
    /// 스테이지 경과시간 갱신 및 Pause 처리.
    /// </summary>
    private void Update()
    {
        // 경과 시간 누적(인트로 종료 이후)
        if (IsIntroProductionFinished)
        {
            StagePlayTime += Time.unscaledDeltaTime;
        }

        // 일시정지 입력(Escape)
        IsPaused = Input.GetKeyDown(KeyCode.Escape);

        if (!isInitialized) return;
        if (!_PlyCtrl.inputEnabled) return;

        if (IsPaused)
        {
            // 버튼 클릭 SFX
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);

            IsPaused = false;
            StageUIManager.Inst.TogglePauseWindow();
        }
    }

    /// <summary>
    /// 필드/플래그 초기화.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst);

        _PlyCtrl = GameManager.Inst._plyCtrl;

        CurrentWave = 0;
        CurrentWaveAtom = 0;
        StagePlayTime = 0;
        KillCount = 0;

        IsIntroProductionFinished = false;
        IsWaveInProgress = false;
        IsEndOfStage = false;

        isInitialized = true;
    }

    /// <summary>
    /// 인트로 동안 카메라 입력을 잠시 막고, 진입 연출/안내 표시 후 해제.
    /// </summary>
    private IEnumerator LockCameraTemporarily()
    {
        float DUR_FADE = StageUIManager.FADE_SCREEN;

        yield return new WaitUntil(() => _PlyCtrl);

        // 카메라 입력 막기 및 초기 방향 설정
        _PlyCtrl.inputEnabled = false;
        _PlyCtrl.transform.eulerAngles = new Vector3(0, -180f, 0);

        // 인트로 종료 대기 → 페이드/안내 표기
        yield return new WaitUntil(() => IsIntroProductionFinished);
        StartCoroutine(StageUIManager.Inst.FadeIn(StageUIManager.Inst.panel_Fade, DUR_FADE));
        StartCoroutine(DisplayInstruct());

        // 입장 지연 후 입력 허용 및 UI 활성화
        yield return new WaitForSeconds(DELAY_ENTRY);
        _PlyCtrl.inputEnabled = true;
        UIManager.Inst.ActivateUIRoot(true);
        _PlyCtrl.ResetVCamRotation();
    }

    /// <summary>
    /// 인트로용 가상 카메라들을 순회하며 간단 연출 후 인트로 종료 플래그 설정.
    /// </summary>
    private IEnumerator ProductIntro()
    {
        float DUR_FADE = StageUIManager.FADE_SCREEN;

        foreach (var vcam in vcams_Intro)
        {
            StartCoroutine(StageUIManager.Inst.FadeIn(StageUIManager.Inst.panel_Fade, DUR_FADE));
            yield return new WaitForSecondsRealtime(DUR_FADE);

            StartCoroutine(StageUIManager.Inst.FadeOut(StageUIManager.Inst.panel_Fade, DUR_FADE));
            yield return new WaitForSecondsRealtime(DUR_FADE);

            vcam.Priority = 0;
            yield return new WaitForSecondsRealtime(1.75f);
        }

        IsIntroProductionFinished = true;
    }

    /// <summary>
    /// 입장 후 일정 시간 동안 안내 문구 표시.
    /// </summary>
    private IEnumerator DisplayInstruct()
    {
        yield return new WaitForSeconds(DELAY_ENTRY - DURATION_INSTRUCT);
        StageUIManager.PopupInstruct(true);

        yield return new WaitForSeconds(DURATION_INSTRUCT);
        StageUIManager.PopupInstruct(false);
    }

    /// <summary>
    /// 인터미션 UI 표시/대기/숨김.
    /// </summary>
    private IEnumerator DisplayIntermission()
    {
        StageUIManager.PopupIntermission(true);
        yield return new WaitForSeconds(DURATION_INTERMISSION);
        StageUIManager.PopupIntermission(false);
    }

    /// <summary>
    /// 게임 오버 처리(클리어/실패 공통): 시간정지, 오디오 전환, 페이드, 결과창 표시.
    /// </summary>
    public IEnumerator GameOver(bool clear)
    {
        Time.timeScale = 0.0f;

        // TimeTick BGM 정리 및 메인 BGM 페이드 아웃
        Destroy(AudioPlayerPoolManager.Instance.BGMTimeTickSource.gameObject);
        AudioPlayerPoolManager.Instance.ControlBGMVolume(false);

        // 입력 차단 및 커서 모드 변경
        _PlyCtrl.inputEnabled = false;
        GameManager.Inst.ChangeMouseInputMode(0);

        // 페이드 아웃
        yield return StartCoroutine(StageUIManager.Inst.FadeOut(
            StageUIManager.Inst.panel_Fade, StageUIManager.FADE_SCREEN));

        // 즉시 페이드 인 상태로 복구(알파=0) + 커서 모드 변경
        var color = StageUIManager.Inst.panel_Fade.color;
        color.a = 0f;
        StageUIManager.Inst.panel_Fade.color = color;
        GameManager.Inst.ChangeMouseInputMode(1);

        // 보상 처리 및 결과창 표기
        if (clear) ClaimRewards();
        StageUIManager.DisplayResultWindow(clear);

        IsEndOfStage = true;
    }

    /// <summary>
    /// 스테이지 클리어 보상 계산 및 저장.
    /// </summary>
    private void ClaimRewards()
    {
        // 최고 스테이지 갱신 및 젬 보상
        if (asset.NUM_STAGE >= GlobalValue.Instance.GetBestStageFromInfo())
        {
            GlobalValue.Instance._Info.BEST_STAGE += 1;
            GlobalValue.Instance._Inven.STGEM_CNT += asset.stGemCount;
            GlobalValue.Instance._Inven.SKGEM_CNT += asset.skGemCount;

            GlobalValue.Instance._Info.FIRST_IN_GAME = false;
            GlobalValue.Instance._Info.STGEM_CLAIMED = true;
            GlobalValue.Instance._Info.SKGEM_CLAIMED = true;
        }

        // 장비 보상
        int elapsed = 0;
        foreach (var item in asset.rewardGears)
        {
            int count = GetRewardGearCountByRarity(item.OUTER);
            GlobalValue.Instance.ElapseGearCountByEnum(
                item.OUTER, item.INNER, GlobalValue.GearCommand.Get, count);

            elapsed += count;
        }

        if (elapsed > 0)
            GlobalValue.Instance._Info.GEAR_CLAIMED = true;

        GlobalValue.Instance.SaveInfo();
        GlobalValue.Instance.SaveInven();
    }

    /// <summary>
    /// 희귀도에 따른 장비 보상 수량을 계산.
    /// </summary>
    private int GetRewardGearCountByRarity(GearController.Rarity rarity)
    {
        int maxCount = 0;
        int minCount = 0;
        float claimRate = 0f;

        switch (rarity)
        {
            case GearController.Rarity.Common:
                maxCount = 3; minCount = 2; claimRate = 1f; break;
            case GearController.Rarity.Rare:
                maxCount = 2; minCount = 1; claimRate = 0.75f; break;
            case GearController.Rarity.Unique:
                maxCount = 1; minCount = 0; claimRate = 0.5f; break;
            case GearController.Rarity.Legendary:
                maxCount = 1; minCount = 0; claimRate = 0.25f; break;
            default:
                return 0;
        }

        float ranValue = Random.Range(0f, 1f);
        return ranValue < claimRate ? maxCount : minCount;
    }

    /// <summary>
    /// 웨이브 시작 연출 및 BGM 전환, HUD 갱신, 초기 소환을 수행한다.
    /// (인터미션 처리/클리어 조건 포함)
    /// </summary>
    public IEnumerator StartWave()
    {
        IsWaveInProgress = true;
        StageUIManager.Inst.DoUpdateDeltaUIs = false;

        // 두 번째 구역으로 전환하는 시점(인터미션)
        if (IsIntermissionExist && (CurrentWave == MAX_WAVE - 1))
        {
            IsIntermissionExist = false;
            currentSphereZone.SphereTrapZone.DeactivateByForce();
            StartCoroutine(DisplayIntermission());
            yield break;
        }

        // 웨이브 증가 및 클리어 체크
        if (++CurrentWave > MAX_WAVE)
        {
            yield return StartCoroutine(GameOver(true));
            yield break;
        }

        float DUR_FADE = StageUIManager.FADE_IMAGE;

        // 1) 웨이브 이미지 표시/페이드
        StageUIManager.Inst.UpdateWaveImage();
        StartCoroutine(StageUIManager.Inst.FadeOut(StageUIManager.Inst.image_Wave, DUR_FADE));
        yield return new WaitForSeconds(2f);
        StartCoroutine(StageUIManager.Inst.FadeIn(StageUIManager.Inst.image_Wave, DUR_FADE));

        // 2) 웨이브별 BGM 크로스페이드
        switch (CurrentWave)
        {
            case 2:
                AudioPlayerPoolManager.Instance.PlayBGMClipCrossfade(
                    asset.bgm_Wave2, AudioPlayerPoolManager.BGM_VOLUME);
                break;
            case 3:
                AudioPlayerPoolManager.Instance.PlayBGMClipCrossfade(
                    asset.bgm_Wave3, AudioPlayerPoolManager.BGM_VOLUME);
                break;
            default:
                break;
        }
        yield return new WaitForSeconds(DUR_FADE);

        // 3) HUD 갱신
        StageUIManager.Inst.UpdateStageHUD();

        // 4) 첫 소환
        StageManager.Inst.CurrentWaveAtom = 0;
        MonsterPoolManager.Inst.SummonWaveAtom();
    }

    // --- Properties / Helpers ---
    public StageSO Asset => asset;
    public SphereTriggerZone CurrentSphereZone => currentSphereZone;

    /// <summary>
    /// 스테이지 전체 몬스터 수에서 처치 수를 뺀 남은 수를 반환.
    /// </summary>
    public int GetRestMonCount()
    {
        return GetMonCountPerStage() - KillCount;
    }

    /// <summary>
    /// 스테이지에 배정된 총 몬스터 수를 계산.
    /// </summary>
    private int GetMonCountPerStage()
    {
        int result = 0;
        foreach (var entry in asset.entries)
        {
            foreach (var count in entry.countPerWave)
            {
                result += count;
            }
        }
        return result;
    }

    /// <summary>
    /// 현재 스피어 존을 갱신한다.
    /// </summary>
    public void UpdateCurrentSphereZone(SphereTriggerZone sphereZone)
        => currentSphereZone = sphereZone;
}
