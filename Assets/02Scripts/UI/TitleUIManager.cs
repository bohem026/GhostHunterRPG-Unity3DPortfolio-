using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUIManager : MonoBehaviour
{
    private const string NEXT_SCENE = "LobbyScene";
    private const float FIRST_CAMERA = 0.66f;
    private const float REST_CAMERA = 1.33f;
    private const float POPUP_UI = 3.99f;
    private const float DURATION_START = 2f;

    public static TitleUIManager Inst;

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

    bool isTitleUIReady = false;
    public bool IsTitleUIReady => isTitleUIReady;

    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    private void Start()
    {
        button_Start.onClick.AddListener(ButtonStartOnClick);
        button_Exit.onClick.AddListener(ButtonExitOnClick);

        StartCoroutine(Init());
        StartCoroutine(ProductIntro());
    }

    IEnumerator Init()
    {
        yield return new WaitForSeconds(POPUP_UI);
        isTitleUIReady = true;
        button_Exit.gameObject.SetActive(true);

        yield return new WaitUntil(() => FBFirestoreManager.Instance.IsInitialized);
        Body.SetActive(true);
    }

    IEnumerator ProductIntro()
    {
        //--- PRODUCTION: INTRO
        //First Camera.
        //yield return new WaitForSeconds(FIRST_CAMERA);

        //Second Camera.
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        //Third Camera.
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        //Main Camera.
        if (panel_Flash.gameObject.activeSelf)
            panel_Flash.gameObject.SetActive(false);
        panel_Flash.gameObject.SetActive(true);
        yield return new WaitForSeconds(REST_CAMERA);

        //Play BGM: Title.
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        AudioPlayerPoolManager.Instance.PlayBGMClipLoop(bgmClip, AudioPlayerPoolManager.BGM_VOLUME);
        //---
    }

    private void ButtonStartOnClick()
    {
        if (!Body.activeSelf) return;

        button_Start.interactable = false;
        button_Exit.interactable = false;
        if (button_Exit.gameObject.activeSelf)
            button_Exit.gameObject.SetActive(false);

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        //--- PRODUCTION
        Body.GetComponentInChildren<Animator>().enabled = false;
        StartCoroutine(ProductStart
            (Body.transform.GetChild(0).GetComponent<RectTransform>(),
            1000f));
        StartCoroutine(ProductStart
            (Body.transform.GetChild(1).GetComponent<RectTransform>(),
            -1000f,
            30f,
            false));
        StartCoroutine(ProductStart
            (rect_P,
            -3000f,
            90f,
            false));
        StartCoroutine(ProductStart
            (rect_M,
            -2500f,
            75f,
            true));
        //---
    }

    private void ButtonExitOnClick()
    {
        if (!isTitleUIReady) return;

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);
        //Reset player info.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator ProductStart
        (
        RectTransform target,
        float moveOffset = 0f,
        float rotateOffset = 0f,
        bool clockwise = true
        )
    {
        Vector3 startPos = target.localPosition;
        Quaternion startRot = target.localRotation;

        Vector3 endPos = startPos + new Vector3(0f, moveOffset);
        Quaternion endRot = Quaternion.Euler
                            (0,
                            0,
                            rotateOffset * (clockwise ? -1f : 1f)) * startRot;

        float elapsed = 0f;
        while (elapsed < DURATION_START)
        {
            float t = elapsed / DURATION_START;

            //Move and rotate interpol.
            target.localPosition = Vector3.Lerp(startPos, endPos, t);
            target.localRotation = Quaternion.Lerp(startRot, endRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        //Finalize.
        target.localPosition = endPos;
        target.localRotation = endRot;

        //--- NEXT SCENE
        StopAllCoroutines();
        //Turn off BGM.
        if (AudioPlayerPoolManager.Instance.BGMSource.isPlaying)
            AudioPlayerPoolManager.Instance.BGMSource.Stop();
        //Load next scene.
        SceneManager.LoadScene(NEXT_SCENE);
        //---
    }
}
