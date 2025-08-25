using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatWindowController : MonoBehaviour
{
    public static StatWindowController Inst;

    [Header("HEADER")]
    [SerializeField] private Button button_Back;
    [SerializeField] private Text text_Gem;

    [Header("BODY")]
    public Button button_HP;
    public Button button_MPITV;
    public Button button_DEF;
    public Button button_MATK;
    public Button button_SATK;
    public Button button_CTKR;
    public Button button_EVDR;
    public Button button_STBR;

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Detail;
    [SerializeField] private GameObject window_Message;

    // 활성화 시 전역 데이터 로딩 후 헤더/바디 초기화
    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// GlobalValue 로딩 대기 후 UI 초기화.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);
        Inst = this;

        InitHeader();
        InitBody();
    }

    /// <summary>
    /// 헤더: 뒤로가기 버튼/재화 텍스트 초기화.
    /// </summary>
    private void InitHeader()
    {
        button_Back.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        text_Gem.text = GlobalValue.Instance._Inven.STGEM_CNT.ToString();
    }

    /// <summary>
    /// 바디: 레벨 표기 및 버튼 리스너 연결.
    /// </summary>
    private void InitBody()
    {
        InitAllContentUI();
        AddStatButtonListener();
    }

    /// <summary>
    /// 모든 스탯 버튼의 현재 레벨 표기 초기화.
    /// </summary>
    private void InitAllContentUI()
    {
        TextMeshProUGUI levelText;
        int level;

        levelText = button_HP.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.HP_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /99";

        levelText = button_MPITV.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.MPITV_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /99";

        levelText = button_DEF.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.DEF_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /99";

        levelText = button_MATK.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.MATK_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /50";

        levelText = button_SATK.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.SATK_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /50";

        levelText = button_CTKR.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.CTKR_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /50";

        levelText = button_EVDR.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.EVDR_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /99";

        levelText = button_STBR.GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance._StatLevel.STBR_LV;
        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /99";
    }

    /// <summary>
    /// 스탯 버튼 클릭 리스너 연결.
    /// </summary>
    private void AddStatButtonListener()
    {
        button_HP.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.HP);
        });
        button_MPITV.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.MPITV);
        });
        button_DEF.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.DEF);
        });
        button_MATK.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.MATK);
        });
        button_SATK.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.SATK);
        });
        button_CTKR.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.CTKR);
        });
        button_EVDR.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.EVDR);
        });
        button_STBR.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            StatButtonOnClick(StatController.StatType.STBR);
        });
    }

    // 비활성화 시 메시지 창 강제 종료
    private void OnDisable()
    {
        StopCoroutine("DisplayMessageWindow");
        window_Message.SetActive(false);
    }

    /// <summary>
    /// 스탯 버튼 클릭 시 상세 창 열고 해당 스탯 상세 갱신.
    /// </summary>
    public void StatButtonOnClick(StatController.StatType type)
    {
        if (IsMessageWindowOnDisplay) return;

        window_Detail.GetComponent<StatDetailWindowController>().UpdateDetailUIByType(type);
        if (!window_Detail.activeSelf) window_Detail.SetActive(true);
    }

    /// <summary>
    /// 특정 스탯의 레벨/재화 UI 갱신.
    /// </summary>
    public void UpdateContentUIByType(StatController.StatType type, int maxLevel)
    {
        TextMeshProUGUI levelText;
        int level;

        switch (type)
        {
            case StatController.StatType.HP:
                levelText = button_HP.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.HP_LV;
                break;
            case StatController.StatType.MPITV:
                levelText = button_MPITV.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.MPITV_LV;
                break;
            case StatController.StatType.DEF:
                levelText = button_DEF.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.DEF_LV;
                break;
            case StatController.StatType.MATK:
                levelText = button_MATK.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.MATK_LV;
                break;
            case StatController.StatType.SATK:
                levelText = button_SATK.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.SATK_LV;
                break;
            case StatController.StatType.CTKR:
                levelText = button_CTKR.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.CTKR_LV;
                break;
            case StatController.StatType.EVDR:
                levelText = button_EVDR.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.EVDR_LV;
                break;
            case StatController.StatType.STBR:
                levelText = button_STBR.GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance._StatLevel.STBR_LV;
                break;
            default:
                Debug.Log("[!!ERROR!!] UNDEFINED STAT TYPE");
                return;
        }

        levelText.text = $"<color=#00ff00><size=125%>{level:00}</size></color> /{maxLevel}";
        text_Gem.text = GlobalValue.Instance._Inven.STGEM_CNT.ToString();
    }

    /// <summary>
    /// 지정 시간 동안 메시지 창 표시.
    /// </summary>
    public IEnumerator DisplayMessageWindow(string message, float duration)
    {
        window_Message.GetComponentInChildren<Text>().text = message;
        window_Message.SetActive(true);
        yield return new WaitForSecondsRealtime(duration);
        window_Message.SetActive(false);
    }

    // GET
    public bool IsMessageWindowOnDisplay => window_Message.activeSelf;
}
