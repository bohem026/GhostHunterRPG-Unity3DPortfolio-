using Cinemachine;
using JetBrains.Annotations;
//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StageManager : MonoBehaviour
{
    private const float DELAY_ENTRY = 5f;
    private const float DURATION_INSTRUCT = 3f;
    private const float DURATION_INTERMISSION = 5f;
    public const int MAX_WAVE = 3;
    public const int MAX_WAVE_ATOM = 3;
    //Duration per wave.
    public const float DURATION_WAVE = 90f;

    public static StageManager Inst;

    [Space(20)]
    [Header("ASSET")]
    [SerializeField] private StageSO asset;
    [Space(20)]
    [Header("PRODUCTION")]
    [SerializeField]
    private CinemachineVirtualCamera[] vcams_Intro;
    [SerializeField]
    private bool IsIntermissionExist;

    // [Component]
    PlayerController _PlyCtrl;
    SphereTriggerZone currentSphereZone;
    // [Variable]
    public int CurrentWave { get; private set; }
    public int CurrentWaveAtom { get; set; }
    public float StagePlayTime { get; private set; }
    public int KillCount { get; set; }
    // [Flag]
    public bool IsIntroProductionFinished { get; private set; }
    public bool IsWaveInProgress { get; set; }
    public bool IsPaused { get; set; }
    public bool IsEndOfStage { get; set; }
    bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    void Awake()
    {
        if (!Inst) Inst = this;
    }

    void Start()
    {
        //Mute BGM: Time Tick.
        AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();
        //Play BGM: Wave.
        AudioPlayerPoolManager.Instance.PlayBGMClipLoop
            (asset.bgm_Wave1, AudioPlayerPoolManager.BGM_VOLUME);

        if (isInitialized) return;

        StartCoroutine(Init());
        StartCoroutine(LockCameraTemporarily());
        StartCoroutine(ProductIntro());
    }

    void Update()
    {
        //--- Elapse stage play time.
        if (IsIntroProductionFinished)
        {
            StagePlayTime += Time.unscaledDeltaTime;
        }
        //---

        //--- Pause function.
        //IsPaused = Input.GetKeyDown(KeyCode.P);
        IsPaused = Input.GetKeyDown(KeyCode.Escape);

        if (!isInitialized) return;
        if (!_PlyCtrl.inputEnabled) return;
        if (IsPaused)
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            IsPaused = false;
            StageUIManager.Inst.TogglePauseWindow();
        }
        //---
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst);

        //Component.
        _PlyCtrl = GameManager.Inst._plyCtrl;
        //Variable.
        CurrentWave = 0;
        CurrentWaveAtom = 0;
        StagePlayTime = 0;
        KillCount = 0;
        //Flag.
        IsIntroProductionFinished = false;
        IsWaveInProgress = false;
        IsEndOfStage = false;
        isInitialized = true;
    }

    IEnumerator LockCameraTemporarily()
    {
        float DUR_FADE = StageUIManager.FADE_SCREEN;

        yield return new WaitUntil(() => _PlyCtrl);
        // 카메라 입력 막기
        _PlyCtrl.inputEnabled = false;
        // 캐릭터 방향 초기화
        Vector3 lookDirection = new Vector3(0, -180f, 0);
        _PlyCtrl.transform.eulerAngles = lookDirection;

        yield return new WaitUntil(() => IsIntroProductionFinished);
        StartCoroutine(StageUIManager.Inst.FadeIn
            (StageUIManager.Inst.panel_Fade,
            DUR_FADE));
        StartCoroutine(DisplayInstruct());

        yield return new WaitForSeconds(DELAY_ENTRY);
        // 카메라 입력 허용
        _PlyCtrl.inputEnabled = true;
        UIManager.Inst.ActivateUIRoot(true);
        _PlyCtrl.ResetVCamRotation();
    }

    IEnumerator ProductIntro()
    {
        float DUR_FADE = StageUIManager.FADE_SCREEN;

        foreach (var item in vcams_Intro)
        {
            StartCoroutine(StageUIManager.Inst.FadeIn
                (StageUIManager.Inst.panel_Fade,
                DUR_FADE));

            yield return new WaitForSecondsRealtime(DUR_FADE);
            StartCoroutine(StageUIManager.Inst.FadeOut
                (StageUIManager.Inst.panel_Fade,
                DUR_FADE));

            yield return new WaitForSecondsRealtime(DUR_FADE);
            item.Priority = 0;

            yield return new WaitForSecondsRealtime(1.75f);
        }

        IsIntroProductionFinished = true;
    }

    IEnumerator DisplayInstruct()
    {
        yield return new WaitForSeconds(DELAY_ENTRY - DURATION_INSTRUCT);
        StageUIManager.PopupInstruct(true);

        yield return new WaitForSeconds(DURATION_INSTRUCT);
        StageUIManager.PopupInstruct(false);
    }

    IEnumerator DisplayIntermission()
    {
        StageUIManager.PopupIntermission(true);

        yield return new WaitForSeconds(DURATION_INTERMISSION);
        StageUIManager.PopupIntermission(false);
    }

    public IEnumerator GameOver(bool clear)
    {
        Time.timeScale = 0.0f;

        //AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();
        Destroy(AudioPlayerPoolManager.Instance.BGMTimeTickSource.gameObject);
        AudioPlayerPoolManager.Instance.ControlBGMVolume(false);

        //1. Refuse player's input.
        _PlyCtrl.inputEnabled = false;
        GameManager.Inst.ChangeMouseInputMode(0);

        //2. Fade-out.
        yield return StartCoroutine(StageUIManager.Inst.FadeOut
            (StageUIManager.Inst.panel_Fade,
            StageUIManager.FADE_SCREEN));

        //3. Fade-in instantly and Allow player's input.
        Color color = StageUIManager.Inst.panel_Fade.color;
        color.a = 0f;
        StageUIManager.Inst.panel_Fade.color = color;
        GameManager.Inst.ChangeMouseInputMode(1);

        //--- REWARD
        if (clear) ClaimRewards();
        StageUIManager.DisplayResultWindow(clear);
        //---

        IsEndOfStage = true;
    }

    private void ClaimRewards()
    {
        if (asset.NUM_STAGE >= GlobalValue.Instance.GetBestStageFromInfo())
        {
            GlobalValue.Instance._Info.BEST_STAGE += 1;
            GlobalValue.Instance._Inven.STGEM_CNT += asset.stGemCount;
            GlobalValue.Instance._Inven.SKGEM_CNT += asset.skGemCount;

            GlobalValue.Instance._Info.FIRST_IN_GAME = false;
            GlobalValue.Instance._Info.STGEM_CLAIMED = true;
            GlobalValue.Instance._Info.SKGEM_CLAIMED = true;
        }

        int count = 0;
        int elapsed = 0;
        foreach (var item in asset.rewardGears)
        {
            count = GetRewardGearCountByRarity(item.OUTER);
            GlobalValue.Instance.ElapseGearCountByEnum
                (item.OUTER,
                item.INNER,
                GlobalValue.GearCommand.Get,
                count);

            elapsed += count;
        }

        if (elapsed > 0)
            GlobalValue.Instance._Info.GEAR_CLAIMED = true;

        GlobalValue.Instance.SaveInfo();
        GlobalValue.Instance.SaveInven();
    }

    private int GetRewardGearCountByRarity(GearController.Rarity rarity)
    {
        int maxCount = 0;
        int minCount = 0;
        float claimRate = 0f;
        switch (rarity)
        {
            case GearController.Rarity.Common:
                maxCount = 3;
                minCount = 2;
                claimRate = 1f;
                break;
            case GearController.Rarity.Rare:
                maxCount = 2;
                minCount = 1;
                claimRate = 0.75f;
                break;
            case GearController.Rarity.Unique:
                maxCount = 1;
                minCount = 0;
                claimRate = 0.5f;
                break;
            case GearController.Rarity.Legendary:
                maxCount = 1;
                minCount = 0;
                claimRate = 0.25f;
                break;
            default:
                return 0;
        }

        float ranValue = Random.Range(0f, 1f);
        return ranValue < claimRate ? maxCount : minCount;
    }

    public IEnumerator StartWave()
    {
        IsWaveInProgress = true;
        StageUIManager.Inst.DoUpdateDeltaUIs = false;

        /*Test*/
        //두 번째 구역으로 전환하는 시점.
        if (IsIntermissionExist && (CurrentWave == MAX_WAVE - 1))
        {
            IsIntermissionExist = false;
            currentSphereZone.SphereTrapZone.DeactivateByForce();
            StartCoroutine(DisplayIntermission());

            yield break;
        }
        /*Test*/

        if (++CurrentWave > MAX_WAVE)
        {
            /*Test*/
            //yield return StartCoroutine(StageUIManager.Inst.FadeOut
            //    (StageUIManager.Inst.panel_Fade,
            //    StageUIManager.FADE_SCREEN));
            //Debug.Log("GAME CLEAR");
            //IsStageCleared = true;
            yield return StartCoroutine(GameOver(true));
            yield break;
            /*Test*/
        }

        float DUR_FADE = StageUIManager.FADE_IMAGE;

        //1. Display wave start image.
        StageUIManager.Inst.UpdateWaveImage();

        //Wave image Fade-out.
        StartCoroutine(StageUIManager.Inst.FadeOut
                (StageUIManager.Inst.image_Wave,
                DUR_FADE));

        //yield return new WaitForSecondsRealtime(2f);
        yield return new WaitForSeconds(2f);

        //Wave image Fade-in
        StartCoroutine(StageUIManager.Inst.FadeIn
            (StageUIManager.Inst.image_Wave,
            DUR_FADE));

        //Crossfade wave BGM.
        switch (CurrentWave)
        {
            case 2:
                AudioPlayerPoolManager.Instance.PlayBGMClipCrossfade
                    (asset.bgm_Wave2,
                    AudioPlayerPoolManager.BGM_VOLUME);
                break;
            case 3:
                AudioPlayerPoolManager.Instance.PlayBGMClipCrossfade
                    (asset.bgm_Wave3,
                    AudioPlayerPoolManager.BGM_VOLUME);
                break;
            default:
                break;
        }
        //yield return new WaitForSecondsRealtime(DUR_FADE);
        yield return new WaitForSeconds(DUR_FADE);

        //2. Display and update stage HUD.
        StageUIManager.Inst.UpdateStageHUD();

        //3. Summon first bunch of monsters.
        StageManager.Inst.CurrentWaveAtom = 0;
        MonsterPoolManager.Inst.SummonWaveAtom();
    }

    public StageSO Asset => asset;
    public SphereTriggerZone CurrentSphereZone
        => currentSphereZone;

    public int GetRestMonCount()
    {
        int result = GetMonCountPerStage();
        result -= KillCount;

        return result;
    }

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

    public void UpdateCurrentSphereZone(SphereTriggerZone sphereZone)
        => currentSphereZone = sphereZone;

    //2.    trigger에 부딪혀서 trapzone 생성 시 카운트다운(3초)
    //3.    스테이지 제한 시간 출력(약 90초)
    //4.    stageAsset 받아와서 해당 에셋에 포함된 몬스터들 생성
    //5.    대략 3웨이브 진행
}