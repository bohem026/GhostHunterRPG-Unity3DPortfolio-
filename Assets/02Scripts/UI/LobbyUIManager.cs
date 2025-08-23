using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Inst;

    private const string NAME_NEW = "NewIndicator";

    [Space(20)]
    [Header("UI\nBUTTON_BODY")]
    [SerializeField] private Button[] buttons_Level;
    [Header("BUTTON_FOOT")]
    [SerializeField] private Button button_Stat;
    [SerializeField] private Button button_Skill;
    [SerializeField] private Button button_Inven;
    [SerializeField] private Button button_ResetAll;
    [SerializeField] private Button button_Exit;
    GameObject statNewIndicator;
    GameObject skillNewIndicator;
    GameObject invenNewIndicator;
    [Header("WINDOW")]
    [SerializeField] private GameObject window_Stat;
    [SerializeField] private GameObject window_Skill;
    [SerializeField] private GameObject window_Inven;
    [SerializeField] private GameObject[] windows_Stage;

    [Space(20)]
    [Header("SoundClip")]
    [SerializeField] private AudioClip bgmClip;

    bool isInitialized = false;

    private void Awake()
    {
        if (!Inst) Inst = this;
        if (!isInitialized) StartCoroutine(Init());
    }

    private void Start()
    {
        //Enable mouse click.
        ChangeMouseInputMode(1);
        //Play BGM: Lobby.
        if (AudioPlayerPoolManager.Instance)
            AudioPlayerPoolManager.Instance.PlayBGMClipLoop
                (bgmClip, AudioPlayerPoolManager.BGM_VOLUME);
    }

    private void ChangeMouseInputMode(int mode)
    {
        switch (mode)
        {
            case 0:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case 1:
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
                break;
            default:
                break;
        }
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance);
        yield return GlobalValue.Instance.LoadData().AsCoroutine();
        yield return new WaitUntil(() => GlobalValue.Instance.IsDataLoaded);
        Debug.Log("GlobalValue 초기화 완료.");
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        Debug.Log("AudioPlayerPoolManager 초기화 완료.");

        for (int index = 0; index < buttons_Level.Length; index++)
        {
            int captured = index;
            Button buttonSelf;
            Button buttonLock;

            (buttonSelf = buttons_Level[captured]).onClick.AddListener(() =>
            {
                //Play SFX: Click.
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
                (AudioPlayerPoolManager.SFXType.Click);
                //Pop-up window.
                windows_Stage[captured].SetActive(true);
            });
            buttonLock = buttonSelf.GetComponentsInChildren<Button>()
                .FirstOrDefault(b => b != buttonSelf);

            //Unlock stage.
            if (!buttonLock) continue;
            if (captured < GlobalValue.Instance.GetBestStageFromInfo())
                buttonLock.gameObject.SetActive(false);
        }

        statNewIndicator = button_Stat.transform.Find(NAME_NEW).gameObject;
        skillNewIndicator = button_Skill.transform.Find(NAME_NEW).gameObject;
        invenNewIndicator = button_Inven.transform.Find(NAME_NEW).gameObject;

        if (GlobalValue.Instance._Info.STGEM_CLAIMED)
            statNewIndicator.SetActive(true);
        if (GlobalValue.Instance._Info.SKGEM_CLAIMED)
            skillNewIndicator.SetActive(true);
        if (GlobalValue.Instance._Info.GEAR_CLAIMED)
            invenNewIndicator.SetActive(true);

        button_Stat.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Pop-up window.
            window_Stat.SetActive(true);
            GlobalValue.Instance._Info.STGEM_CLAIMED = false;
            GlobalValue.Instance.SaveInfo();
            if (statNewIndicator.activeSelf) statNewIndicator.SetActive(false);
        });
        button_Skill.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Pop-up window.
            window_Skill.SetActive(true);
            GlobalValue.Instance._Info.SKGEM_CLAIMED = false;
            GlobalValue.Instance.SaveInfo();
            if (skillNewIndicator.activeSelf) skillNewIndicator.SetActive(false);
        });
        button_Inven.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Pop-up window.
            window_Inven.SetActive(true);
            GlobalValue.Instance._Info.GEAR_CLAIMED = false;
            GlobalValue.Instance.SaveInfo();
            if (invenNewIndicator.activeSelf) invenNewIndicator.SetActive(false);
        });

        /*
        button_ResetAll.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Reset player info.
            GlobalValue.Instance.ResetInven();
            GlobalValue.Instance.ResetInfo();
            GlobalValue.Instance.ResetStatLevel();
            GlobalValue.Instance.ResetSkillLevel();
            //Reload scene.
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        });
        */

        button_Exit.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            //Reset player info.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        isInitialized = true;
    }
}
