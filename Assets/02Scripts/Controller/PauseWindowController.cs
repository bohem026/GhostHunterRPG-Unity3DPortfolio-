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
    /// ��ư ������ �� 1ȸ�� �ʱ�ȭ�� �����Ѵ�.
    /// </summary>
    private void Awake()
    {
        if (!isInitialized) Init();
    }

    /// <summary>
    /// �� ������: ���� ���̺�, ��� �ð�, ���� ���� �� UI�� �����Ѵ�.
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
    /// ��ư �ݹ� ���� �� ���� �ʱ�ȭ.
    /// </summary>
    private void Init()
    {
        AddButtonListeners();
        isInitialized = true;
    }

    /// <summary>
    /// ��ư Ŭ�� �� ������ ����Ѵ�. (�簳/�����/������)
    /// </summary>
    private void AddButtonListeners()
    {
        // FULL ���� Ŭ��: â �ݱ�
        buttonArea_Close.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        // �簳
        button_Resume.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        // �����: �ε� ���� ���� ���� �� ��ε�(+ ���̽� ��)
        button_Retry.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

            Scene currentScene = SceneManager.GetActiveScene();
            LoadingSceneManager.LoadSceneWithLoading(currentScene.name, NAME_OF_BASE_SCENE);
        });

        // ������: ��� â(����)�� ó��
        button_Quit.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StartCoroutine(StageManager.Inst.GameOver(false));
        });
    }

    /// <summary>
    /// �Ͻ����� â ǥ�� ��: ���콺/�ð�/����� ���¸� �Ͻ����� ���� ��ȯ�Ѵ�.
    /// </summary>
    private void OnEnable()
    {
        StageUIManager.Inst.IsPauseWindowOnDisplay = true;

        // Ÿ�̸� ���� ���Ұ�
        if (AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.volume = 0f;

        // BGM ���� ����
        if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.volume *= 0.33f;

        // ���콺 Ŀ�� ����
        GameManager.Inst.ChangeMouseInputMode(1);

        // ���� �ð� ����
        Time.timeScale = 0.0f;
    }

    /// <summary>
    /// �Ͻ����� â ���� ��: ���콺/�ð�/����� ���¸� �簳�Ѵ�.
    /// </summary>
    private void OnDisable()
    {
        // Ÿ�̸� ���� ����
        if (AudioPlayerPoolManager.Instance.BGMTimeTickSource &&
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
        {
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.volume = AudioPlayerPoolManager.SFX_VOLUME;
        }

        // BGM ���� ����
        if (AudioPlayerPoolManager.Instance.BGMSource &&
            AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
        {
            AudioPlayerPoolManager.Instance.BGMSource.volume = AudioPlayerPoolManager.BGM_VOLUME;
        }

        // ���콺 Ŀ�� ���
        GameManager.Inst.ChangeMouseInputMode(0);

        // ���� �ð� �簳
        Time.timeScale = 1.0f;

        StageUIManager.Inst.IsPauseWindowOnDisplay = false;
    }
}
