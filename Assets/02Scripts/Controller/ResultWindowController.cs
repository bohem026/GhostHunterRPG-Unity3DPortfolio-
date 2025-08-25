using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스테이지 종료 결과창(클리어/패배) UI를 관리합니다.
/// 재시도/로비 이동 버튼 처리와 결과 이미지, 효과음을 제어합니다.
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
        // 결과창이 표시될 때 버튼 리스너 바인딩
        button_Retry.onClick.AddListener(() =>
        {
            // SFX + 타임스케일 복구 후 현재 씬 재시작(베이스 씬 함께 로드)
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            Time.timeScale = 1.0f;
            Scene currentScene = SceneManager.GetActiveScene();
            LoadingSceneManager.LoadSceneWithLoading(currentScene.name, NAME_OF_BASE_SCENE);
        });

        button_Lobby.onClick.AddListener(() =>
        {
            // SFX + 타임스케일 복구 후 로비로 이동
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            Time.timeScale = 1.0f;
            LoadingSceneManager.LoadSceneWithLoading(NAME_OF_LOBBY_SCENE);
        });
    }

    /// <summary>
    /// 결과값에 따라 결과 이미지를 설정하고 대응 SFX를 재생합니다.
    /// </summary>
    /// <param name="result">true=클리어, false=패배</param>
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
