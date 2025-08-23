using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

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

    [Space(20)]
    [Header("SoundClip")]
    [SerializeField] private AudioClip sfxClip_Click;

    bool isInitialized;

    // Start is called before the first frame update
    void Start()
    {
        if (isInitialized) return;

        //--- HEADER
        buttonArea_Close.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Close.
            gameObject.SetActive(false);
        });

        button_Close.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Close.
            gameObject.SetActive(false);
        });
        //---

        //--- FOOT
        button_Enter.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Turn off BGM.
            if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
                AudioPlayerPoolManager.Instance.BGMSource.Stop();
            //Load next scene.
            LoadingSceneManager.LoadSceneWithLoading
            (NAME_OF_SCENE,
            NAME_OF_BASE_SCENE);
        });
        //---

        isInitialized = true;
    }
}
