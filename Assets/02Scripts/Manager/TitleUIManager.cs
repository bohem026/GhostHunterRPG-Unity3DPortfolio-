using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    // --- Constants ---
    private const string NEXT_SCENE = "LobbyScene";
    private const float FIRST_CAMERA = 0.66f;  // (�ɼ�) 1�� ī�޶� ���� ��� �ð�
    private const float REST_CAMERA = 1.33f;   // 2~4�� ī�޶� ���� ����
    private const float POPUP_UI = 3.99f;      // Exit ��ư ��������� ��� �ð�
    private const float DURATION_START = 2f;   // ���� ����(�̵�/ȸ��) �ҿ� �ð�

    // --- Singleton ---
    public static TitleUIManager Inst;

    // --- Serialized UI References ---
    [Space(20)]
    [Header("UI\nFULL SCREEN")]
    [SerializeField] private Image panel_Fade;
    [SerializeField] private Image panel_Flash;
    [SerializeField] private Button button_Start;
    [SerializeField] private Button button_Exit;

    [Header("BODY")]
    [SerializeField] private GameObject Body;
    [SerializeField] private RectTransform rect_P;
    [SerializeField] private RectTransform rect_M;

    [Space(20)]
    [Header("SoundClip")]
    [SerializeField] private AudioClip bgmClip;

    // --- State ---
    private bool isTitleUIReady = false;
    public bool IsTitleUIReady => isTitleUIReady;

    /// <summary>
    /// �̱��� ���۷����� �ʱ�ȭ�Ѵ�.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// ��ư �ڵ鷯�� ���ε��ϰ� �ʱ�ȭ/��Ʈ�� ���� �ڷ�ƾ�� �����Ѵ�.
    /// </summary>
    private void Start()
    {
        button_Start.onClick.AddListener(ButtonStartOnClick);
        button_Exit.onClick.AddListener(ButtonExitOnClick);

        StartCoroutine(Init());
        StartCoroutine(ProductIntro());
    }

    /// <summary>
    /// Exit ��ư ���� Ÿ�̹��� �����ϰ�,
    /// Firestore �ʱ�ȭ�� ������ ����(Body)�� Ȱ��ȭ�Ѵ�.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitForSeconds(POPUP_UI);

        isTitleUIReady = true;
        button_Exit.gameObject.SetActive(true);

        // Firestore �ʱ�ȭ �Ϸ� ��� �� ���� Ȱ��ȭ
        yield return new WaitUntil(() => FBFirestoreManager.Instance.IsInitialized);
        Body.SetActive(true);
    }

    /// <summary>
    /// Ÿ��Ʋ ���� �� ī�޶� �÷��� ������ ��Ʈ�� ������ �����ϰ�
    /// BGM�� ����Ѵ�.
    /// </summary>
    private IEnumerator ProductIntro()
    {
        // --- PRODUCTION: INTRO ---

        // (�ɼ�) 1�� ī�޶�
        // yield return new WaitForSeconds(FIRST_CAMERA);

        // 2�� ī�޶�
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        // 3�� ī�޶�
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        // ���� ī�޶�
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        // BGM ���(������Ʈ Ǯ �Ŵ��� �غ� ���)
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        AudioPlayerPoolManager.Instance.PlayBGMClipLoop(bgmClip, AudioPlayerPoolManager.BGM_VOLUME);
    }

    /// <summary>
    /// Start ��ư Ŭ�� ��: ��ư ���, Ŭ�� SFX ���,
    /// ���� ��ҵ��� ���� ���� �ڷ�ƾ�� �����Ѵ�.
    /// </summary>
    private void ButtonStartOnClick()
    {
        if (!Body.activeSelf) return;

        // �ߺ� �Է� ����
        button_Start.interactable = false;
        button_Exit.interactable = false;
        if (button_Exit.gameObject.activeSelf)
            button_Exit.gameObject.SetActive(false);

        // Ŭ�� SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        // --- PRODUCTION ---
        // ��Ʈ�� �ִϸ����� ���� �� ���� ����� ��ȯ
        Body.GetComponentInChildren<Animator>().enabled = false;

        // ���� ��� �̵�/ȸ�� ����
        StartCoroutine(ProductStart(Body.transform.GetChild(0).GetComponent<RectTransform>(), 1000f));
        StartCoroutine(ProductStart(Body.transform.GetChild(1).GetComponent<RectTransform>(), -1000f, 30f, false));
        StartCoroutine(ProductStart(rect_P, -3000f, 90f, false));
        StartCoroutine(ProductStart(rect_M, -2500f, 75f, true));
    }

    /// <summary>
    /// Exit ��ư Ŭ�� ��: �غ� �Ϸ� ���¿����� ����.
    /// ������/��Ÿ�ӿ� ���� �����Ѵ�.
    /// </summary>
    private void ButtonExitOnClick()
    {
        if (!isTitleUIReady) return;

        // Ŭ�� SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// ������ RectTransform�� DURATION_START ���� �̵�/ȸ�� �����ϰ�,
    /// ���� �� ��� �ڷ�ƾ�� �ߴ��ϰ� ���� ������ ��ȯ�Ѵ�.
    /// </summary>
    private IEnumerator ProductStart(
        RectTransform target,
        float moveOffset = 0f,
        float rotateOffset = 0f,
        bool clockwise = true)
    {
        // ����/���� ���� ���
        Vector3 startPos = target.localPosition;
        Quaternion startRot = target.localRotation;

        Vector3 endPos = startPos + new Vector3(0f, moveOffset);
        Quaternion endRot = Quaternion.Euler(0, 0, rotateOffset * (clockwise ? -1f : 1f)) * startRot;

        // �̵�/ȸ�� ����
        float elapsed = 0f;
        while (elapsed < DURATION_START)
        {
            float t = elapsed / DURATION_START;

            // ��ġ/ȸ�� ���� ����
            target.localPosition = Vector3.Lerp(startPos, endPos, t);
            target.localRotation = Quaternion.Lerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ������ ����
        target.localPosition = endPos;
        target.localRotation = endRot;

        // --- NEXT SCENE ---
        StopAllCoroutines(); // ���� ���� ����(���� ���� ���� ���⿡�� �� ��ȯ)
        if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.Stop();
        SceneManager.LoadScene(NEXT_SCENE);
    }
}
