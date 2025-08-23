using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class StatDetailWindowController : MonoBehaviour
{
    private const string STAT_SO_PRELINK = "ScriptableObjects/Stat/";
    private const string STAT_SO_POSTLINK = "Asset";
    private const string NOTICE_CONFIRM = " ��ȭ �Ϸ�";
    private const string POSTNOTICE_RESET = "�� ��ȭ ������ �ʱ�ȭ�߽��ϴ�.";
    private const string ALERT_MAX_LEVEL = "�̹� �ְ� �����Դϴ�.";
    private const string ALERT_GEM = "��ȭ�� �����մϴ�.";

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

    //1. Ȱ��ȭ �� ���ù��� ���� ������ �°� Icon�� sprite ��ü
    //2. StatSO ���޹޾� ����� ���� UI�� ���
    //3. ��ȭ ��ư ������ �˾���ų ���� �޼���|| �˸� �޼��� ����(����st)

    private void OnEnable()
    {
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
    }

    private void InitFoot()
    {
        button_Confirm.onClick.AddListener(()
            => ConfirmButtonOnClick());
        button_Reset.onClick.AddListener(()
            => ResetButtonOnClick());
    }

    private void InitConfirmButton()
    {
        if ((GlobalValue.Instance.GetStatLevelByType(selectedType)
                == selectedAsset.MAXLV)
                ||
                (selectedAsset.COST
                > GlobalValue.Instance._Inven.STGEM_CNT))
        {
            ChangeButtonToDisabled(button_Confirm, true);
        }
        else
        {
            ChangeButtonToDisabled(button_Confirm, false);
        }
    }

    private void InitResetButton()
    {
        if (GlobalValue.Instance.GetStatLevelByType(selectedType) > 0)
        {
            button_Reset.interactable = true;
        }
        else
        {
            button_Reset.interactable = false;
        }
    }

    public void UpdateDetailUIByType(StatController.StatType type)
    {
        InitSelected(type);

        if (!selectedAsset)
        {
            Debug.Log("[!!ERROR!!] FAILED TO LOAD ASSET");
            return;
        }

        Image_Icon.sprite = selectedAsset.ICON;
        Text_Name.text = $"[Lv." +
                        $"{GlobalValue.Instance.GetStatLevelByType(type).ToString("00")}] "
                        + selectedAsset.NAME;
        Text_Intro.text = selectedAsset.INTRO;
        Text_Detail.text = GetDetailText(selectedAsset);
        Text_Cost.text = selectedAsset.COST.ToString();

        InitConfirmButton();
        InitResetButton();
    }

    private void InitSelected(StatController.StatType type)
    {
        selectedType = type;
        selectedAsset = ResourceUtility.GetResourceByType<StatSO>
            (GetAssetPath(selectedType));
    }

    #region EVENT
    private void ResetButtonOnClick()
    {
        if (StatWindowController.Inst.IsMessageWindowOnDisplay)
            return;

        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        int currentLevel
            = GlobalValue.Instance.GetStatLevelByType(selectedType);

        //1. Restore GEM.
        GlobalValue.Instance._Inven.STGEM_CNT
            += selectedAsset.COST * currentLevel;

        //2. Reset selected stat level.
        GlobalValue.Instance.ElapseStatLevelByType(
            selectedType
            , selectedAsset.MAXLV
            , -currentLevel);

        //3. Pop up message window.
        StartCoroutine(
                StatWindowController
                .Inst
                .DisplayMessageWindow(
                    $"[{selectedAsset.NAME}]{POSTNOTICE_RESET}"
                    , 1.5f));

        //4. Update UIs.
        UpdateDetailUIByType(selectedType);
        StatWindowController.Inst
            .UpdateContentUIByType(selectedType, selectedAsset.MAXLV);
    }

    private void ConfirmButtonOnClick()
    {
        if (StatWindowController.Inst.IsMessageWindowOnDisplay)
            return;

        //--- Exception care
        //1. Alert: already max level
        if (GlobalValue.Instance.GetStatLevelByType(selectedType)
            == StatController.MAX_STAT_LEVEL)
        {
            //Play SFX: Alert.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(
                StatWindowController
                .Inst
                .DisplayMessageWindow(ALERT_MAX_LEVEL, 1f));

            return;
        }
        //2. Alert: out of GEM
        else if (selectedAsset.COST
            > GlobalValue.Instance._Inven.STGEM_CNT)
        {
            //Play SFX: Alert.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Alert);
            StartCoroutine(StatWindowController.Inst
                .DisplayMessageWindow(ALERT_GEM, 1f));

            return;
        }
        //---

        //--- LEVEL UP!!
        int possibleCount = Mathf.FloorToInt(
                            GlobalValue.Instance._Inven.STGEM_CNT
                            / selectedAsset.COST);
        possibleCount = Mathf.Clamp(
                        possibleCount
                        , 0
                        , selectedAsset.MAXLV
                        - GlobalValue.Instance.GetStatLevelByType(selectedType));

        //2 �̻� ���� �� ������ ��� �ϰ� ���� ��
        if (possibleCount > 1)
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            window_Count.gameObject.SetActive(true);
        }
        //�׷��� ���� ��� ���� ���� ��
        else
        {
            //Play SFX: Level up.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Confirm);

            //1. Increase stat level.
            GlobalValue.Instance.ElapseStatLevelByType(
                selectedType
                , selectedAsset.MAXLV
                , 1);

            //2. Decrease GEM.
            GlobalValue.Instance._Inven.STGEM_CNT -= selectedAsset.COST;
            GlobalValue.Instance.SaveInven();

            //3. Update UI.
            UpdateDetailUIByType(selectedType);
            StatWindowController.Inst
                .UpdateContentUIByType(selectedType, selectedAsset.MAXLV);

            /*Test*/
            Debug.Log($"{selectedType.ToString()}: " +
                $"{GlobalValue.Instance.GetStatLevelByType(selectedType)}");
            /*Test*/
        }
        //---
    }
    #endregion

    private void ChangeButtonToDisabled(Button button, bool command)
    {
        ColorBlock colors = button.colors;

        colors.normalColor = command
            ? new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f)
            : Color.white;
    }

    #region GET
    private string GetDetailText(StatSO asset)
    {
        int LV = GlobalValue.Instance.GetStatLevelByType(asset.TYPE);
        float preValue = asset.BASE + asset.PER * LV;
        float postValue = preValue + asset.PER;

        string textPreValue = "";
        string textPostValue = "";
        switch (asset.FORMAT)
        {
            case StatSO.ValueType.Sec:
                textPreValue = $"{preValue.ToString("F3")}�ʴ� 1";
                textPostValue = $"{postValue.ToString("F3")}�ʴ� 1";
                break;
            case StatSO.ValueType.Rate:
                textPreValue = $"{(preValue * 100f).ToString("F1")}%";
                textPostValue = $"{(postValue * 100f).ToString("F1")}%";
                break;
            default:
                textPreValue = $"{preValue.ToString()}";
                textPostValue = $"{postValue.ToString()}";
                break;
        }

        //string textInc = asset.PER < 0 ? "����" : "����";

        return $"�⺻ {asset.NAME}(��)�� " +
            $"<size=180%><color=#ffff00>{textPreValue}</color></size>���� " +
            $"<size=180%><color=#00ff00>{textPostValue}</color></size>�� " +
            $"����";
        //$"{textInc}";
    }

    private string GetAssetPath(StatController.StatType type)
        => STAT_SO_PRELINK + type.ToString() + STAT_SO_POSTLINK;

    public StatSO SelectedAsset => selectedAsset;
    #endregion
}
