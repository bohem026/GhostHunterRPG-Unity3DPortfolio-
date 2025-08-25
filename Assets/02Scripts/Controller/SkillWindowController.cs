using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        // ���� ������ �غ� ���
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);

        Inst = this;
        InitHeader();
        InitBody();

        // �ʱ� ��� �� ����(���� ���� ����)
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
    /// ��ų ��ư�� ����/��õ/���� ���¸� �ϰ� �ʱ�ȭ�մϴ�.
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
    /// ���� ������ ��ų ��ư�� ���� ǥ�÷� �����մϴ�.
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
    /// ��ų ��ư/��� ��ư�� Ŭ�� �����ʸ� �����մϴ�.
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
    /// ��ų ���� ��带 ���(����������)�ϰ� ��ư ��ȣ�ۿ�/����� �����մϴ�.
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
    /// ��ų ��ư Ŭ�� �� ��â ���� �� (���� ����� ���) ����/���� ó��.
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
    /// ���õ� ��ų�� ����/���� ��ȭ�� �ֽ� ���·� �����մϴ�.
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
    /// ���� �ð� ���� �޽��� �˾��� ǥ���մϴ�.
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
