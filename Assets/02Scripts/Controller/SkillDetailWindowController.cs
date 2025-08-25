using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SkillWindowController;

public class SkillDetailWindowController : MonoBehaviour
{
    private const string POSTNOTICE_RESET = "의 연구 레벨을 초기화했습니다.";
    private const string ALERT_MAX_LEVEL = "이미 최고 레벨입니다.";
    private const string ALERT_GEM = "재화가 부족합니다.";

    public static SkillDetailWindowController Inst;

    [Header("BODY")]
    [SerializeField] private Image Image_Icon;
    [SerializeField] private Text Text_Name;
    [SerializeField] private Text Text_Intro;
    [SerializeField] private TextMeshProUGUI Text_Detail;
    [SerializeField] private Text Text_Cost;

    [Header("FOOT")]
    [SerializeField] private Button button_Confirm;
    [SerializeField] private Button button_Reset;

    private bool isInitialized;
    private SkillSO selectedAsset;

    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        // 전역 저장소 로딩 완료 대기
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);

        Inst = this;
        InitFoot();
        isInitialized = true;
    }

    private void InitFoot()
    {
        // 강화/초기화 버튼 리스너 바인딩
        button_Confirm.onClick.AddListener(ConfirmButtonOnClick);
        button_Reset.onClick.AddListener(ResetButtonOnClick);
    }

    private void InitResetButton()
    {
        // 현재 레벨이 0이면 초기화 비활성화
        button_Reset.interactable =
            GlobalValue.Instance.GetSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER) > 0;
    }

    /// <summary>
    /// 선택된 스킬의 상세 정보(아이콘/이름/설명/수치/비용)와 버튼 상태를 갱신합니다.
    /// </summary>
    public void UpdateDetailUI(SkillSO asset)
    {
        selectedAsset = asset;

        // 1) 아이콘/이름/소개
        Image_Icon.sprite = selectedAsset.ICON;

        int level = GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER);
        Text_Name.text = (level == asset.MAXLV)
            ? $"[MAX] {selectedAsset.NAME}"
            : $"[Lv.{GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER)}] {selectedAsset.NAME}";

        Text_Intro.text = selectedAsset.INTRO;

        // 2) 상세 수치(레벨 및 모드에 따라 표시 형식 다름)
        Text_Detail.text = GetDetailText(selectedAsset);

        // 3) 비용/버튼 가시성: 투자 모드면 표시, 장착 모드면 숨김
        Text_Cost.text = selectedAsset.COST.ToString();

        switch (SkillWindowController.Inst.CUR_MOD)
        {
            case SkillWindowController.MOD.INVEST:
                Text_Detail.alignment = TextAlignmentOptions.Top;
                Text_Cost.transform.parent.gameObject.SetActive(true);
                button_Confirm.gameObject.SetActive(true);
                button_Reset.gameObject.SetActive(true);
                break;
            case SkillWindowController.MOD.EQUIP:
                Text_Detail.alignment = TextAlignmentOptions.Bottom;
                Text_Cost.transform.parent.gameObject.SetActive(false);
                button_Confirm.gameObject.SetActive(false);
                button_Reset.gameObject.SetActive(false);
                break;
        }

        InitResetButton();
    }

    #region Button Handlers

    /// <summary>
    /// 스킬 레벨 초기화(환급 및 장착 해제 포함) 처리.
    /// </summary>
    private void ResetButtonOnClick()
    {
        if (SkillWindowController.Inst.IsMessageWindowOnDisplay) return;

        // Click SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        var OUTER = selectedAsset.OUTER;
        var INNER = selectedAsset.INNER;
        int currentLevel = GlobalValue.Instance.GetSkillLevelByEnum(OUTER, INNER);

        // 1) 재화 환급
        GlobalValue.Instance._Inven.SKGEM_CNT += selectedAsset.COST * currentLevel;

        // 2) 레벨 초기화
        GlobalValue.Instance.ElapseSkillLevelByEnum(OUTER, INNER, selectedAsset.MAXLV, -currentLevel);

        // 3) 장착 중이었다면 장착 해제 및 버튼 갱신
        GlobalValue.SkillOrder order = ConvertSkillTypeToOrder(OUTER);
        if (order != GlobalValue.SkillOrder.Count)
        {
            int equippedID = GlobalValue.Instance.GetSkillIDsFromInfo()[(int)order];
            if (equippedID == selectedAsset.ID)
            {
                GlobalValue.Instance.RemoveSkillIDFromInfo(order);
                SkillWindowController.Inst.UpdateSkillButtonsOnSelect();
            }
        }

        // 4) 안내 메시지 표시
        StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(
            $"[{selectedAsset.NAME}]{POSTNOTICE_RESET}", 1.5f));

        // 5) UI 갱신
        UpdateDetailUI(selectedAsset);
        SkillWindowController.Inst.UpdateContentUI();
    }

    /// <summary>
    /// 스킬 강화(레벨 +1, 비용 차감) 처리.
    /// </summary>
    private void ConfirmButtonOnClick()
    {
        if (SkillWindowController.Inst.IsMessageWindowOnDisplay) return;

        // 1) 예외: 최대 레벨
        if (GlobalValue.Instance.GetSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER) == selectedAsset.MAXLV)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(ALERT_MAX_LEVEL, 1f));
            return;
        }

        // 2) 예외: 재화 부족
        if (selectedAsset.COST > GlobalValue.Instance._Inven.SKGEM_CNT)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(ALERT_GEM, 1f));
            return;
        }

        // 3) 강화 처리
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);
        GlobalValue.Instance.ElapseSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER, selectedAsset.MAXLV, 1);

        GlobalValue.Instance._Inven.SKGEM_CNT -= selectedAsset.COST;
        GlobalValue.Instance.SaveInven();

        // 4) UI 갱신
        UpdateDetailUI(selectedAsset);
        SkillWindowController.Inst.UpdateContentUI();
    }

    #endregion

    #region Helpers / GET

    /// <summary>
    /// 현재 레벨/다음 레벨 수치를 형식(초/%)에 맞춰 가공하여 상세 텍스트를 생성합니다.
    /// </summary>
    private string GetDetailText(SkillSO asset)
    {
        var main = asset.MainAbility;
        var sub = asset.SubAbility;

        int LV = GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER);

        float mainPre = main.BASE + main.PER * LV;
        float mainPost = mainPre + main.PER;
        float subPre = sub.BASE + sub.PER * LV;
        float subPost = subPre + sub.PER;

        // 레벨 0인 액티브/디펜스 스킬은 현재값 0 표기
        if (LV == 0 && asset.OUTER != SkillWindowController.SKType.Passive)
        {
            mainPre = 0f;
            subPre = 0f;
        }

        string mainPreText = ConcatStringByFormat(mainPre, main.FORMAT);
        string mainPostText = ConcatStringByFormat(mainPost, main.FORMAT);
        string subPreText = ConcatStringByFormat(subPre, sub.FORMAT);
        string subPostText = ConcatStringByFormat(subPost, sub.FORMAT);

        bool showAsFinal = LV == asset.MAXLV || SkillWindowController.Inst.CUR_MOD == SkillWindowController.MOD.EQUIP;

        string mainText = showAsFinal
            ? $"{main.NAME} <size=150%><color=#00ff00>{mainPreText}</color></size>"
            : $"{main.NAME} <size=150%><color=#ffff00>{mainPreText}</color></size>에서 <size=150%><color=#00ff00>{mainPostText}</color></size>";

        string subText = "";
        if (!string.IsNullOrEmpty(sub.NAME))
        {
            subText = showAsFinal
                ? $"<br>{sub.NAME} <size=150%><color=#00ff00>{subPreText}</color></size>"
                : $"<br>{sub.NAME} <size=150%><color=#ffff00>{subPreText}</color></size>에서 <size=150%><color=#00ff00>{subPostText}</color></size>";
        }

        return mainText + subText;
    }

    /// <summary>
    /// 값 형식을 초/퍼센트/수치로 포맷합니다.
    /// </summary>
    private string ConcatStringByFormat(float value, SkillSO.ValueType format)
    {
        switch (format)
        {
            case SkillSO.ValueType.Sec: return $"{value}초";
            case SkillSO.ValueType.Rate: return $"{(value * 100f).ToString("F1")}%";
            default: return $"{value}";
        }
    }

    /// <summary>
    /// 스킬 종류를 단축키 슬롯(Q/E) 구분자로 변환합니다.
    /// </summary>
    private GlobalValue.SkillOrder ConvertSkillTypeToOrder(SKType type)
    {
        switch (type)
        {
            case SKType.Defense: return GlobalValue.SkillOrder.Q;
            case SKType.Active: return GlobalValue.SkillOrder.E;
            default: return GlobalValue.SkillOrder.Count;
        }
    }

    #endregion
}
