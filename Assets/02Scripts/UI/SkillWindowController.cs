using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using static Cinemachine.DocumentationSortingAttribute;
using static UnityEngine.InputSystem.InputRemoting;

public class SkillWindowController : MonoBehaviour
{
    private const string TITLE_INVEST = "��ų����";
    private const string TITLE_EQUIP = "��ų����";
    private const string INTRO_INVEST = "��ų�� ������ �� �ֽ��ϴ�.";
    private const string INTRO_EQUIP = "��ų�� �����ؼ� ������ �� �ֽ��ϴ�.";
    private const string POSTNOTICE_EQUIP = " ��ų�� �����߽��ϴ�.";
    private const string POSTNOTICE_UNEQUIP = " ��ų�� �����߽��ϴ�.";
    private const string SKILL_SO_PRELINK = "ScriptableObjects/Skill/";
    private const string SKILL_SO_POSTLINK = "Asset";
    private const int INDEX_FIRST_RECOMMENDED = 1;

    public static SkillWindowController Inst;

    public enum MOD
    { INVEST, EQUIP, COUNT }
    public enum SKType
    { Passive, Defense, Active, Count/*Length*/}
    public enum ELType
    { Fire, Ice, Light, Poison, Count/*Length*/}

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

    IEnumerator Init()
    {
        yield return new WaitUntil(()
            => GlobalValue.Instance
            && GlobalValue.Instance.IsDataLoaded);

        Inst = this;

        InitHeader();
        InitBody();

        CUR_MOD = MOD.INVEST;
        isInitialized = true;
    }

    private void InitHeader()
    {
        button_Back.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        //--- TEXT_GEM
        text_Gem.text = GlobalValue.Instance._Inven
            .SKGEM_CNT.ToString();
        //---
    }

    private void InitBody()
    {
        InitAllContentUI();
        AddButtonListener();
    }

