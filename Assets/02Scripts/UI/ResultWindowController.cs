using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    bool result;

    void OnEnable()
    {
        button_Retry.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            Time.timeScale = 1.0f;
            Scene currentScene = SceneManager.GetActiveScene();
            LoadingSceneManager.LoadSceneWithLoading
                (currentScene.name,
                NAME_OF_BASE_SCENE);
        });
        button_Lobby.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            Time.timeScale = 1.0f;
            LoadingSceneManager.LoadSceneWithLoading(NAME_OF_LOBBY_SCENE);
        });
    }

    public void InitUIs(bool result)
    {
        if (result)
        {
            //Play SFX: Clear.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Clear);
            image_Result.sprite = sprite_Clear;
        }
        else
        {
            //Play SFX: Lose.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Lose);
            image_Result.sprite = sprite_Lose;
        }
    }
}
