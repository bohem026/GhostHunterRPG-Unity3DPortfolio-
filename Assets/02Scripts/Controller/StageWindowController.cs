using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �������� ���� �˾��� UI �̺�Ʈ�� ó���Ѵ�.
/// - �ݱ�(���/��ư), ���� ��ư Ŭ�� �� ����
/// - ���� �� BGM ���� �� �ε� ���� ���� �������� �� �ε�
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
    /// ��ư �����ʸ� ����ϰ� 1ȸ�� �ʱ�ȭ�Ѵ�.
    /// </summary>
    private void Start()
    {
        if (isInitialized) return;

        // ���� HEADER
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

        // ���� FOOT
        button_Enter.onClick.AddListener(() =>
        {
            // SFX: Click
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);

            // Turn off lobby BGM (�ִٸ�)
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
