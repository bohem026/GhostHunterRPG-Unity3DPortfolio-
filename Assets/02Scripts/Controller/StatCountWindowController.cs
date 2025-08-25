using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스탯 일괄 강화 팝업:
/// - 선택된 스탯/레벨/재화 기준으로 일괄 강화 수량 계산
/// - 슬라이더로 강화 횟수 선택, 확인 시 레벨 상승/재화 차감/관련 UI 갱신
/// </summary>
public class StatCountWindowController : MonoBehaviour
{
    [Header("HEADER")]
    [SerializeField] private Button buttonArea_Close;
    [SerializeField] private Button button_Close;

    [Header("BODY")]
    [SerializeField] private Image image_Icon;
    [SerializeField] private Text text_Name;
    [SerializeField] private TextMeshProUGUI text_Level;
    [SerializeField] private Slider slider_Count;

    [Header("FOOT")]
    [SerializeField] private Button button_Confirm;

    private StatSO selectedAsset;
    private StatController.StatType selectedType;
    private int selectedLevel;      // 현재 레벨(강화 전 기준)
    private int selectedCount = 1;  // 일괄 강화 횟수

    private void OnEnable()
    {
        Init();
        InitBody();
        InitFoot();
    }

    private void Start()
    {
        InitListener();
    }

    /// <summary>
    /// 버튼/슬라이더 리스너 등록.
    /// </summary>
    private void InitListener()
    {
        // HEADER
        buttonArea_Close.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });
        button_Close.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        // BODY
        slider_Count.onValueChanged.AddListener(CountSliderValueOnChange);

        // FOOT
        button_Confirm.onClick.AddListener(ConfirmButtonOnClick);
    }

    /// <summary>
    /// 현재 선택된 스탯/레벨/초기 강화 횟수 설정.
    /// </summary>
    private void Init()
    {
        selectedAsset = StatDetailWindowController.Inst.SelectedAsset;
        selectedType = selectedAsset.TYPE;
        selectedLevel = GlobalValue.Instance.GetStatLevelByType(selectedType);
        selectedCount = 1;
    }

    /// <summary>
    /// 본문 UI(아이콘/이름/레벨/슬라이더) 초기화.
    /// </summary>
    private void InitBody()
    {
        image_Icon.sprite = selectedAsset.ICON;
        text_Name.text = selectedAsset.NAME;
        text_Level.text = $"<color=#00ff00><size=125%>{(selectedLevel + selectedCount):00}</size></color> /{selectedAsset.MAXLV}";

        InitCountSlider();
    }

    /// <summary>
    /// 하단 버튼 초기 텍스트 설정.
    /// </summary>
    private void InitFoot()
    {
        button_Confirm.GetComponentInChildren<Text>().text = $"{selectedCount}회 강화";
    }

    /// <summary>
    /// 보유 GEM/최대 레벨 기준으로 슬라이더 범위/값 초기화.
    /// </summary>
    private void InitCountSlider()
    {
        int minLevel = selectedLevel + selectedCount;
        int possibleCount = Mathf.FloorToInt(GlobalValue.Instance._Inven.STGEM_CNT / selectedAsset.COST);
        int maxLevel = Mathf.Clamp(selectedLevel + possibleCount, minLevel, selectedAsset.MAXLV);

        slider_Count.minValue = minLevel;
        slider_Count.maxValue = maxLevel;
        slider_Count.value = slider_Count.minValue;
    }

    /// <summary>
    /// 확인 버튼: 일괄 강화 진행(레벨 증가, GEM 차감, 관련 UI 갱신) 후 창 닫기.
    /// </summary>
    private void ConfirmButtonOnClick()
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);

        // 1) 레벨 증가
        GlobalValue.Instance.ElapseStatLevelByType(selectedType, selectedAsset.MAXLV, selectedCount);

        // 2) GEM 차감 저장
        GlobalValue.Instance._Inven.STGEM_CNT -= selectedAsset.COST * selectedCount;
        GlobalValue.Instance.SaveInven();

        // 3) 관련 UI 갱신
        StatDetailWindowController.Inst.UpdateDetailUIByType(selectedType);
        StatWindowController.Inst.UpdateContentUIByType(selectedType, selectedAsset.MAXLV);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 슬라이더 값 변경 시 강화 횟수/표시 텍스트 갱신.
    /// </summary>
    private void CountSliderValueOnChange(float _)
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Slider);

        selectedCount = (int)slider_Count.value - selectedLevel;

        text_Level.text = $"<color=#00ff00><size=125%>{(selectedLevel + selectedCount):00}</size></color> /{selectedAsset.MAXLV}";
        button_Confirm.GetComponentInChildren<Text>().text = $"{selectedCount}회 강화";
    }
}
