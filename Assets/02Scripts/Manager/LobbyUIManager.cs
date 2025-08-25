using System.Collections;
using System.Linq;
using UnityEngine;
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

    private GameObject statNewIndicator;
    private GameObject skillNewIndicator;
    private GameObject invenNewIndicator;

    [Header("WINDOW")]
    [SerializeField] private GameObject window_Stat;
    [SerializeField] private GameObject window_Skill;
    [SerializeField] private GameObject window_Inven;
    [SerializeField] private GameObject[] windows_Stage;

    [Space(20)]
    [Header("SoundClip")]
    [SerializeField] private AudioClip bgmClip;

    private bool isInitialized = false;

    /// <summary>
    /// 싱글톤 설정 및 초기화 코루틴 시작.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
        if (!isInitialized) StartCoroutine(Init());
    }

    /// <summary>
    /// 마우스 입력 모드 활성(클릭 가능) 및 로비 BGM 재생.
    /// </summary>
    private void Start()
    {
        // Enable mouse click.
        ChangeMouseInputMode(1);

        // Play BGM: Lobby.
        if (AudioPlayerPoolManager.Instance)
            AudioPlayerPoolManager.Instance.PlayBGMClipLoop(
                bgmClip, AudioPlayerPoolManager.BGM_VOLUME);
    }

    /// <summary>
    /// 0: 커서 잠금/숨김, 1: 창 내부 제한/표시.
    /// </summary>
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

    /// <summary>
    /// GlobalValue/Audio 매니저 준비 대기 → 스테이지 버튼 바인딩 및 잠금 해제 →
    /// 하단 버튼(New 인디케이터) 설정 → 종료 버튼 바인딩.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance);
        yield return GlobalValue.Instance.LoadData().AsCoroutine();
        yield return new WaitUntil(() => GlobalValue.Instance.IsDataLoaded);
        Debug.Log("GlobalValue 초기화 완료.");
        yield return new WaitUntil(() => AudioPlayerPoolManager.Instance);
        Debug.Log("AudioPlayerPoolManager 초기화 완료.");

        // 스테이지 버튼: 클릭 시 해당 창 팝업, 잠금 버튼은 진행도에 따라 해제
        for (int index = 0; index < buttons_Level.Length; index++)
        {
            int captured = index;
            Button buttonSelf;
            Button buttonLock;

            (buttonSelf = buttons_Level[captured]).onClick.AddListener(() =>
            {
                // Play SFX: Click.
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                    AudioPlayerPoolManager.SFXType.Click);
                // Pop-up window.
                windows_Stage[captured].SetActive(true);
            });

            // 자식 중 잠금 버튼 탐색(자기 자신 제외)
            buttonLock = buttonSelf.GetComponentsInChildren<Button>()
                .FirstOrDefault(b => b != buttonSelf);

            // 진행도 기준으로 스테이지 잠금 해제
            if (!buttonLock) continue;
            if (captured < GlobalValue.Instance.GetBestStageFromInfo())
                buttonLock.gameObject.SetActive(false);
        }

        // 하단 버튼의 New 인디케이터 캐싱
        statNewIndicator = button_Stat.transform.Find(NAME_NEW).gameObject;
        skillNewIndicator = button_Skill.transform.Find(NAME_NEW).gameObject;
        invenNewIndicator = button_Inven.transform.Find(NAME_NEW).gameObject;

        // New 표시 초기 상태 반영
        if (GlobalValue.Instance._Info.STGEM_CLAIMED)
            statNewIndicator.SetActive(true);
        if (GlobalValue.Instance._Info.SKGEM_CLAIMED)
            skillNewIndicator.SetActive(true);
        if (GlobalValue.Instance._Info.GEAR_CLAIMED)
            invenNewIndicator.SetActive(true);

        // 스탯 창
        button_Stat.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);
            window_Stat.SetActive(true);
            GlobalValue.Instance._Info.STGEM_CLAIMED = false;
            GlobalValue.Instance.SaveInfo();
            if (statNewIndicator.activeSelf) statNewIndicator.SetActive(false);
        });

        // 스킬 창
        button_Skill.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);
            window_Skill.SetActive(true);
            GlobalValue.Instance._Info.SKGEM_CLAIMED = false;
            GlobalValue.Instance.SaveInfo();
            if (skillNewIndicator.activeSelf) skillNewIndicator.SetActive(false);
        });

        // 인벤 창
        button_Inven.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);
            window_Inven.SetActive(true);
            GlobalValue.Instance._Info.GEAR_CLAIMED = false;
            GlobalValue.Instance.SaveInfo();
            if (invenNewIndicator.activeSelf) invenNewIndicator.SetActive(false);
        });

        // 종료 버튼
        button_Exit.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(
                AudioPlayerPoolManager.SFXType.Click);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });

        isInitialized = true;
    }
}
