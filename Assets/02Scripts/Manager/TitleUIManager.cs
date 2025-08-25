using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    // --- Constants ---
    private const string NEXT_SCENE = "LobbyScene";
    private const float FIRST_CAMERA = 0.66f;  // (옵션) 1차 카메라 연출 대기 시간
    private const float REST_CAMERA = 1.33f;   // 2~4차 카메라 연출 간격
    private const float POPUP_UI = 3.99f;      // Exit 버튼 노출까지의 대기 시간
    private const float DURATION_START = 2f;   // 시작 연출(이동/회전) 소요 시간

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
    /// 싱글톤 레퍼런스를 초기화한다.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 버튼 핸들러를 바인딩하고 초기화/인트로 연출 코루틴을 시작한다.
    /// </summary>
    private void Start()
    {
        button_Start.onClick.AddListener(ButtonStartOnClick);
        button_Exit.onClick.AddListener(ButtonExitOnClick);

        StartCoroutine(Init());
        StartCoroutine(ProductIntro());
    }

    /// <summary>
    /// Exit 버튼 노출 타이밍을 제어하고,
    /// Firestore 초기화가 끝나면 본문(Body)을 활성화한다.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitForSeconds(POPUP_UI);

        isTitleUIReady = true;
        button_Exit.gameObject.SetActive(true);

        // Firestore 초기화 완료 대기 후 본문 활성화
        yield return new WaitUntil(() => FBFirestoreManager.Instance.IsInitialized);
        Body.SetActive(true);
    }

    /// <summary>
    /// 타이틀 진입 시 카메라 플래시 느낌의 인트로 연출을 수행하고
    /// BGM을 재생한다.
    /// </summary>
    private IEnumerator ProductIntro()
    {
        // --- PRODUCTION: INTRO ---

        // (옵션) 1차 카메라
        // yield return new WaitForSeconds(FIRST_CAMERA);

        // 2차 카메라
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        // 3차 카메라
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        // 메인 카메라
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        // BGM 재생(오브젝트 풀 매니저 준비 대기)
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        AudioPlayerPoolManager.Instance.PlayBGMClipLoop(bgmClip, AudioPlayerPoolManager.BGM_VOLUME);
    }

    /// <summary>
    /// Start 버튼 클릭 시: 버튼 잠금, 클릭 SFX 재생,
    /// 개별 요소들의 시작 연출 코루틴을 실행한다.
    /// </summary>
    private void ButtonStartOnClick()
    {
        if (!Body.activeSelf) return;

        // 중복 입력 방지
        button_Start.interactable = false;
        button_Exit.interactable = false;
        if (button_Exit.gameObject.activeSelf)
            button_Exit.gameObject.SetActive(false);

        // 클릭 SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        // --- PRODUCTION ---
        // 인트로 애니메이터 종료 후 수동 연출로 전환
        Body.GetComponentInChildren<Animator>().enabled = false;

        // 개별 요소 이동/회전 연출
        StartCoroutine(ProductStart(Body.transform.GetChild(0).GetComponent<RectTransform>(), 1000f));
        StartCoroutine(ProductStart(Body.transform.GetChild(1).GetComponent<RectTransform>(), -1000f, 30f, false));
        StartCoroutine(ProductStart(rect_P, -3000f, 90f, false));
        StartCoroutine(ProductStart(rect_M, -2500f, 75f, true));
    }

    /// <summary>
    /// Exit 버튼 클릭 시: 준비 완료 상태에서만 동작.
    /// 에디터/런타임에 맞춰 종료한다.
    /// </summary>
    private void ButtonExitOnClick()
    {
        if (!isTitleUIReady) return;

        // 클릭 SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 지정한 RectTransform을 DURATION_START 동안 이동/회전 보간하고,
    /// 종료 시 모든 코루틴을 중단하고 다음 씬으로 전환한다.
    /// </summary>
    private IEnumerator ProductStart(
        RectTransform target,
        float moveOffset = 0f,
        float rotateOffset = 0f,
        bool clockwise = true)
    {
        // 시작/종료 포즈 계산
        Vector3 startPos = target.localPosition;
        Quaternion startRot = target.localRotation;

        Vector3 endPos = startPos + new Vector3(0f, moveOffset);
        Quaternion endRot = Quaternion.Euler(0, 0, rotateOffset * (clockwise ? -1f : 1f)) * startRot;

        // 이동/회전 보간
        float elapsed = 0f;
        while (elapsed < DURATION_START)
        {
            float t = elapsed / DURATION_START;

            // 위치/회전 선형 보간
            target.localPosition = Vector3.Lerp(startPos, endPos, t);
            target.localRotation = Quaternion.Lerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 최종값 스냅
        target.localPosition = endPos;
        target.localRotation = endRot;

        // --- NEXT SCENE ---
        StopAllCoroutines(); // 현재 설계 유지(가장 먼저 끝난 연출에서 씬 전환)
        if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.Stop();
        SceneManager.LoadScene(NEXT_SCENE);
    }
}
