using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 입장 팝업의 UI 이벤트를 처리한다.
/// - 닫기(배경/버튼), 입장 버튼 클릭 시 동작
/// - 입장 시 BGM 정지 및 로딩 씬을 통해 스테이지 씬 로드
/// </summary>
public class StageWindowController : MonoBehaviour
{
    private const string NAME_OF_BASE_SCENE = "LevelBaseScene";

    [Space(20)]
    [Header("INFO")]
    [SerializeField] private string NAME_OF_SCENE;

    [Space(20)]
    [Header("UI\nHEADER")]
    [SerializeField] private Button buttonArea_Close;
    [SerializeField] private Button button_Close;

    [Header("FOOT")]
    [SerializeField] private Button button_Enter;

    private bool isInitialized;

    /// <summary>
    /// 버튼 리스너를 등록하고 1회만 초기화한다.
    /// </summary>
    private void Start()
    {
        if (isInitialized) return;

        // ── HEADER
        buttonArea_Close.onClick.AddListener(() =>
        {
            // SFX: Click
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);
            // Close
            gameObject.SetActive(false);
        });

        button_Close.onClick.AddListener(() =>
        {
            // SFX: Click
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);
            // Close
            gameObject.SetActive(false);
        });

        // ── FOOT
        button_Enter.onClick.AddListener(() =>
        {
            // SFX: Click
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);

            // Turn off lobby BGM (있다면)
            if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
                AudioPlayerPoolManager.Instance.BGMSource.Stop();

            // Load next scene via loading scene
            LoadingSceneManager.LoadSceneWithLoading(
                NAME_OF_SCENE,
                NAME_OF_BASE_SCENE);
        });

        isInitialized = true;
    }
}
