using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatCountWindowController : MonoBehaviour
{
    private const string NOTICE_CONFIRM = "일괄 강화했습니다.";

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
    private int selectedLevel;      // 강화 후 레벨
    private int selectedCount = 1;  // 강화 횟수

    void Start()
    {
        InitListener();
    }

    private void InitListener()
    {
        //HEADER
        buttonArea_Close.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });
        button_Close.onClick.AddListener(() =>
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });

        //BODY
        slider_Count.onValueChanged.AddListener(CountSliderValueOnChange);

        //FOOT
        button_Confirm.onClick.AddListener(()
            => ConfirmButtonOnClick());
    }

    private void ConfirmButtonOnClick()
    {
        //Play SFX: Level up.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Confirm);

        //--- LEVEL UP!!
        //1. Increase stat level.
        GlobalValue.Instance.ElapseStatLevelByType(
            selectedType
            , selectedAsset.MAXLV
            , selectedCount);

        //2. Decrease GEM.
        GlobalValue.Instance._Inven.STGEM_CNT
            -= selectedAsset.COST * selectedCount;
        GlobalValue.Instance.SaveInven();

        //3. Update UI.
        StatDetailWindowController.Inst.UpdateDetailUIByType(selectedType);
        StatWindowController.Inst
            .UpdateContentUIByType(selectedType, selectedAsset.MAXLV);
        //---

        //Close window.
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        Init();
        InitBody();
        InitFoot();
    }

    private void Init()
    {
        selectedAsset = StatDetailWindowController.Inst.SelectedAsset;
        selectedType = selectedAsset.TYPE;
        selectedLevel = GlobalValue.Instance
            .GetStatLevelByType(selectedType);
        selectedCount = 1;
    }

    private void InitFoot()
    {
        button_Confirm.GetComponentInChildren<Text>().text
            = $"{selectedCount}회 강화";
    }

    private void InitBody()
    {
        image_Icon.sprite = selectedAsset.ICON;
        text_Name.text = selectedAsset.NAME;
        text_Level.text = $"<color=#00ff00><size=125%>" +
            $"{(selectedLevel + selectedCount).ToString("00")}</size></color>" +
            $" /{selectedAsset.MAXLV}";

        InitCountSlider();
    }

    private void InitCountSlider()
    {
        int minLevel = selectedLevel + selectedCount;
        int possibleCount = Mathf.FloorToInt(
                            GlobalValue.Instance._Inven.STGEM_CNT
                            / selectedAsset.COST);
        int maxLevel = Mathf.Clamp(
                        selectedLevel + possibleCount
                        , minLevel
                        , selectedAsset.MAXLV);

        slider_Count.minValue = minLevel;
        slider_Count.maxValue = maxLevel;
        slider_Count.value = slider_Count.minValue;
    }

    #region EVENT
    private void CountSliderValueOnChange(float value)
    {
        //Play SFX: Slider.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Slider);

        //1. Turn quantity to int.
        selectedCount = (int)slider_Count.value - selectedLevel;

        //2. Change text.
        text_Level.text = $"<color=#00ff00><size=125%>" +
            $"{(selectedLevel + selectedCount).ToString("00")}</size></color>" +
            $" /{selectedAsset.MAXLV}";

        button_Confirm.GetComponentInChildren<Text>().text
            = $"{selectedCount}회 강화";
    }
    #endregion
}
