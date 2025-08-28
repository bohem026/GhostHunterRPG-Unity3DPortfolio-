using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스탯 상세/강화 창:
/// - 선택된 스탯의 아이콘/이름/설명/수치/비용 표시
/// - 단일 강화, 초기화(레벨/재화 롤백), 다중 강화 창 호출
/// - 버튼 활성 조건(최대 레벨/재화 부족) 반영
/// </summary>
public class StatDetailWindowController : MonoBehaviour
{
    private const string STAT_SO_PRELINK = "ScriptableObjects/Stat/";
    private const string STAT_SO_POSTLINK = "Asset";
    private const string POSTNOTICE_RESET = "의 강화 레벨을 초기화했습니다.";
    private const string ALERT_MAX_LEVEL = "이미 최고 레벨입니다.";
    private const string ALERT_GEM = "재화가 부족합니다.";

    public static StatDetailWindowController Inst;

    [Header("BODY")]
    [SerializeField] private Image Image_Icon;
    [SerializeField] private Text Text_Name;
    [SerializeField] private Text Text_Intro;
    [SerializeField] private TextMeshProUGUI Text_Detail;
    [SerializeField] private Text Text_Cost;

    [Header("FOOT")]
    [SerializeField] private Button button_Confirm;
    [SerializeField] private Button button_Reset;

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Count;

