using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// �������� ���� ���â(Ŭ����/�й�) UI�� �����մϴ�.
/// ��õ�/�κ� �̵� ��ư ó���� ��� �̹���, ȿ������ �����մϴ�.
/// </summary>
public class ResultWindowController : MonoBehaviour
{
    private const string NAME_OF_BASE_SCENE = "LevelBaseScene";
    private const string NAME_OF_LOBBY_SCENE = "LobbyScene";

    [Space(20)]
    [Header("IMAGE")]
    [SerializeField] private Image image_Result;
    [Space(10)]
    [SerializeField] private Sprite sprite_Clear;
    [SerializeField] private Sprite sprite_Lose;

    [Space(20)]
    [Header("BUTTON")]
    [SerializeField] private Button button_Retry;
    [SerializeField] private Button button_Lobby;

    private void OnEnable()
    {
        // ���â�� ǥ�õ� �� ��ư ������ ���ε�
        button_Retry.onClick.AddListener(() =>
        {
            // SFX + Ÿ�ӽ����� ���� �� ���� �� �����(���̽� �� �Բ� �ε�)
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            Time.timeScale = 1.0f;
            Scene currentScene = SceneManager.GetActiveScene();
            LoadingSceneManager.LoadSceneWithLoading(currentScene.name, NAME_OF_BASE_SCENE);
        });

        button_Lobby.onClick.AddListener(() =>
        {
            // SFX + Ÿ�ӽ����� ���� �� �κ�� �̵�
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            Time.timeScale = 1.0f;
            LoadingSceneManager.LoadSceneWithLoading(NAME_OF_LOBBY_SCENE);
        });
    }

    /// <summary>
    /// ������� ���� ��� �̹����� �����ϰ� ���� SFX�� ����մϴ�.
    /// </summary>
    /// <param name="result">true=Ŭ����, false=�й�</param>
    public void InitUIs(bool result)
    {
        if (result)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Clear);
            image_Result.sprite = sprite_Clear;
        }
        else
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Lose);
            image_Result.sprite = sprite_Lose;
        }
    }
}
