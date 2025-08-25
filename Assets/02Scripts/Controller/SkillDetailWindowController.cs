using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SkillWindowController;

public class SkillDetailWindowController : MonoBehaviour
{
    private const string POSTNOTICE_RESET = "�� ���� ������ �ʱ�ȭ�߽��ϴ�.";
    private const string ALERT_MAX_LEVEL = "�̹� �ְ� �����Դϴ�.";
    private const string ALERT_GEM = "��ȭ�� �����մϴ�.";

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
        // ���� ����� �ε� �Ϸ� ���
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);

        Inst = this;
        InitFoot();
        isInitialized = true;
    }

    private void InitFoot()
    {
        // ��ȭ/�ʱ�ȭ ��ư ������ ���ε�
        button_Confirm.onClick.AddListener(ConfirmButtonOnClick);
        button_Reset.onClick.AddListener(ResetButtonOnClick);
    }

    private void InitResetButton()
    {
        // ���� ������ 0�̸� �ʱ�ȭ ��Ȱ��ȭ
        button_Reset.interactable =
            GlobalValue.Instance.GetSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER) > 0;
    }

    /// <summary>
    /// ���õ� ��ų�� �� ����(������/�̸�/����/��ġ/���)�� ��ư ���¸� �����մϴ�.
    /// </summary>
    public void UpdateDetailUI(SkillSO asset)
    {
        selectedAsset = asset;

        // 1) ������/�̸�/�Ұ�
        Image_Icon.sprite = selectedAsset.ICON;

        int level = GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER);
        Text_Name.text = (level == asset.MAXLV)
            ? $"[MAX] {selectedAsset.NAME}"
            : $"[Lv.{GlobalValue.Instance.GetSkillLevelByEnum(asset.OUTER, asset.INNER)}] {selectedAsset.NAME}";

        Text_Intro.text = selectedAsset.INTRO;

        // 2) �� ��ġ(���� �� ��忡 ���� ǥ�� ���� �ٸ�)
        Text_Detail.text = GetDetailText(selectedAsset);

        // 3) ���/��ư ���ü�: ���� ���� ǥ��, ���� ���� ����
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
    /// ��ų ���� �ʱ�ȭ(ȯ�� �� ���� ���� ����) ó��.
    /// </summary>
    private void ResetButtonOnClick()
    {
        if (SkillWindowController.Inst.IsMessageWindowOnDisplay) return;

        // Click SFX
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        var OUTER = selectedAsset.OUTER;
        var INNER = selectedAsset.INNER;
        int currentLevel = GlobalValue.Instance.GetSkillLevelByEnum(OUTER, INNER);

        // 1) ��ȭ ȯ��
        GlobalValue.Instance._Inven.SKGEM_CNT += selectedAsset.COST * currentLevel;

        // 2) ���� �ʱ�ȭ
        GlobalValue.Instance.ElapseSkillLevelByEnum(OUTER, INNER, selectedAsset.MAXLV, -currentLevel);

        // 3) ���� ���̾��ٸ� ���� ���� �� ��ư ����
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

        // 4) �ȳ� �޽��� ǥ��
        StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(
            $"[{selectedAsset.NAME}]{POSTNOTICE_RESET}", 1.5f));

        // 5) UI ����
        UpdateDetailUI(selectedAsset);
        SkillWindowController.Inst.UpdateContentUI();
    }

    /// <summary>
    /// ��ų ��ȭ(���� +1, ��� ����) ó��.
    /// </summary>
    private void ConfirmButtonOnClick()
    {
        if (SkillWindowController.Inst.IsMessageWindowOnDisplay) return;

        // 1) ����: �ִ� ����
        if (GlobalValue.Instance.GetSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER) == selectedAsset.MAXLV)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(ALERT_MAX_LEVEL, 1f));
            return;
        }

        // 2) ����: ��ȭ ����
        if (selectedAsset.COST > GlobalValue.Instance._Inven.SKGEM_CNT)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(SkillWindowController.Inst.DisplayMessageWindow(ALERT_GEM, 1f));
            return;
        }

        // 3) ��ȭ ó��
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);
        GlobalValue.Instance.ElapseSkillLevelByEnum(selectedAsset.OUTER, selectedAsset.INNER, selectedAsset.MAXLV, 1);

        GlobalValue.Instance._Inven.SKGEM_CNT -= selectedAsset.COST;
        GlobalValue.Instance.SaveInven();

        // 4) UI ����
        UpdateDetailUI(selectedAsset);
        SkillWindowController.Inst.UpdateContentUI();
    }

    #endregion

    #region Helpers / GET

    /// <summary>
    /// ���� ����/���� ���� ��ġ�� ����(��/%)�� ���� �����Ͽ� �� �ؽ�Ʈ�� �����մϴ�.
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

        // ���� 0�� ��Ƽ��/���潺 ��ų�� ���簪 0 ǥ��
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
            : $"{main.NAME} <size=150%><color=#ffff00>{mainPreText}</color></size>���� <size=150%><color=#00ff00>{mainPostText}</color></size>";

        string subText = "";
        if (!string.IsNullOrEmpty(sub.NAME))
        {
            subText = showAsFinal
                ? $"<br>{sub.NAME} <size=150%><color=#00ff00>{subPreText}</color></size>"
                : $"<br>{sub.NAME} <size=150%><color=#ffff00>{subPreText}</color></size>���� <size=150%><color=#00ff00>{subPostText}</color></size>";
        }

        return mainText + subText;
    }

    /// <summary>
    /// �� ������ ��/�ۼ�Ʈ/��ġ�� �����մϴ�.
    /// </summary>
    private string ConcatStringByFormat(float value, SkillSO.ValueType format)
    {
        switch (format)
        {
            case SkillSO.ValueType.Sec: return $"{value}��";
            case SkillSO.ValueType.Rate: return $"{(value * 100f).ToString("F1")}%";
            default: return $"{value}";
        }
    }

    /// <summary>
    /// ��ų ������ ����Ű ����(Q/E) �����ڷ� ��ȯ�մϴ�.
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
