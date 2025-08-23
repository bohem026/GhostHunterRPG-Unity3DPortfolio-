using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class StageUIManager : MonoBehaviour
{
    public static StageUIManager Inst;
    public static float FADE_SCREEN = 2.5f;
    public static float FADE_IMAGE = 0.66f;

    [Space(20)]
    [Header("UI\nFULL SCREEN")]
    public Image panel_Fade;
    [Header("HEADER")]
    [SerializeField] private GameObject popup_Instruct;
    [SerializeField] private GameObject popup_Intermission;
    [SerializeField] private GameObject HUD_Stage;
    [SerializeField] private Text text_Wave;
    [SerializeField] private Text text_MonLeft;
    [SerializeField] private Text text_TimeLeft;
    [SerializeField] private Text text_TimeLeftMilli;
    [Header("BODY")]
    public Image image_Wave;
    [SerializeField] private Sprite[] sprites_Wave;
    [Header("WINDOW")]
    [SerializeField] private GameObject window_Pause;
    [SerializeField] private GameObject window_Result;


    public bool IsPauseWindowOnDisplay { get; set; }
    float deltaWave = 0f;
    float deltaWaveAtom = 0f;
    bool doUpdateDeltaUIs = false;

    void Awake()
    {
        if (!Inst) Inst = this;
        IsPauseWindowOnDisplay = false;
    }

    void Update()
    {
        if (!doUpdateDeltaUIs) return;

        //1. Update text_MonLeft.
        text_MonLeft.text
            = (MonsterManager.Inst.InstMonsTotal()).ToString();

        //2. Update text_TimeLeft.
        int sec;
        text_TimeLeft.text
            = (sec = (int)deltaWave).ToString("00") + " :";
        text_TimeLeftMilli.text = ((deltaWave - sec) * 100f).ToString("00");
        Color color = text_TimeLeft.color;
        color.g = color.b
                = Mathf.Clamp
                (deltaWave / StageManager.DURATION_WAVE,
                0f,
                1f);
        text_TimeLeft.color = color;
        text_TimeLeftMilli.color = color;

        //시간(Wave)이 다 된 경우
        //      몬스터 남아 있을 경우 게임 오버
        //      몬스터 남아 있지 않은 경우 다음 웨이브
        //시간(Wave)이 다 되지 않은 경우
        //      몬스터 남아 있지 않은 경우 -> 다음 웨이브 진행

        //3-1. Go to next wave(Time-out).
        if ((deltaWave -= Time.deltaTime) <= 0f)
        {
            doUpdateDeltaUIs = false;
            deltaWave = 0f;

            //Mute BGM: Time tick.
            AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();

            if (MonsterManager.Inst.MONS_LEFT)
            {
                /*Test*/
                Debug.Log("GAME OVER");
                /*Test*/

                StartCoroutine(StageManager.Inst.GameOver(false));
            }
            else
            {
                //3부대가 모두 소환되어 웨이브가 종료된 상태.
                if (!StageManager.Inst.IsWaveInProgress)
                    StartCoroutine(StageManager.Inst.StartWave());
            }
        }
        //3-2. Check is time to summon next wave atom.
        else
        {
            //Play BGM: Time tick.
            if (AudioPlayerPoolManager.Instance.BGMTimeTickSource)
                if (deltaWave <= AudioPlayerPoolManager.TIMETICK_DURATION &&
                    !AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
                    AudioPlayerPoolManager.Instance.PlayBGMTimeTickClipLoop();

            if (CheckNextWaveAtom())
                MonsterPoolManager.Inst.SummonWaveAtom();
        }
    }

    public IEnumerator FadeIn(Image image, float duration)
    {
        //1. Initialize panel color: Black.
        Color color = image.color;
        color.a = 1f;
        float elapsedAlpha = 0f;
        image.color = color;

        //2. Reduce color alpha along fadeDuration.
        while (elapsedAlpha < duration)
        {
            elapsedAlpha += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedAlpha / duration);
            image.color = color;
            yield return null;
        }

        //3. Finalize panel color: Transparent.
        color.a = 0f;
        image.color = color;
    }

    public IEnumerator FadeOut(Image image, float duration)
    {
        Debug.Log("FadeOut Start");

        //1. Initialize panel color: Transparent.
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        float elapsedAlpha = 0f;

        //2. Increase color alpha along fadeDuration.
        while (elapsedAlpha < duration)
        {
            elapsedAlpha += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedAlpha / duration); // 안전한 보정
            color.a = Mathf.Lerp(0f, 1f, t);
            image.color = color;

            yield return null;
        }

        //3. Finalize panel color: Black.
        color.a = 1f;
        image.color = color;

        Debug.Log("FadeOut End");
    }

    /// <summary>
    /// 현재 Wave에 맞는 이미지 교체 메서드 입니다.
    /// </summary>
    public void UpdateWaveImage()
    {
        int currentWave = StageManager.Inst.CurrentWave;
        int index = Mathf.Clamp
                    (currentWave - 1,
                    0,
                    sprites_Wave.Length - 1);

        image_Wave.sprite = sprites_Wave[index];
    }

    public void UpdateStageHUD()
    {
        if (!HUD_Stage.activeSelf) HUD_Stage.SetActive(true);

        int currentWave = StageManager.Inst.CurrentWave;

        //1. Update wave number.
        text_Wave.text = $"WAVE {currentWave}";
        //2. Start updating delta UIs.
        deltaWave = StageManager.DURATION_WAVE;
        deltaWaveAtom = 0f;
        doUpdateDeltaUIs = true;
    }

    public void TogglePauseWindow()
    {
        if (!window_Pause.activeSelf)
        {
            window_Pause.SetActive(true);
        }
        else
        {
            window_Pause.SetActive(false);
        }
    }

    public void DisplayPauseWindow(bool command)
        => window_Pause.SetActive(command);

    public static void PopupInstruct(bool command)
        => Inst.popup_Instruct.SetActive(command);

    public static void PopupIntermission(bool command)
        => Inst.popup_Intermission.SetActive(command);

    public static void DisplayResultWindow(bool result)
    {
        Inst.window_Result.SetActive(true);
        Inst.window_Result.GetComponent<ResultWindowController>()
            .InitUIs(result);

        GameManager.Inst.ChangeMouseInputMode(1);
    }

    public bool DoUpdateDeltaUIs
    {
        get => doUpdateDeltaUIs;
        set => doUpdateDeltaUIs = value;
    }

    private bool CheckNextWaveAtom()
    {
        //1. Go to next wave atom if there's no mons left.
        if (!MonsterManager.Inst.MONS_LEFT)
            return true;

        //2. Continue if is last wave atom.
        if (StageManager.Inst.CurrentWaveAtom >= StageManager.MAX_WAVE_ATOM)
            return false;

        //3. Go to next atom if is out of time.
        if ((deltaWaveAtom += Time.deltaTime) >
            StageManager.DURATION_WAVE / StageManager.MAX_WAVE_ATOM)
        {
            deltaWaveAtom = 0f;
            return true;
        }
        else
        {
            return false;
        }
    }
}
