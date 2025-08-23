using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SkillWindowController;
//using static UnityEditor.Searcher.SearcherWindow;

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

    //1. 활성화 시 선택받은 스탯 종류에 맞게 Icon의 sprite 교체
    //2. StatSO 전달받아 저장된 정보 UI에 출력
    //3. 강화 버튼 누르면 팝업시킬 오류 메세지|| 알림 메세지 제작(니케st)

    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(()
            => GlobalValue.Instance
            && GlobalValue.Instance.IsDataLoaded);

        Inst = this;

        //InitBody();
        InitFoot();

        isInitialized = true;
    }

    private void InitFoot()
    {
        button_Confirm.onClick.AddListener(()
            => ConfirmButtonOnClick());
        button_Reset.onClick.AddListener(()
            => ResetButtonOnClick());
    }

    private void InitResetButton()
    {
        if (GlobalValue.Instance.GetSkillLevelByEnum(
            selectedAsset.OUTER
            , selectedAsset.INNER) > 0)
        {
            button_Reset.interactable = true;
        }
        else
        {
            button_Reset.interactable = false;
        }
    }

    public void UpdateDetailUI(SkillSO asset)
    {
        selectedAsset = asset;

        //1. Icon.
        Image_Icon.sprite = selectedAsset.ICON;
        //2. Name.
        int level = GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER);
        if (level == asset.MAXLV)
            Text_Name.text = $"[MAX] {selectedAsset.NAME}";
        else
            Text_Name.text = $"[Lv." +
                            $"{GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER).ToString()}] "
                            + selectedAsset.NAME;
        //3. Intro.
        Text_Intro.text = selectedAsset.INTRO;
        //4. Detail(INVEST: Top alignment, EQUIO: Bottom alignment).
        Text_Detail.text = GetDetailText(selectedAsset);
        //5. Cost(INVEST: Enable, EQUIP: Disable).
        Text_Cost.text = selectedAsset.COST.ToString();

        switch (SkillWindowController.Inst.CUR_MOD)
        {
            case SkillWindowController.MOD.INVEST:
                //Detail(Top alignment).
                Text_Detail.alignment = TextAlignmentOptions.Top;
                //Cost(Enable).
                Text_Cost.transform.parent.gameObject.SetActive(true);
                //Buttons(Enable).
                button_Confirm.gameObject.SetActive(true);
                button_Reset.gameObject.SetActive(true);
                break;
            case SkillWindowController.MOD.EQUIP:
                //Detail(Bottom alignment).
                Text_Detail.alignment = TextAlignmentOptions.Bottom;
                //Cost(Disable).
                Text_Cost.transform.parent.gameObject.SetActive(false);
                //Buttons(Disable).
                button_Confirm.gameObject.SetActive(false);
                button_Reset.gameObject.SetActive(false);
                break;
            default:
                break;
        }

        InitResetButton();
    }

    #region EVENT
    private void ResetButtonOnClick()
    {
        if (SkillWindowController.Inst.IsMessageWindowOnDisplay)
            return;

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        SkillWindowController.SKType OUTER = selectedAsset.OUTER;
        SkillWindowController.ELType INNER = selectedAsset.INNER;
        int currentLevel
            = GlobalValue.Instance.GetSkillLevelByEnum(OUTER, INNER);

        //1. Restore GEM.
        GlobalValue.Instance._Inven.SKGEM_CNT
            += selectedAsset.COST * currentLevel;

        //2. Reset selected skill level.
        GlobalValue.Instance.ElapseSkillLevelByEnum
            (OUTER,
            INNER,
            selectedAsset.MAXLV,
            -currentLevel);

        //3. Remove reset one from SkillInfo.
        GlobalValue.SkillOrder skillOrder = ConvertSkillTypeToOrder(OUTER);
        if (skillOrder != GlobalValue.SkillOrder.Count)
        {
            int equippedID
                    = GlobalValue
                    .Instance
                    .GetSkillIDsFromInfo()[(int)skillOrder];
            if (equippedID == selectedAsset.ID)
            {
                GlobalValue.Instance.RemoveSkillIDFromInfo(skillOrder);
                SkillWindowController.Inst.UpdateSkillButtonsOnSelect();
            }
        }

        //3. Pop up message window.
        StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(
                        $"[{selectedAsset.NAME}]{POSTNOTICE_RESET}"
                        , 1.5f));

        //4. Update UIs.
        UpdateDetailUI(selectedAsset);
        SkillWindowController.Inst.UpdateContentUI();
    }

    private void ConfirmButtonOnClick()
    {
        if (SkillWindowController.Inst.IsMessageWindowOnDisplay)
            return;

        //--- Exception care
        //1. Alert: already max level
        if (GlobalValue.Instance.GetSkillLevelByEnum(
            selectedAsset.OUTER
            , selectedAsset.INNER)
            == selectedAsset.MAXLV)
        {
            //Play SFX: Alert.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(
                            ALERT_MAX_LEVEL
                            , 1f));

            return;
        }
        //2. Alert: out of GEM
        else if (selectedAsset.COST
                > GlobalValue.Instance._Inven.SKGEM_CNT)
        {
            //Play SFX: Alert.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(
                            ALERT_GEM
                            , 1f));

            return;
        }
        //---

        //--- LEVEL UP!!
        //Play SFX: Level up.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Confirm);

        //1. Increase skill level.
        int test = GlobalValue.Instance.ElapseSkillLevelByEnum(
            selectedAsset.OUTER
            , selectedAsset.INNER
            , selectedAsset.MAXLV
            , 1);

        Debug.Log($"[LEVEL UP]({(int)selectedAsset.OUTER},{(int)selectedAsset.INNER}){test}");

        //2. Decrease GEM.
        GlobalValue.Instance._Inven.SKGEM_CNT -= selectedAsset.COST;
        GlobalValue.Instance.SaveInven();

        //3. Update UI.
        UpdateDetailUI(selectedAsset);
        SkillWindowController.Inst.UpdateContentUI();
        //---
    }
    #endregion

    #region GET
    private string GetDetailText(SkillSO asset)
    {
        SkillSO.SkillAbility mainAbility = asset.MainAbility;
        SkillSO.SkillAbility subAbility = asset.SubAbility;

        int LV = GlobalValue.Instance.GetSkillLevelByEnum(
                asset.OUTER, asset.INNER);

        float mainPreValue = mainAbility.BASE + mainAbility.PER * LV;
        float mainPostValue = mainPreValue + mainAbility.PER;
        float subPreValue = subAbility.BASE + subAbility.PER * LV;
        float subPostValue = subPreValue + subAbility.PER;

        //레벨이 0일 때 액티브 스킬의 경우 이전 능력치를 0으로 표기
        if (LV == 0 && asset.OUTER != SkillWindowController.SKType.Passive)
            mainPreValue = (subPreValue = 0f);

        string mainPreText = ConcatStringByFormat(mainPreValue, mainAbility.FORMAT);
        string mainPostText = ConcatStringByFormat(mainPostValue, mainAbility.FORMAT);
        string subPreText = ConcatStringByFormat(subPreValue, subAbility.FORMAT);
        string subPostText = ConcatStringByFormat(subPostValue, subAbility.FORMAT);

        string mainText = "";
        string subText = "";

        if (LV == asset.MAXLV
            || SkillWindowController.Inst.CUR_MOD == SkillWindowController.MOD.EQUIP)
        {
            mainText = $"{mainAbility.NAME} <size=150%><color=#00ff00>{mainPreText}</color></size>";
            if (subAbility.NAME != "")
                subText = $"<br>{subAbility.NAME} <size=150%><color=#00ff00>{subPreText}</color></size>";
        }
        else
        {
            mainText = $"{mainAbility.NAME} <size=150%><color=#ffff00>{mainPreText}</color></size>에서 <size=150%><color=#00ff00>{mainPostText}</color></size>";
            if (subAbility.NAME != "")
                subText = $"<br>{subAbility.NAME} <size=150%><color=#ffff00>{subPreText}</color></size>에서 <size=150%><color=#00ff00>{subPostText}</color></size>";
        }

        return mainText + subText;
    }

    private string ConcatStringByFormat
        (
        float value
        , SkillSO.ValueType format
        )
    {
        string result;

        switch (format)
        {
            case SkillSO.ValueType.Sec:
                result = $"{value}초";
                break;
            case SkillSO.ValueType.Rate:
                result = $"{(value * 100f).ToString("F1")}%";
                break;
            default:
                result = $"{value}";
                break;
        }

        return result;

        //return value + format.ToString();
    }

    private GlobalValue.SkillOrder ConvertSkillTypeToOrder(SKType type)
    {
        switch (type)
        {
            case SKType.Defense:
                return GlobalValue.SkillOrder.Q;
            case SKType.Active:
                return GlobalValue.SkillOrder.E;
            default:
                return GlobalValue.SkillOrder.Count;
        }
    }
    #endregion
}
