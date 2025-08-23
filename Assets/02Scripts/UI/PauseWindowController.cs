using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseWindowController : MonoBehaviour
{
    private const string NAME_OF_BASE_SCENE = "LevelBaseScene";

    //[Header("INFO")]
    //[SerializeField] private string NAME_OF_SCENE;
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

    bool doNotCallOnDisable = false;
    bool isInitialized = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (!isInitialized) Init();
    }

    void Update()
    {
        text_Wave.text = $"{StageManager.Inst.CurrentWave} / {StageManager.MAX_WAVE}";

        int totalSeconds = Mathf.FloorToInt(StageManager.Inst.StagePlayTime);
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        text_Time.text = string.Format("{0:00} : {1:00}", minutes, seconds);

        text_Rest.text = $"{StageManager.Inst.GetRestMonCount()}";
    }

    private void Init()
    {
        AddButtonListeners();

        isInitialized = true;
    }

    private void AddButtonListeners()
    {
        //FULL
        buttonArea_Close.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            gameObject.SetActive(false);
        });
        //FOOT
        button_Resume.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            gameObject.SetActive(false);
        });
        button_Retry.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            Scene currentScene = SceneManager.GetActiveScene();
            LoadingSceneManager.LoadSceneWithLoading
                (currentScene.name,
                NAME_OF_BASE_SCENE);
        });
        button_Quit.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            StartCoroutine(StageManager.Inst.GameOver(false));
        });
    }

    void OnEnable()
    {
        StageUIManager.Inst.IsPauseWindowOnDisplay = true;

        //Mute BGMTimeTick.
        if (AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.volume = 0f;
        //Tune BGM volume down.
        if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.volume *= 0.33f;
        //1. Unlock mouse pointer.
        GameManager.Inst.ChangeMouseInputMode(1);
        //2. Set time scale to 0.
        Time.timeScale = 0.0f;
    }

    void OnDisable()
    {
        if (doNotCallOnDisable) return;

        //Resume BGMTimeTick.
        if (AudioPlayerPoolManager.Instance.BGMTimeTickSource &&
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMTimeTickSource.volume
                = AudioPlayerPoolManager.SFX_VOLUME;
        //Tune BGM volume up.
        if (AudioPlayerPoolManager.Instance.BGMSource &&
            AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.volume
                = AudioPlayerPoolManager.BGM_VOLUME;
        //1. Lock mouse pointer.
        GameManager.Inst.ChangeMouseInputMode(0);
        //2. Reset time scale.
        Time.timeScale = 1.0f;

        StageUIManager.Inst.IsPauseWindowOnDisplay = false;
    }
}