    private StatController.StatType selectedType;
    private StatSO selectedAsset;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// 전역 데이터 로딩 대기 및 푸터 버튼 리스너 초기화.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);
        Inst = this;
        InitFoot();
    }

    /// <summary>
    /// 하단 버튼 리스너 연결.
    /// </summary>
    private void InitFoot()
    {
        button_Confirm.onClick.AddListener(ConfirmButtonOnClick);
        button_Reset.onClick.AddListener(ResetButtonOnClick);
    }

    /// <summary>
    /// 확인 버튼 색/상태(시각) 초기화. (최대 레벨/재화 부족 시 비활성 색)
    /// </summary>
    private void InitConfirmButton()
    {
        if ((GlobalValue.Instance.GetStatLevelByType(selectedType) == selectedAsset.MAXLV) ||
            (selectedAsset.COST > GlobalValue.Instance._Inven.STGEM_CNT))
        {
            ChangeButtonToDisabled(button_Confirm, true);
        }
        else
        {
            ChangeButtonToDisabled(button_Confirm, false);
        }
    }

    /// <summary>
    /// 초기화 버튼 활성 조건(레벨 보유 여부) 반영.
    /// </summary>
    private void InitResetButton()
    {
        button_Reset.interactable = GlobalValue.Instance.GetStatLevelByType(selectedType) > 0;
    }

    /// <summary>
    /// 타입에 해당하는 스탯 상세 UI를 갱신.
    /// </summary>
    public void UpdateDetailUIByType(StatController.StatType type)
    {
        InitSelected(type);
        if (!selectedAsset)
        {
            Debug.Log("[!!ERROR!!] FAILED TO LOAD ASSET");
            return;
        }

        Image_Icon.sprite = selectedAsset.ICON;
        Text_Name.text = $"[Lv.{GlobalValue.Instance.GetStatLevelByType(type):00}] {selectedAsset.NAME}";
        Text_Intro.text = selectedAsset.INTRO;
        Text_Detail.text = GetDetailText(selectedAsset);
        Text_Cost.text = selectedAsset.COST.ToString();

        InitConfirmButton();
        InitResetButton();
    }

    /// <summary>
    /// 선택 대상(타입/에셋) 갱신.
    /// </summary>
    private void InitSelected(StatController.StatType type)
    {
        selectedType = type;
        selectedAsset = ResourceUtility.GetResourceByType<StatSO>(GetAssetPath(selectedType));
    }

    /// <summary>
    /// 초기화: 현재 레벨만큼 GEM 환급 후 레벨 0으로 롤백, 메시지 및 UI 갱신.
    /// </summary>
    private void ResetButtonOnClick()
    {
        if (StatWindowController.Inst.IsMessageWindowOnDisplay) return;

        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        int currentLevel = GlobalValue.Instance.GetStatLevelByType(selectedType);

        GlobalValue.Instance._Inven.STGEM_CNT += selectedAsset.COST * currentLevel;
        GlobalValue.Instance.ElapseStatLevelByType(selectedType, selectedAsset.MAXLV, -currentLevel);

        StartCoroutine(StatWindowController.Inst.DisplayMessageWindow($"[{selectedAsset.NAME}]{POSTNOTICE_RESET}", 1.5f));

        UpdateDetailUIByType(selectedType);
        StatWindowController.Inst.UpdateContentUIByType(selectedType, selectedAsset.MAXLV);
    }

    /// <summary>
    /// 단일 강화 또는 다중 강화 창 호출(여러 번 가능 시): 레벨업/재화 차감/저장/UI 갱신.
    /// </summary>
    private void ConfirmButtonOnClick()
    {
        if (StatWindowController.Inst.IsMessageWindowOnDisplay) return;

        // 최대 레벨
        if (GlobalValue.Instance.GetStatLevelByType(selectedType) == StatController.MAX_STAT_LEVEL)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(StatWindowController.Inst.DisplayMessageWindow(ALERT_MAX_LEVEL, 1f));
            return;
        }

        // 재화 부족
        if (selectedAsset.COST > GlobalValue.Instance._Inven.STGEM_CNT)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(StatWindowController.Inst.DisplayMessageWindow(ALERT_GEM, 1f));
            return;
        }

        // 가능한 횟수 계산
        int possibleCount = Mathf.FloorToInt(GlobalValue.Instance._Inven.STGEM_CNT / selectedAsset.COST);
        possibleCount = Mathf.Clamp(
            possibleCount,
            0,
            selectedAsset.MAXLV - GlobalValue.Instance.GetStatLevelByType(selectedType));

        // 2회 이상 가능: 일괄 강화 창 오픈
        if (possibleCount > 1)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            window_Count.gameObject.SetActive(true);
        }
        // 단일 강화 진행
        else
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);

            GlobalValue.Instance.ElapseStatLevelByType(selectedType, selectedAsset.MAXLV, 1);
            GlobalValue.Instance._Inven.STGEM_CNT -= selectedAsset.COST;
            GlobalValue.Instance.SaveInven();

            UpdateDetailUIByType(selectedType);
            StatWindowController.Inst.UpdateContentUIByType(selectedType, selectedAsset.MAXLV);

            Debug.Log($"{selectedType}: {GlobalValue.Instance.GetStatLevelByType(selectedType)}");
        }
    }

    /// <summary>
    /// 버튼 색만 바꿔 비활성 상태처럼 보이게 함.
    /// </summary>
    private void ChangeButtonToDisabled(Button button, bool command)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = command
            ? new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f)
            : Color.white;
    }

    private string GetDetailText(StatSO asset)
    {
        int LV = GlobalValue.Instance.GetStatLevelByType(asset.TYPE);
        float preValue = asset.BASE + asset.PER * LV;
        float postValue = preValue + asset.PER;

        string textPreValue;
        string textPostValue;

        switch (asset.FORMAT)
        {
            case StatSO.ValueType.Sec:
                textPreValue = $"{preValue:F3}초당 1";
                textPostValue = $"{postValue:F3}초당 1";
                break;
            case StatSO.ValueType.Rate:
                textPreValue = $"{(preValue * 100f):F1}%";
                textPostValue = $"{(postValue * 100f):F1}%";
                break;
            default:
                textPreValue = $"{preValue}";
                textPostValue = $"{postValue}";
                break;
        }

        return $"기본 {asset.NAME}(이)가 " +
               $"<size=180%><color=#ffff00>{textPreValue}</color></size>에서 " +
               $"<size=180%><color=#00ff00>{textPostValue}</color></size>로 증가";
    }

    private string GetAssetPath(StatController.StatType type)
        => STAT_SO_PRELINK + type + STAT_SO_POSTLINK;

    public StatSO SelectedAsset => selectedAsset;
}