    private void InitAllContentUI()
    {
        SkillSO asset;
        TextMeshProUGUI levelText;
        int level;

        if ((skillButtonList.Capacity
            != (int)SKType.Count)
            ||
            (skillButtonList[0].buttons.Capacity
            != (int)ELType.Count))
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

                asset = ResourceUtility.GetResourceByType<SkillSO>
                    (GetAssetPath((SKType)capturedOuter, (ELType)capturedInner));
                levelText = skillButtonList[capturedOuter]
                            .buttons[capturedInner]
                            .GetComponentInChildren<TextMeshProUGUI>();
                level = GlobalValue.Instance
                        .GetSkillLevelByEnum
                        ((SKType)capturedOuter, (ELType)capturedInner);
                levelText.text = $"<color=#00ff00><size=125%>{level}</size></color>" +
                                $" /{asset.MAXLV}";
            }
        }

        // Display recommended skills.
        foreach (var item in NewIndicators)
        {
            item.SetActive(GlobalValue.Instance._Info.FIRST_IN_GAME);
        }

        // Select if is registered in SkillInfo.
        UpdateSkillButtonsOnSelect();
    }

    /// <summary>
    /// ���� ����� ��ų�� ��ư�� UI�� �����ϴ� �޼��� �Դϴ�.
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

                asset = ResourceUtility.GetResourceByType<SkillSO>
                    (GetAssetPath((SKType)capturedOuter, (ELType)capturedInner));

                //1. Unselect all skill buttons.
                SkillButtonOnSelect
                    (skillButtonList[capturedOuter].buttons[capturedInner],
                    false);

                //2. Select if is registered in SkillInfo.
                foreach (var item in skillIDs)
                {
                    if (asset.ID == item)
                    {
                        SkillButtonOnSelect
                            (skillButtonList[capturedOuter].buttons[capturedInner],
                            true);
                    }
                }
            }
        }
    }

    private void SkillButtonOnSelect
        (
        Button button,
        bool command
        )
    {
        button.GetComponent<Image>().sprite
            = command ? sprite_Select : sprite_Normal;
    }

    private void AddButtonListener()
    {
        Button button;

        //1. Skill buttons.
        for (int outer = 0; outer < (int)SKType.Count; outer++)
        {
            for (int inner = 0; inner < (int)ELType.Count; inner++)
            {
                int capturedOuter = outer;
                int capturedInner = inner;

                button = skillButtonList[outer].buttons[inner];
                button.onClick.AddListener(()
                    => SkillButtonOnClick(capturedOuter, capturedInner));
            }
        }

        //2. MOD buttons.
        foreach (var item in MODButtons)
        {
            item.onClick.AddListener(ChangeMODAndInit);
        }
    }

    /// <summary>
    /// ��ų ���� ��带 �����ϴ� �޼��� �Դϴ�.
    /// ���� '����'�� '����' 2���� �Դϴ�.
    /// </summary>
    private void ChangeMODAndInit()
    {
        if (IsMessageWindowOnDisplay)
            return;

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        int OUTER_LEN = (int)SKType.Count;
        int INNER_LEN = (int)ELType.Count;

        //1. MOD SWITCH
        switch (CUR_MOD)
        {
            case MOD.INVEST:
                // Change MOD to EQUIP.
                CUR_MOD = MOD.EQUIP;

                // Change header text.
                text_Title.text = TITLE_EQUIP;
                text_Intro.text = INTRO_EQUIP;

                // Change skill buttons.
                for (int outer = 0; outer < OUTER_LEN; outer++)
                {
                    for (int inner = 0; inner < INNER_LEN; inner++)
                    {
                        int capturedOuter = outer;
                        int capturedInner = inner;
                        int level = GlobalValue.Instance
                                    .GetSkillLevelByEnum((SKType)outer, (ELType)inner);

                        //Set uninteractable if is passive type or not invested one.
                        if (capturedOuter == (int)SKType.Passive || level == 0)
                            skillButtonList[capturedOuter].buttons[capturedInner]
                                .interactable = false;
                        else
                            skillButtonList[capturedOuter].buttons[capturedInner]
                                .interactable = true;
                    }
                }

                break;
            case MOD.EQUIP:
                // Change MOD to INVEST.
                CUR_MOD = MOD.INVEST;

                // Change header text.
                text_Title.text = TITLE_INVEST;
                text_Intro.text = INTRO_INVEST;

                // Change skill buttons.
                for (int outer = 0; outer < OUTER_LEN; outer++)
                {
                    for (int inner = 0; inner < INNER_LEN; inner++)
                    {
                        int capturedOuter = outer;
                        int capturedInner = inner;

                        skillButtonList[capturedOuter].buttons[capturedInner]
                            .interactable = true;
                    }
                }

                break;
            default:
                break;
        }

        //2. Set detail window disable.
        if (window_Detail.activeSelf)
            window_Detail.SetActive(false);
    }

    public void SkillButtonOnClick(int outer, int inner)
    {
        if (IsMessageWindowOnDisplay)
            return;

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

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

        //--- EQUIP MOD
        if (CUR_MOD == MOD.EQUIP)
        {
            GlobalValue.SkillOrder skillOrder
                = ConvertSkillTypeToOrder(selectedAsset.OUTER);
            int equippedID
                = GlobalValue.Instance.GetSkillIDsFromInfo()[(int)skillOrder];

            //1. Update skill ID in SkillInfo.
            //Delete skill ID already equipped.
            if (equippedID == selectedAsset.ID)
            {
                GlobalValue.Instance.RemoveSkillIDFromInfo(skillOrder);

                //Pop up message window.
                StartCoroutine
                    (DisplayMessageWindow
                    ($"[{selectedAsset.NAME}]{POSTNOTICE_UNEQUIP}",
                    1.5f));
            }
            //Switch skill ID with already equipped one.
            else
            {
                GlobalValue.Instance.SwitchSkillIDFromInfo
                    (selectedAsset.ID,
                    skillOrder);

                //Pop up message window.
                StartCoroutine
                    (DisplayMessageWindow
                    ($"[{selectedAsset.NAME}]{POSTNOTICE_EQUIP}",
                    1.5f));
            }

            //2. Update skill buttons by SkillInfo.
            UpdateSkillButtonsOnSelect();
        }
        //---
    }

    private void OnDisable()
    {
        StopCoroutine("DisplayMessageWindow");
        window_Message.SetActive(false);
    }

    public void UpdateContentUI()
    {
        TextMeshProUGUI levelText;
        int level;
        int outer = (int)selectedAsset.OUTER;
        int inner = (int)selectedAsset.INNER;

        levelText = skillButtonList[outer]
                    .buttons[inner]
                    .GetComponentInChildren<TextMeshProUGUI>();
        level = GlobalValue.Instance.GetSkillLevelByEnum(
                selectedAsset.OUTER
                , selectedAsset.INNER);
        levelText.text = $"<color=#00ff00><size=125%>{level}</size></color>" +
                        $" /{selectedAsset.MAXLV}";
        text_Gem.text = GlobalValue.Instance._Inven.SKGEM_CNT.ToString();
    }

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
            case SKType.Defense:
                return GlobalValue.SkillOrder.Q;
            case SKType.Active:
                return GlobalValue.SkillOrder.E;
            default:
                return GlobalValue.SkillOrder.Count;
        }
    }

    #region GET
    private SkillSO GetAsset
        (
        SkillWindowController.SKType outer
        , SkillWindowController.ELType inner
        )
        => ResourceUtility.GetResourceByType<SkillSO>(
            GetAssetPath(outer, inner));

    private string GetAssetPath
        (
        SkillWindowController.SKType outer
        , SkillWindowController.ELType inner
        )
        => SKILL_SO_PRELINK + outer.ToString()
        + inner.ToString() + SKILL_SO_POSTLINK;

    public bool IsMessageWindowOnDisplay => window_Message.activeSelf;
    #endregion
}

[Serializable]
public class SkillButtonInnerList
{
    public List<Button> buttons;
}
