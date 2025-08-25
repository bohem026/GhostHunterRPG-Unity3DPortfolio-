using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageUIManager : MonoBehaviour
{
    public static StageUIManager Inst;

    // ȭ��/�̹��� ���̵� �ð�(�ܺο��� ����)
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

    // ���� ��
    public bool IsPauseWindowOnDisplay { get; set; }
    private float deltaWave = 0f;      // ���� ���̺� ���/�ܿ� �ð�(ǥ�ÿ�)
    private float deltaWaveAtom = 0f;  // ���� ���̺� �� �ð� ����
    private bool doUpdateDeltaUIs = false;

    /// <summary>
    /// �̱��� ���� �� �Ͻ����� â ���� �ʱ�ȭ.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
        IsPauseWindowOnDisplay = false;
    }

    /// <summary>
    /// ���̺� �ܿ��ð�/���� �� UI�� �����ϰ�,
    /// �ð� ����� ���� ���� �ܰ�(���ӿ���/���� ���̺�/���� ��ȯ)�� Ʈ�����Ѵ�.
    /// </summary>
    private void Update()
    {
        if (!doUpdateDeltaUIs) return;

        // 1) ���� ���� �� ǥ��
        text_MonLeft.text = (MonsterManager.Inst.InstMonsTotal()).ToString();

        // 2) ���� �ð� ǥ��(��/�и���) + ���� ������ ���� ���� ����
        int sec;
        text_TimeLeft.text = (sec = (int)deltaWave).ToString("00") + " :";
        text_TimeLeftMilli.text = ((deltaWave - sec) * 100f).ToString("00");

        Color color = text_TimeLeft.color;
        color.g = color.b = Mathf.Clamp(
            deltaWave / StageManager.DURATION_WAVE, 0f, 1f);
        text_TimeLeft.color = color;
        text_TimeLeftMilli.color = color;

        // 3) �ð� ó��
        if ((deltaWave -= Time.deltaTime) <= 0f)
        {
            // 3-1) ���̺� �ð� ����
            doUpdateDeltaUIs = false;
            deltaWave = 0f;

            // Ÿ��ƽ BGM ����
            AudioPlayerPoolManager.Instance.MuteBGMTimeTickClip();

            if (MonsterManager.Inst.MONS_LEFT)
            {
                // ���Ͱ� ���� ������ ���ӿ���
                StartCoroutine(StageManager.Inst.GameOver(false));
            }
            else
            {
                // ��� ���� ��ȯ�� ������ ���� ���� �ƴϸ� ���� ���̺�
                if (!StageManager.Inst.IsWaveInProgress)
                    StartCoroutine(StageManager.Inst.StartWave());
            }
        }
        else
        {
            // 3-2) �ܿ� �ð��� ������ Ÿ��ƽ BGM ���
            if (AudioPlayerPoolManager.Instance.BGMTimeTickSource)
                if (deltaWave <= AudioPlayerPoolManager.TIMETICK_DURATION &&
                    !AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
                    AudioPlayerPoolManager.Instance.PlayBGMTimeTickClipLoop();

            // 3-3) ���� ���� ���̺� �����̸� ��ȯ
            if (CheckNextWaveAtom())
                MonsterPoolManager.Inst.SummonWaveAtom();
        }
    }

    /// <summary>
    /// ���� �̹����� �־��� �ð� ���� ���̵� ��(������ �� ����)�Ѵ�.
    /// </summary>
    public IEnumerator FadeIn(Image image, float duration)
    {
        // 1) ����: ������
        Color color = image.color;
        color.a = 1f;
        float elapsedAlpha = 0f;
        image.color = color;

        // 2) ��� �ð��� ����Ͽ� ���� ����
        while (elapsedAlpha < duration)
        {
            elapsedAlpha += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedAlpha / duration);
            image.color = color;
            yield return null;
        }

        // 3) ����: ���� ����
        color.a = 0f;
        image.color = color;
    }

    /// <summary>
    /// ���� �̹����� �־��� �ð� ���� ���̵� �ƿ�(���� �� ������)�Ѵ�.
    /// </summary>
    public IEnumerator FadeOut(Image image, float duration)
    {
        // 1) ����: ����
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        float elapsedAlpha = 0f;

        // 2) ��� �ð��� ����Ͽ� ���� ����
        while (elapsedAlpha < duration)
        {
            elapsedAlpha += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedAlpha / duration);
            color.a = Mathf.Lerp(0f, 1f, t);
            image.color = color;
            yield return null;
        }

        // 3) ����: ���� ������
        color.a = 1f;
        image.color = color;
    }

    /// <summary>
    /// ���� ���̺꿡 �ش��ϴ� ��������Ʈ�� ��ü�Ѵ�.
    /// </summary>
    public void UpdateWaveImage()
    {
        int currentWave = StageManager.Inst.CurrentWave;
        int index = Mathf.Clamp(currentWave - 1, 0, sprites_Wave.Length - 1);
        image_Wave.sprite = sprites_Wave[index];
    }

    /// <summary>
    /// �������� HUD�� ǥ���ϰ� ���̺� Ÿ�̸� ������ �����Ѵ�.
    /// </summary>
    public void UpdateStageHUD()
    {
        if (!HUD_Stage.activeSelf) HUD_Stage.SetActive(true);

        int currentWave = StageManager.Inst.CurrentWave;

        // 1) ���̺� ��ȣ ǥ��
        text_Wave.text = $"WAVE {currentWave}";
        // 2) Ÿ�̸� �ʱ�ȭ �� ���� ����
        deltaWave = StageManager.DURATION_WAVE;
        deltaWaveAtom = 0f;
        doUpdateDeltaUIs = true;
    }

    /// <summary>
    /// �Ͻ����� â ���.
    /// </summary>
    public void TogglePauseWindow()
    {
        if (!window_Pause.activeSelf)
            window_Pause.SetActive(true);
        else
            window_Pause.SetActive(false);
    }

    /// <summary>
    /// �Ͻ����� â ǥ��/����.
    /// </summary>
    public void DisplayPauseWindow(bool command)
        => window_Pause.SetActive(command);

    /// <summary>
    /// ���� �ȳ� �˾� ǥ��/����.
    /// </summary>
    public static void PopupInstruct(bool command)
        => Inst.popup_Instruct.SetActive(command);

    /// <summary>
    /// ���͹̼� �˾� ǥ��/����.
    /// </summary>
    public static void PopupIntermission(bool command)
        => Inst.popup_Intermission.SetActive(command);

    /// <summary>
    /// ��� â�� ǥ���ϰ� ���콺 �Է� ��带 UI ���� ���·� ��ȯ�Ѵ�.
    /// </summary>
    public static void DisplayResultWindow(bool result)
    {
        Inst.window_Result.SetActive(true);
        Inst.window_Result.GetComponent<ResultWindowController>().InitUIs(result);
        GameManager.Inst.ChangeMouseInputMode(1);
    }

    /// <summary>
    /// ���̺� ���� �� �ð�/���� ���� ���� ����.
    /// </summary>
    public bool DoUpdateDeltaUIs
    {
        get => doUpdateDeltaUIs;
        set => doUpdateDeltaUIs = value;
    }

    /// <summary>
    /// ���� ���� ���̺긦 ��ȯ�� Ÿ�̹����� �Ǵ��Ѵ�.
    /// - ���Ͱ� ��� ����� �� ��� true
    /// - ������ ���� �ܰ迡 ���� �� false
    /// - ���� �ð� �ʰ� �� true
    /// </summary>
    private bool CheckNextWaveAtom()
    {
        // 1) ���� ���Ͱ� ������ �ٷ� ���� ���� ���̺�
        if (!MonsterManager.Inst.MONS_LEFT)
            return true;

        // 2) �̹� ������ ���� �ܰ��̸� ���
        if (StageManager.Inst.CurrentWaveAtom >= StageManager.MAX_WAVE_ATOM)
            return false;

        // 3) ���� �ð�(�� ���̺� �ð� / ���� ��) �ʰ� �� ���� ���� ���̺�
        if ((deltaWaveAtom += Time.deltaTime) >
            StageManager.DURATION_WAVE / StageManager.MAX_WAVE_ATOM)
        {
            deltaWaveAtom = 0f;
            return true;
        }

        return false;
    }
}
