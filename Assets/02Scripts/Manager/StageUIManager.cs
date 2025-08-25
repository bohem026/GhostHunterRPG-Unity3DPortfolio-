using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageUIManager : MonoBehaviour
{
    public static StageUIManager Inst;

    // 화면/이미지 페이드 시간(외부에서 참조)
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

    // 상태 값
    public bool IsPauseWindowOnDisplay { get; set; }
    private float deltaWave = 0f;      // 현재 웨이브 경과/잔여 시간(표시용)
    private float deltaWaveAtom = 0f;  // 원자 웨이브 간 시간 누적
    private bool doUpdateDeltaUIs = false;

    /// <summary>
    /// 싱글톤 설정 및 일시정지 창 상태 초기화.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
        IsPauseWindowOnDisplay = false;
    }

    /// <summary>
    /// 웨이브 잔여시간/몬스터 수 UI를 갱신하고,
    /// 시간 경과에 따라 다음 단계(게임오버/다음 웨이브/원자 소환)를 트리거한다.
    /// </summary>
    private void Update()
    {
        if (!doUpdateDeltaUIs) return;

        // 1) 남은 몬스터 수 표시
        text_MonLeft.text = (MonsterManager.Inst.InstMonsTotal()).ToString();

        // 2) 남은 시간 표시(초/밀리초) + 남은 비율에 따른 색상 보정
        int sec;
        text_TimeLeft.text = (sec = (int)deltaWave).ToString("00") + " :";
        text_TimeLeftMilli.text = ((deltaWave - sec) * 100f).ToString("00");

        Color color = text_TimeLeft.color;
        color.g = color.b = Mathf.Clamp(
            deltaWave / StageManager.DURATION_WAVE, 0f, 1f);
        text_TimeLeft.color = color;
        text_TimeLeftMilli.color = color;

        // 3) 시간 처리
        if ((deltaWave -= Time.deltaTime) <= 0f)
        {
            // 3-1) 웨이브 시간 종료
            doUpdateDeltaUIs = false;
            deltaWave = 0f;

            // 타임틱 BGM 중지
            AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();

            if (MonsterManager.Inst.MONS_LEFT)
            {
                // 몬스터가 남아 있으면 게임오버
                StartCoroutine(StageManager.Inst.GameOver(false));
            }
            else
            {
                // 모든 원자 소환이 끝났고 진행 중이 아니면 다음 웨이브
                if (!StageManager.Inst.IsWaveInProgress)
                    StartCoroutine(StageManager.Inst.StartWave());
            }
        }
        else
        {
            // 3-2) 잔여 시간이 적으면 타임틱 BGM 재생
            if (AudioPlayerPoolManager.Instance.BGMTimeTickSource)
                if (deltaWave <= AudioPlayerPoolManager.TIMETICK_DURATION &&
                    !AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
                    AudioPlayerPoolManager.Instance.PlayBGMTimeTickClipLoop();

            // 3-3) 다음 원자 웨이브 시점이면 소환
            if (CheckNextWaveAtom())
                MonsterPoolManager.Inst.SummonWaveAtom();
        }
    }

    /// <summary>
    /// 지정 이미지를 주어진 시간 동안 페이드 인(불투명 → 투명)한다.
    /// </summary>
    public IEnumerator FadeIn(Image image, float duration)
    {
        // 1) 시작: 불투명
        Color color = image.color;
        color.a = 1f;
        float elapsedAlpha = 0f;
        image.color = color;

        // 2) 경과 시간에 비례하여 알파 감소
        while (elapsedAlpha < duration)
        {
            elapsedAlpha += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedAlpha / duration);
            image.color = color;
            yield return null;
        }

        // 3) 종료: 완전 투명
        color.a = 0f;
        image.color = color;
    }

    /// <summary>
    /// 지정 이미지를 주어진 시간 동안 페이드 아웃(투명 → 불투명)한다.
    /// </summary>
    public IEnumerator FadeOut(Image image, float duration)
    {
        // 1) 시작: 투명
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        float elapsedAlpha = 0f;

        // 2) 경과 시간에 비례하여 알파 증가
        while (elapsedAlpha < duration)
        {
            elapsedAlpha += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedAlpha / duration);
            color.a = Mathf.Lerp(0f, 1f, t);
            image.color = color;
            yield return null;
        }

        // 3) 종료: 완전 불투명
        color.a = 1f;
        image.color = color;
    }

    /// <summary>
    /// 현재 웨이브에 해당하는 스프라이트로 교체한다.
    /// </summary>
    public void UpdateWaveImage()
    {
        int currentWave = StageManager.Inst.CurrentWave;
        int index = Mathf.Clamp(currentWave - 1, 0, sprites_Wave.Length - 1);
        image_Wave.sprite = sprites_Wave[index];
    }

    /// <summary>
    /// 스테이지 HUD를 표시하고 웨이브 타이머 갱신을 시작한다.
    /// </summary>
    public void UpdateStageHUD()
    {
        if (!HUD_Stage.activeSelf) HUD_Stage.SetActive(true);

        int currentWave = StageManager.Inst.CurrentWave;

        // 1) 웨이브 번호 표시
        text_Wave.text = $"WAVE {currentWave}";
        // 2) 타이머 초기화 및 갱신 시작
        deltaWave = StageManager.DURATION_WAVE;
        deltaWaveAtom = 0f;
        doUpdateDeltaUIs = true;
    }

    /// <summary>
    /// 일시정지 창 토글.
    /// </summary>
    public void TogglePauseWindow()
    {
        if (!window_Pause.activeSelf)
            window_Pause.SetActive(true);
        else
            window_Pause.SetActive(false);
    }

    /// <summary>
    /// 일시정지 창 표시/숨김.
    /// </summary>
    public void DisplayPauseWindow(bool command)
        => window_Pause.SetActive(command);

    /// <summary>
    /// 입장 안내 팝업 표시/숨김.
    /// </summary>
    public static void PopupInstruct(bool command)
        => Inst.popup_Instruct.SetActive(command);

    /// <summary>
    /// 인터미션 팝업 표시/숨김.
    /// </summary>
    public static void PopupIntermission(bool command)
        => Inst.popup_Intermission.SetActive(command);

    /// <summary>
    /// 결과 창을 표시하고 마우스 입력 모드를 UI 가능 상태로 전환한다.
    /// </summary>
    public static void DisplayResultWindow(bool result)
    {
        Inst.window_Result.SetActive(true);
        Inst.window_Result.GetComponent<ResultWindowController>().InitUIs(result);
        GameManager.Inst.ChangeMouseInputMode(1);
    }

    /// <summary>
    /// 웨이브 진행 중 시간/몬스터 상태 갱신 여부.
    /// </summary>
    public bool DoUpdateDeltaUIs
    {
        get => doUpdateDeltaUIs;
        set => doUpdateDeltaUIs = value;
    }

    /// <summary>
    /// 다음 원자 웨이브를 소환할 타이밍인지 판단한다.
    /// - 몬스터가 모두 사라짐 → 즉시 true
    /// - 마지막 원자 단계에 도달 → false
    /// - 구간 시간 초과 → true
    /// </summary>
    private bool CheckNextWaveAtom()
    {
        // 1) 남은 몬스터가 없으면 바로 다음 원자 웨이브
        if (!MonsterManager.Inst.MONS_LEFT)
            return true;

        // 2) 이미 마지막 원자 단계이면 대기
        if (StageManager.Inst.CurrentWaveAtom >= StageManager.MAX_WAVE_ATOM)
            return false;

        // 3) 구간 시간(총 웨이브 시간 / 원자 수) 초과 시 다음 원자 웨이브
        if ((deltaWaveAtom += Time.deltaTime) >
            StageManager.DURATION_WAVE / StageManager.MAX_WAVE_ATOM)
        {
            deltaWaveAtom = 0f;
            return true;
        }

        return false;
    }
}
