using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseWindowController : MonoBehaviour
{
    private const string NAME_OF_BASE_SCENE = "LevelBaseScene";

    [Space(20)]
    [Header("FULL")]
    [SerializeField] private Button buttonArea_Close;

    [Space(20)]
    [Header("BODY")]
    [SerializeField] private Text text_Wave;
    [SerializeField] private Text text_Time;
    [SerializeField] private Text text_Rest;

    [Space(20)]
    [Header("FOOT")]
    [SerializeField] private Button button_Resume;
    [SerializeField] private Button button_Retry;
    [SerializeField] private Button button_Quit;

    private bool isInitialized = false;

    /// <summary>
    /// 버튼 리스너 등 1회성 초기화를 수행한다.
    /// </summary>
    private void Awake()
    {
        if (!isInitialized) Init();
    }

    /// <summary>
    /// 매 프레임: 현재 웨이브, 경과 시간, 남은 몬스터 수 UI를 갱신한다.
    /// </summary>
    private void Update()
    {
        text_Wave.text = $"{StageManager.Inst.CurrentWave} / {StageManager.MAX_WAVE}";

        int totalSeconds = Mathf.FloorToInt(StageManager.Inst.StagePlayTime);
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        text_Time.text = $"{minutes:00} : {seconds:00}";

        text_Rest.text = $"{StageManager.Inst.GetRestMonCount()}";
    }

    /// <summary>
    /// 버튼 콜백 연결 등 내부 초기화.
    /// </summary>
    private void Init()
    {
        AddButtonListeners();
        isInitialized = true;
    }

    /// <summary>
    /// 버튼 클릭 시 동작을 등록한다. (재개/재시작/나가기)
    /// </summary>
    private void AddButtonListeners()
    {
        // FULL 영역 클릭: 창 닫기
        buttonArea_Close.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        // 재개
        button_Resume.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        // 재시작: 로딩 씬을 통해 현재 씬 재로딩(+ 베이스 씬)
        button_Retry.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

            Scene currentScene = SceneManager.GetActiveScene();
            LoadingSceneManager.LoadSceneWithLoading(currentScene.name, NAME_OF_BASE_SCENE);
        });

        // 나가기: 결과 창(실패)로 처리
        button_Quit.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StartCoroutine(StageManager.Inst.GameOver(false));
        });
    }

    /// <summary>
    /// 일시정지 창 표시 시: 마우스/시간/오디오 상태를 일시정지 모드로 전환한다.
    /// </summary>
    private void OnEnable()
    {
        StageUIManager.Inst.IsPauseWindowOnDisplay = true;

        // 타이머 사운드 음소거
        if (AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.volume = 0f;

        // BGM 볼륨 감쇠
        if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.volume *= 0.33f;

        // 마우스 커서 해제
        GameManager.Inst.ChangeMouseInputMode(1);

        // 게임 시간 정지
        Time.timeScale = 0.0f;
    }

    /// <summary>
    /// 일시정지 창 종료 시: 마우스/시간/오디오 상태를 재개한다.
    /// </summary>
    private void OnDisable()
    {
        // 타이머 사운드 복구
        if (AudioPlayerPoolManager.Instance.BGMTimeTickSource &&
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
        {
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.volume = AudioPlayerPoolManager.SFX_VOLUME;
        }

        // BGM 볼륨 복구
        if (AudioPlayerPoolManager.Instance.BGMSource &&
            AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
        {
            AudioPlayerPoolManager.Instance.BGMSource.volume = AudioPlayerPoolManager.BGM_VOLUME;
        }

        // 마우스 커서 잠금
        GameManager.Inst.ChangeMouseInputMode(0);

        // 게임 시간 재개
        Time.timeScale = 1.0f;

        StageUIManager.Inst.IsPauseWindowOnDisplay = false;
    }
}
