using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillWindowController : MonoBehaviour
{
    private const string TITLE_INVEST = "스킬연구";
    private const string TITLE_EQUIP = "스킬장착";
    private const string INTRO_INVEST = "스킬을 연구할 수 있습니다.";
    private const string INTRO_EQUIP = "스킬을 선택해서 장착할 수 있습니다.";
    private const string POSTNOTICE_EQUIP = " 스킬을 장착했습니다.";
    private const string POSTNOTICE_UNEQUIP = " 스킬을 해제했습니다.";
    private const string SKILL_SO_PRELINK = "ScriptableObjects/Skill/";
    private const string SKILL_SO_POSTLINK = "Asset";

    public static SkillWindowController Inst;

    public enum MOD { INVEST, EQUIP, COUNT }
    public enum SKType { Passive, Defense, Active, Count /*Length*/ }
    public enum ELType { Fire, Ice, Light, Poison, Count /*Length*/ }

    [Header("HEADER")]
    [SerializeField] private Text text_Title;
    [SerializeField] private Text text_Intro;
    [SerializeField] private Button button_Back;
    [SerializeField] private Text text_Gem;

    [Header("BODY")]
    public List<SkillButtonInnerList> skillButtonList;
    [SerializeField] private Sprite sprite_Select;
    [SerializeField] private Sprite sprite_Normal;
    [SerializeField] private List<Button> MODButtons;
    [SerializeField] private List<GameObject> NewIndicators;

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Detail;
    [SerializeField] private GameObject window_Message;

    [HideInInspector] public MOD CUR_MOD;
    private bool isInitialized;
    private SkillSO selectedAsset;

    private void OnEnable()
    {
        CUR_MOD = MOD.EQUIP;
        ChangeMODAndInit();

        if (!isInitialized)
            StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        // 전역 데이터 준비 대기
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);

        Inst = this;
        InitHeader();
        InitBody();

        // 초기 모드 값 설정(동작 변경 없음)
        CUR_MOD = MOD.INVEST;
        isInitialized = true;
    }

    private void InitHeader()
    {
        button_Back.onClick.AddListener(() =>
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        text_Gem.text = GlobalValue.Instance._Inven.SKGEM_CNT.ToString();
    }

    private void InitBody()
    {
        InitAllContentUI();
        AddButtonListener();
    }

    /// <summary>
    /// 스킬 버튼의 레벨/추천/선택 상태를 일괄 초기화합니다.
    /// </summary>
    private void InitAllContentUI()
    {
        SkillSO asset;
        TextMeshProUGUI levelText;
        int level;

        if ((skillButtonList.Capacity != (int)SKType.Count) ||
            (skillButtonList[0].buttons.Capacity != (int)ELType.Count))
        {
            Debug.Log("[!!ERROR!!] MISMATCH BETWEEN SKILL LIST AND ENUM");
            return;
        }

        for (int outer = 0; outer < (int)SKType.Count; outer++)
        {
            for (int inner = 0; inner < (int)ELType.Count; inner++)
            {
                int capturedOuter = outer;
                int capturedInner = inner;

                asset = ResourceUtility.GetResourceByType<SkillSO>(
                    GetAssetPath((SKType)capturedOuter, (ELType)capturedInner));

                levelText = skillButtonList[capturedOuter]
                                .buttons[capturedInner]
                                .GetComponentInChildren<TextMeshProUGUI>();

                level = GlobalValue.Instance.GetSkillLevelByEnum(
                            (SKType)capturedOuter, (ELType)capturedInner);

                levelText.text = $"<color=#00ff00><size=125%>{level}</size></color> /{asset.MAXLV}";
            }
        }

        foreach (var item in NewIndicators)
            item.SetActive(GlobalValue.Instance._Info.FIRST_IN_GAME);

        UpdateSkillButtonsOnSelect();
    }

    /// <summary>
    /// 현재 장착된 스킬 버튼만 선택 표시로 갱신합니다.
    /// </summary>
    public void UpdateSkillButtonsOnSelect()
    {
        SkillSO asset;
        List<int> skillIDs = GlobalValue.Instance.GetSkillIDsFromInfo();

        for (int outer = 0; outer < (int)SKType.Count; outer++)
        {
            for (int inner = 0; inner < (int)ELType.Count; inner++)
            {
                int capturedOuter = outer;
                int capturedInner = inner;

                asset = ResourceUtility.GetResourceByType<SkillSO>(
                    GetAssetPath((SKType)capturedOuter, (ELType)capturedInner));

                SkillButtonOnSelect(skillButtonList[capturedOuter].buttons[capturedInner], false);

                foreach (var id in skillIDs)
                {
                    if (asset.ID == id)
                    {
                        SkillButtonOnSelect(skillButtonList[capturedOuter].buttons[capturedInner], true);
                    }
                }
            }
        }
    }

    private void SkillButtonOnSelect(Button button, bool command)
    {
        button.GetComponent<Image>().sprite = command ? sprite_Select : sprite_Normal;
    }

    /// <summary>
    /// 스킬 버튼/모드 버튼에 클릭 리스너를 연결합니다.
    /// </summary>
    private void AddButtonListener()
    {
        for (int outer = 0; outer < (int)SKType.Count; outer++)
        {
            for (int inner = 0; inner < (int)ELType.Count; inner++)
            {
                int capturedOuter = outer;
                int capturedInner = inner;

                skillButtonList[outer].buttons[inner]
                    .onClick.AddListener(() => SkillButtonOnClick(capturedOuter, capturedInner));
            }
        }

        foreach (var item in MODButtons)
            item.onClick.AddListener(ChangeMODAndInit);
    }

    /// <summary>
    /// 스킬 관리 모드를 토글(연구↔장착)하고 버튼 상호작용/헤더를 갱신합니다.
    /// </summary>
    private void ChangeMODAndInit()
    {
        if (IsMessageWindowOnDisplay) return;

        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        int OUTER_LEN = (int)SKType.Count;
        int INNER_LEN = (int)ELType.Count;

        switch (CUR_MOD)
        {
            case MOD.INVEST:
                CUR_MOD = MOD.EQUIP;
                text_Title.text = TITLE_EQUIP;
                text_Intro.text = INTRO_EQUIP;

                for (int outer = 0; outer < OUTER_LEN; outer++)
                {
                    for (int inner = 0; inner < INNER_LEN; inner++)
                    {
                        int capturedOuter = outer;
                        int capturedInner = inner;
                        int level = GlobalValue.Instance.GetSkillLevelByEnum(
                                        (SKType)outer, (ELType)inner);

                        bool interactable = !(capturedOuter == (int)SKType.Passive || level == 0);
                        skillButtonList[capturedOuter].buttons[capturedInner].interactable = interactable;
                    }
                }
                break;

            case MOD.EQUIP:
                CUR_MOD = MOD.INVEST;
                text_Title.text = TITLE_INVEST;
                text_Intro.text = INTRO_INVEST;

                for (int outer = 0; outer < OUTER_LEN; outer++)
                {
                    for (int inner = 0; inner < INNER_LEN; inner++)
                    {
                        int capturedOuter = outer;
                        int capturedInner = inner;
                        skillButtonList[capturedOuter].buttons[capturedInner].interactable = true;
                    }
                }
                break;
        }

        if (window_Detail.activeSelf)
            window_Detail.SetActive(false);
    }

    /// <summary>
    /// 스킬 버튼 클릭 시 상세창 갱신 및 (장착 모드일 경우) 장착/해제 처리.
    /// </summary>
    public void SkillButtonOnClick(int outer, int inner)
    {
        if (IsMessageWindowOnDisplay) return;

        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        selectedAsset = GetAsset((SKType)outer, (ELType)inner);
        if (!selectedAsset)
        {
            Debug.Log("[!!ERROR!!] FAILED TO LOAD ASSET");
            return;
        }

        Debug.Log($"[SELECT]({outer},{inner}) ID({selectedAsset.ID})");

        window_Detail.GetComponent<SkillDetailWindowController>()
                     .UpdateDetailUI(selectedAsset);

        if (!window_Detail.activeSelf)
            window_Detail.SetActive(true);

        if (CUR_MOD == MOD.EQUIP)
        {
            GlobalValue.SkillOrder order = ConvertSkillTypeToOrder(selectedAsset.OUTER);
            int equippedID = GlobalValue.Instance.GetSkillIDsFromInfo()[(int)order];

            if (equippedID == selectedAsset.ID)
            {
                GlobalValue.Instance.RemoveSkillIDFromInfo(order);
                StartCoroutine(DisplayMessageWindow($"[{selectedAsset.NAME}]{POSTNOTICE_UNEQUIP}", 1.5f));
            }
            else
            {
                GlobalValue.Instance.SwitchSkillIDFromInfo(selectedAsset.ID, order);
                StartCoroutine(DisplayMessageWindow($"[{selectedAsset.NAME}]{POSTNOTICE_EQUIP}", 1.5f));
            }

            UpdateSkillButtonsOnSelect();
        }
    }

    private void OnDisable()
    {
        StopCoroutine("DisplayMessageWindow");
        window_Message.SetActive(false);
    }

    /// <summary>
    /// 선택된 스킬의 레벨/보유 재화를 최신 상태로 갱신합니다.
    /// </summary>
    public void UpdateContentUI()
    {
        TextMeshProUGUI levelText;
        int outer = (int)selectedAsset.OUTER;
        int inner = (int)selectedAsset.INNER;

        levelText = skillButtonList[outer].buttons[inner].GetComponentInChildren<TextMeshProUGUI>();
        int level = GlobalValue.Instance.GetSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER);

        levelText.text = $"<color=#00ff00><size=125%>{level}</size></color> /{selectedAsset.MAXLV}";
        text_Gem.text = GlobalValue.Instance._Inven.SKGEM_CNT.ToString();
    }

    /// <summary>
    /// 일정 시간 동안 메시지 팝업을 표시합니다.
    /// </summary>
    public IEnumerator DisplayMessageWindow(string message, float duration)
    {
        window_Message.GetComponentInChildren<Text>().text = message;
        window_Message.SetActive(true);
        yield return new WaitForSecondsRealtime(duration);
        window_Message.SetActive(false);
    }

    private GlobalValue.SkillOrder ConvertSkillTypeToOrder(SKType type)
    {
        switch (type)
        {
            case SKType.Defense: return GlobalValue.SkillOrder.Q;
            case SKType.Active: return GlobalValue.SkillOrder.E;
            default: return GlobalValue.SkillOrder.Count;
        }
    }

    #region GET
    private SkillSO GetAsset(SKType outer, ELType inner)
        => ResourceUtility.GetResourceByType<SkillSO>(GetAssetPath(outer, inner));

    private string GetAssetPath(SKType outer, ELType inner)
        => SKILL_SO_PRELINK + outer + inner + SKILL_SO_POSTLINK;

    public bool IsMessageWindowOnDisplay => window_Message.activeSelf;
    #endregion
}

[Serializable]
public class SkillButtonInnerList
{
    public List<Button> buttons;
}
