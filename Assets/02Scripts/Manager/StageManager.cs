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
    public const float DURATION_WAVE = 90f; // wave �� ���� �ð�(������)

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
    /// �̱��� ����.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// BGM ����, �ʱ�ȭ/���� �ڷ�ƾ ����.
    /// </summary>
    private void Start()
    {
        // TimeTick BGM ���Ұ� �� ���̺� BGM ���
        AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();
        AudioPlayerPoolManager.Instance.PlayBGMClipLoop(
            asset.bgm_Wave1, AudioPlayerPoolManager.BGM_VOLUME);

        if (isInitialized) return;

        StartCoroutine(Init());
        StartCoroutine(LockCameraTemporarily());
        StartCoroutine(ProductIntro());
    }

    /// <summary>
    /// �������� ����ð� ���� �� Pause ó��.
    /// </summary>
    private void Update()
    {
        // ��� �ð� ����(��Ʈ�� ���� ����)
        if (IsIntroProductionFinished)
        {
            StagePlayTime += Time.unscaledDeltaTime;
        }

        // �Ͻ����� �Է�(Escape)
        IsPaused = Input.GetKeyDown(KeyCode.Escape);

        if (!isInitialized) return;
        if (!_PlyCtrl.inputEnabled) return;

        if (IsPaused)
        {
            // ��ư Ŭ�� SFX
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);

            IsPaused = false;
            StageUIManager.Inst.TogglePauseWindow();
        }
    }

    /// <summary>
    /// �ʵ�/�÷��� �ʱ�ȭ.
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
    /// ��Ʈ�� ���� ī�޶� �Է��� ��� ����, ���� ����/�ȳ� ǥ�� �� ����.
    /// </summary>
    private IEnumerator LockCameraTemporarily()
    {
        float DUR_FADE = StageUIManager.FADE_SCREEN;

        yield return new WaitUntil(() => _PlyCtrl);

        // ī�޶� �Է� ���� �� �ʱ� ���� ����
        _PlyCtrl.inputEnabled = false;
        _PlyCtrl.transform.eulerAngles = new Vector3(0, -180f, 0);

        // ��Ʈ�� ���� ��� �� ���̵�/�ȳ� ǥ��
        yield return new WaitUntil(() => IsIntroProductionFinished);
        StartCoroutine(StageUIManager.Inst.FadeIn(StageUIManager.Inst.panel_Fade, DUR_FADE));
        StartCoroutine(DisplayInstruct());

        // ���� ���� �� �Է� ��� �� UI Ȱ��ȭ
        yield return new WaitForSeconds(DELAY_ENTRY);
        _PlyCtrl.inputEnabled = true;
        UIManager.Inst.ActivateUIRoot(true);
        _PlyCtrl.ResetVCamRotation();
    }

    /// <summary>
    /// ��Ʈ�ο� ���� ī�޶���� ��ȸ�ϸ� ���� ���� �� ��Ʈ�� ���� �÷��� ����.
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
    /// ���� �� ���� �ð� ���� �ȳ� ���� ǥ��.
    /// </summary>
    private IEnumerator DisplayInstruct()
    {
        yield return new WaitForSeconds(DELAY_ENTRY - DURATION_INSTRUCT);
        StageUIManager.PopupInstruct(true);

        yield return new WaitForSeconds(DURATION_INSTRUCT);
        StageUIManager.PopupInstruct(false);
    }

    /// <summary>
    /// ���͹̼� UI ǥ��/���/����.
    /// </summary>
    private IEnumerator DisplayIntermission()
    {
        StageUIManager.PopupIntermission(true);
        yield return new WaitForSeconds(DURATION_INTERMISSION);
        StageUIManager.PopupIntermission(false);
    }

    /// <summary>
    /// ���� ���� ó��(Ŭ����/���� ����): �ð�����, ����� ��ȯ, ���̵�, ���â ǥ��.
    /// </summary>
    public IEnumerator GameOver(bool clear)
    {
        Time.timeScale = 0.0f;

        // TimeTick BGM ���� �� ���� BGM ���̵� �ƿ�
        Destroy(AudioPlayerPoolManager.Instance.BGMTimeTickSource.gameObject);
        AudioPlayerPoolManager.Instance.ControlBGMVolume(false);

        // �Է� ���� �� Ŀ�� ��� ����
        _PlyCtrl.inputEnabled = false;
        GameManager.Inst.ChangeMouseInputMode(0);

        // ���̵� �ƿ�
        yield return StartCoroutine(StageUIManager.Inst.FadeOut(
            StageUIManager.Inst.panel_Fade, StageUIManager.FADE_SCREEN));

        // ��� ���̵� �� ���·� ����(����=0) + Ŀ�� ��� ����
        var color = StageUIManager.Inst.panel_Fade.color;
        color.a = 0f;
        StageUIManager.Inst.panel_Fade.color = color;
        GameManager.Inst.ChangeMouseInputMode(1);

        // ���� ó�� �� ���â ǥ��
        if (clear) ClaimRewards();
        StageUIManager.DisplayResultWindow(clear);

        IsEndOfStage = true;
    }

    /// <summary>
    /// �������� Ŭ���� ���� ��� �� ����.
    /// </summary>
    private void ClaimRewards()
    {
        // �ְ� �������� ���� �� �� ����
        if (asset.NUM_STAGE >= GlobalValue.Instance.GetBestStageFromInfo())
        {
            GlobalValue.Instance._Info.BEST_STAGE += 1;
            GlobalValue.Instance._Inven.STGEM_CNT += asset.stGemCount;
            GlobalValue.Instance._Inven.SKGEM_CNT += asset.skGemCount;

            GlobalValue.Instance._Info.FIRST_IN_GAME = false;
            GlobalValue.Instance._Info.STGEM_CLAIMED = true;
            GlobalValue.Instance._Info.SKGEM_CLAIMED = true;
        }

        // ��� ����
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
    /// ��͵��� ���� ��� ���� ������ ���.
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
    /// ���̺� ���� ���� �� BGM ��ȯ, HUD ����, �ʱ� ��ȯ�� �����Ѵ�.
    /// (���͹̼� ó��/Ŭ���� ���� ����)
    /// </summary>
    public IEnumerator StartWave()
    {
        IsWaveInProgress = true;
        StageUIManager.Inst.DoUpdateDeltaUIs = false;

        // �� ��° �������� ��ȯ�ϴ� ����(���͹̼�)
        if (IsIntermissionExist && (CurrentWave == MAX_WAVE - 1))
        {
            IsIntermissionExist = false;
            currentSphereZone.SphereTrapZone.DeactivateByForce();
            StartCoroutine(DisplayIntermission());
            yield break;
        }

        // ���̺� ���� �� Ŭ���� üũ
        if (++CurrentWave > MAX_WAVE)
        {
            yield return StartCoroutine(GameOver(true));
            yield break;
        }

        float DUR_FADE = StageUIManager.FADE_IMAGE;

        // 1) ���̺� �̹��� ǥ��/���̵�
        StageUIManager.Inst.UpdateWaveImage();
        StartCoroutine(StageUIManager.Inst.FadeOut(StageUIManager.Inst.image_Wave, DUR_FADE));
        yield return new WaitForSeconds(2f);
        StartCoroutine(StageUIManager.Inst.FadeIn(StageUIManager.Inst.image_Wave, DUR_FADE));

        // 2) ���̺꺰 BGM ũ�ν����̵�
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

        // 3) HUD ����
        StageUIManager.Inst.UpdateStageHUD();

        // 4) ù ��ȯ
        StageManager.Inst.CurrentWaveAtom = 0;
        MonsterPoolManager.Inst.SummonWaveAtom();
    }

    // --- Properties / Helpers ---
    public StageSO Asset => asset;
    public SphereTriggerZone CurrentSphereZone => currentSphereZone;

    /// <summary>
    /// �������� ��ü ���� ������ óġ ���� �� ���� ���� ��ȯ.
    /// </summary>
    public int GetRestMonCount()
    {
        return GetMonCountPerStage() - KillCount;
    }

    /// <summary>
    /// ���������� ������ �� ���� ���� ���.
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
    /// ���� ���Ǿ� ���� �����Ѵ�.
    /// </summary>
    public void UpdateCurrentSphereZone(SphereTriggerZone sphereZone)
        => currentSphereZone = sphereZone;
}
