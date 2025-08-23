using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
//using UnityEditor.UI;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GearController;
using static UnityEngine.Rendering.VolumeComponent;

public class GearWindowController : MonoBehaviour
{
    private const string GEAR_SO_PRELINK = "ScriptableObjects/Gear/";
    private const string GEAR_SO_POSTLINK = "Asset";

    public static GearWindowController Inst;

    public enum GearLocation
    { Inven, Equip, Count/*Length*/}

    [Header("HEADER")]
    [SerializeField] private Button button_Back;
    //[SerializeField] private Text text_Gem;

    [Header("BODY")]
    [SerializeField] private ScrollRect scrollRect_Inven;
    [SerializeField] private GameObject[] filterButtons;
    [SerializeField] private InvenDropZone dropZone_Inven;
    [SerializeField] private GameObject panel_Empty;
    [Space(10)]
    [SerializeField] private EquipDropZone dropZone_Equip;
    public GameObject[] gearAbilities;
    public List<Button>[,] gearButtons_Inven;
    public Button[,] gearButtons_Equip;

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Detail;

    private GearType selectedFilterType = GearType.Count;
    private GearSO selectedAsset;
    private bool isInitialized;
    private bool isGearButtonsInitialized;

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

        InitHeader();
        InitBody();

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
    }

    private void InitBody()
    {
        InitVariable();
        StartCoroutine(InitAllEquipUI());
        StartCoroutine(InitAllInvenUI());
    }

    private void InitVariable()
    {
        if (!isGearButtonsInitialized)
        {
            InitGearButtons();
            isGearButtonsInitialized = true;
        }

        gearAbilities = ResourceUtility.SortByEnum
                        <
                        GearAbilitySorter,
                        GearSO.StatType
                        >
                        (gearAbilities, e => e.Type);

        scrollRect_Inven.verticalNormalizedPosition = 1f;
    }

    void InitGearButtons()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        gearButtons_Inven = new List<Button>[OUTER_LEN, INNER_LEN];
        gearButtons_Equip = new Button[OUTER_LEN, INNER_LEN];

        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                gearButtons_Inven[outer, inner] = new List<Button>();
                gearButtons_Equip[outer, inner] = null;
            }
        }
    }

    /// <summary>
    /// GlobalValue.cs에 저장된 착용 장비 버튼을
    /// 슬롯에 초기화 하는 메서드 입니다.
    /// 씬 이동 전에 한 번만 호출합니다.
    /// </summary>
    /// <returns></returns>
    IEnumerator InitAllEquipUI()
    {
        yield return new WaitUntil(() => isGearButtonsInitialized);

        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;
        // For finding first-displayed equipped gear.
        KeyValuePair<int, int> gearID
            = new KeyValuePair<int, int>(OUTER_LEN, INNER_LEN);

        //--- Init all equipped gear buttons.
        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                GearController.Rarity Outer = (GearController.Rarity)outer;
                GearController.GearType Inner = (GearController.GearType)inner;

                if (GlobalValue.Instance.GetGearCountByEnum(
                    Outer,
                    Inner,
                    GearLocation.Equip) > 0)
                {
                    AddGearButton(Outer, Inner, GearLocation.Equip);

                    // Display gear by order(Hat-> Sweater-> Gloves-> Sneakers).
                    if (inner < gearID.Value)
                        gearID = new KeyValuePair<int, int>(outer, inner);
                }
            }
        }

        DisplayEquipButtons();
        //---

        //--- Init all equipped gear ability texts.
        DisplayEquipAbilities();
        ResetPreviewTextColor();
        //---

        //--- Init detail window.
        if (gearID.Key != OUTER_LEN
            && gearID.Value != INNER_LEN)
        {
            GearController.Rarity Outer = (GearController.Rarity)gearID.Key;
            GearController.GearType Inner = (GearController.GearType)gearID.Value;

            GearDetailWindowController.Inst.UpdateDetailUI(GetAsset(Outer, Inner));
        }
        //---
    }

    /// <summary>
    /// 장비 스탯 창을 초기화 하는 메서드 입니다.
    /// </summary>
    private void DisplayEquipAbilities()
    {
        for (int index = 0; index < (int)GearSO.StatType.Count; index++)
        {
            float value = GlobalValue.Instance.GetEquipStatByEnum(
                        (GearSO.StatType)index);

            gearAbilities[(int)index].transform.GetChild(1)
                .GetComponent<Text>().text
                = ConcatFormatByType(value, (GearSO.StatType)index);
        }
    }

    /// <summary>
    /// 현재 표시되는 착용 장비 버튼 배열 gearButtons_Equip
    /// 멤버들의 화면 상 위치를 조정하는 메서드 입니다.
    /// </summary>
    private void DisplayEquipButtons()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                if (gearButtons_Equip[outer, inner])
                {
                    //dropZone_Equip.EquipGearButton(
                    //    gearButtons_Equip[outer, inner]);
                    dropZone_Equip.SetGearButtonPosition(
                        gearButtons_Equip[outer, inner]);
                }
            }
        }
    }

    /// <summary>
    /// 인벤토리 또는 장비 창에 장비 버튼을 생성하여 추가하는 메서드 입니다.
    /// 초기화 시 사용합니다.
    /// </summary>
    /// <param name="outer">장비 버튼 이중 배열의 1차 인덱스</param>
    /// <param name="inner">장비 버튼 이중 배열의 2차 인덱스</param>
    /// <param name="location">장비 버튼이 추가되는 위치</param>
    public Button AddGearButton
        (
        GearController.Rarity outer,
        GearController.GearType inner,
        GearLocation location
        )
    {
        Button button = GetGearButton(GetAsset(outer, inner), location);

        switch (location)
        {
            case GearLocation.Inven:
                gearButtons_Inven[(int)outer, (int)inner].Add(button);
                break;
            case GearLocation.Equip:
                gearButtons_Equip[(int)outer, (int)inner] = button;
                break;
            default:
                break;
        }

        return button;
    }

    /// <summary>
    /// 인벤토리 또는 장비 창에 장비 버튼을 옮겨서 추가하는 메서드 입니다.
    /// 드래그 앤 드롭 시 사용합니다.
    /// </summary>
    /// <param name="gear">장비 버튼</param>
    /// <param name="location">장비 버튼이 추가되는 위치</param>
    public void AddGearButton
        (
        Button gearButton,
        GearLocation location
        )
    {
        GearController gear = gearButton.GetComponent<GearController>();
        GearController.Rarity outer = gear.Asset.OUTER;
        GearController.GearType inner = gear.Asset.INNER;

        switch (location)
        {
            case GearLocation.Inven:
                gearButtons_Inven[(int)outer, (int)inner].Add(gearButton);
                break;
            case GearLocation.Equip:
                gearButtons_Equip[(int)outer, (int)inner] = gearButton;
                break;
            default:
                break;
        }
    }

    public void RemoveGearButton
        (
        Button gearButton,
        GearLocation location
        )
    {
        GearController gear = gearButton.GetComponent<GearController>();
        GearController.Rarity outer = gear.Asset.OUTER;
        GearController.GearType inner = gear.Asset.INNER;

        switch (location)
        {
            case GearLocation.Inven:
                gearButtons_Inven[(int)outer, (int)inner].Remove(gearButton);
                break;
            case GearLocation.Equip:
                gearButtons_Equip[(int)outer, (int)inner] = null;
                break;
            default:
                break;
        }
    }

    IEnumerator InitAllInvenUI()
    {
        yield return new WaitUntil(() => isGearButtonsInitialized);

        //Add filter buttons' listener.
        AddFilterButtonListener();

        //Init collected gears.
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;
        int count = 0;

        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                GearController.Rarity Outer = (GearController.Rarity)outer;
                GearController.GearType Inner = (GearController.GearType)inner;

                if ((count = GlobalValue.Instance.GetGearCountByEnum(
                    Outer,
                    Inner,
                    GearLocation.Inven)) > 0)
                {
                    while ((count--) > 0)
                    {
                        AddGearButton(Outer, Inner, GearLocation.Inven);
                    }
                }
            }
        }

        FilterButtonOnClick(0);
    }

    private void AddFilterButtonListener()
    {
        ResourceUtility.SortByEnum<GearFilterButtonController, GearType>
            (filterButtons, e => e.Sort());

        int LEN = (int)GearType.Count + 1;
        for (int index = 0; index < LEN; index++)
        {
            int capturedIndex = index;
            filterButtons[capturedIndex].GetComponent<Button>().onClick.AddListener(
                () => FilterButtonOnClick(capturedIndex));
        }
    }

    private void FilterButtonOnClick(int index)
    {
        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        int LEN = (int)GearType.Count + 1;
        int sortedIndex = (index + LEN - 1) % LEN;

        //1. Set clicked button to selected.
        foreach (var item in filterButtons)
        {
            item.GetComponent<GearFilterButtonController>().Select(false);
        }
        filterButtons[index].GetComponent<GearFilterButtonController>()
            .Select(true);
        //2. Set selectedFilterType.
        selectedFilterType = (GearType)sortedIndex;
        //3. Rearrange gear buttons in inven.
        DisplayInvenButtons();
    }

    public void DisplayInvenButtons()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;
        int count = 0;
        int siblingIndex = 0;

        List<Button> filteredButtons = new List<Button>();
        List<Button> otherButtons = new List<Button>();

        //for (int outer = 0; outer < OUTER_LEN; outer++)
        for (int outer = OUTER_LEN - 1; outer >= 0; outer--)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                count += gearButtons_Inven[outer, inner].Count;

                foreach (var item in gearButtons_Inven[outer, inner])
                {
                    if (item == null) continue;

                    //bool isTarget = (inner == (int)selectedFilterType);
                    //item.interactable = isTarget;
                    //dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);

                    //1. Divide buttons into 2 groups by selected filtering type.
                    if (selectedFilterType != GearType.Count)
                    {
                        if (inner == (int)selectedFilterType)
                            filteredButtons.Add(item);
                        else
                            otherButtons.Add(item);
                    }
                    else
                    {
                        item.gameObject.SetActive(true);
                        dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
                    }
                }
            }
        }

        //1. Do work with filtered gears.
        //   Activate interaction + arrange upper.
        if (filteredButtons.Count > 0)
            foreach (var item in filteredButtons)
            {
                //item.interactable = true;
                item.gameObject.SetActive(true);
                dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
            }

        //1. Do work with filtered gears.
        //   Deactivate interaction + arrange lower.
        if (otherButtons.Count > 0)
            foreach (var item in otherButtons)
            {
                //item.interactable = false;
                item.gameObject.SetActive(false);
                dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
            }

        if (count == 0) panel_Empty.SetActive(true);
        else panel_Empty.SetActive(false);
    }

    public void GearButtonOnClick(GearSO asset)
    {
        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);
        //1. Preview gear ability changes by texts.
        ResetPreviewTextColor();
        PreviewGearAbilities(asset);
        //2. Update detail window ui.
        UpdateDetailUI(asset);
    }

    /// <summary>
    /// 장비 교체로 인한 스탯 변화를 텍스트 컬러로 표시하는 메서드 입니다.
    /// </summary>
    /// <param name="asset"></param>
    public void PreviewGearAbilities
        (
        GearSO asset,
        GearLocation from = GearLocation.Inven
        )
    {
        Button selectedButton;
        GearController selectedGear;

        if (from == GearLocation.Inven)
        {
            int inner = (int)asset.INNER;
            for (int outer = 0; outer < (int)GearController.Rarity.Count; outer++)
            {
                if (!(selectedButton = gearButtons_Equip[outer, inner]))
                    continue;

                selectedGear = selectedButton.GetComponent<GearController>();
                UpdatePreviewTextColor(asset, selectedGear.Asset, from);
                return;
            }

            UpdatePreviewTextColor(asset, null, from);
        }
        else
        {
            UpdatePreviewTextColor(asset, null, from);
        }
    }

    private void UpdatePreviewTextColor
        (
        GearSO self,
        GearSO equipped = null,
        GearLocation from = GearLocation.Inven
        )
    {
        GearSO.GearStat selfAbility;
        GearSO.GearStat equippedAbility;

        if (from == GearLocation.Inven)
        {
            if (!equipped)
            {
                // main
                selfAbility = self.MainStat;
                SetPreviewTextColor(selfAbility.TYPE, true);

                // sub
                selfAbility = self.SubStat;
                SetPreviewTextColor(selfAbility.TYPE, true);
            }
            else
            {
                // main
                selfAbility = self.MainStat;
                equippedAbility = equipped.MainStat;

                if (selfAbility.BASE > equippedAbility.BASE)
                    SetPreviewTextColor(selfAbility.TYPE, true);
                else if (selfAbility.BASE < equippedAbility.BASE)
                    SetPreviewTextColor(selfAbility.TYPE, false);
                else
                {
                    if (selfAbility.TYPE != GearSO.StatType.Count)
                        gearAbilities[(int)selfAbility.TYPE].transform.GetChild(1)
                            .GetComponent<Text>().color = Color.yellow;
                }

                // sub
                selfAbility = self.SubStat;
                equippedAbility = equipped.SubStat;

                if (selfAbility.BASE > equippedAbility.BASE)
                    SetPreviewTextColor(selfAbility.TYPE, true);
                else if (selfAbility.BASE < equippedAbility.BASE)
                    SetPreviewTextColor(selfAbility.TYPE, false);
                else
                {
                    if (selfAbility.TYPE != GearSO.StatType.Count)
                        gearAbilities[(int)selfAbility.TYPE].transform.GetChild(1)
                            .GetComponent<Text>().color = Color.yellow;
                }
            }
        }
        else
        {
            // main
            selfAbility = self.MainStat;
            SetPreviewTextColor(selfAbility.TYPE, false);

            // sub
            selfAbility = self.SubStat;
            SetPreviewTextColor(selfAbility.TYPE, false);
        }
    }

    public void SetPreviewTextColor
        (
        GearSO.StatType type,
        bool upgrade
        )
    {
        if (!IsValidStatType(type))
        {
            Debug.LogWarning($"[GearWindowController] Invalid StatType: {type}");
            return;
        }

        int index = (int)type;
        var textComponent = gearAbilities[index].transform
                            .GetChild(1).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError($"Missing Text component in gearAbilities[{index}]");
            return;
        }

        textComponent.color = upgrade ? Color.green : Color.red;
    }

    private bool IsValidStatType(GearSO.StatType statType)
    {
        return statType >= GearSO.StatType.HP && statType < GearSO.StatType.Count;
    }

    public void ResetPreviewTextColor()
    {
        for (int i = 0; i < gearAbilities.Length; i++)
        {
            gearAbilities[i].transform.GetChild(1)
                    .GetComponent<Text>().color = Color.white;
        }
    }

    /// <summary>
    /// 선택한 장비와 같은 타입의 착용 중인 
    /// 장비 세부 사항을 출력하는 메서드 입니다.
    /// </summary>
    /// <param name="asset">장비 관련 스크립터블 오브젝트</param>
    public void UpdateDetailUI(GearSO asset)
    {
        Button equippedButton = null;
        GearSO equippedAsset = null;
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int inner = (int)asset.INNER;

        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            if (equippedButton = gearButtons_Equip[outer, inner])
            {
                equippedAsset = equippedButton.GetComponent<GearController>().Asset;
                break;
            }
        }

        GearDetailWindowController.Inst.UpdateDetailUI(equippedAsset);

        ////--- Init detail window.
        //if (gearID.Key != OUTER_LEN
        //    && gearID.Value != INNER_LEN)
        //{
        //    GearController.Rarity Outer = (GearController.Rarity)gearID.Key;
        //    GearController.GearType Inner = (GearController.GearType)gearID.Value;

        //    GearDetailWindowController.Inst.UpdateDetailUI(GetAsset(Outer, Inner));
        //}
    }

    /// <summary>
    /// 선택한 장비에 따른 스탯 변화를 내부 데이터에 반영하는 메서드 입니다.
    /// </summary>
    /// <param name="asset">장비 관련 스크립터블 오브젝트</param>
    public void UpdateEquipStat
        (
        GearSO asset,
        GlobalValue.GearCommand command
        )
    {
        GearSO.GearStat ability;

        switch (command)
        {
            case GlobalValue.GearCommand.Equip:
                // Main stat.
                ability = asset.MainStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(
                    ability.TYPE,
                    ability.BASE);
                // Sub stat.
                ability = asset.SubStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(
                    ability.TYPE,
                    ability.BASE);
                break;
            case GlobalValue.GearCommand.Unequip:
                // Main stat.
                ability = asset.MainStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(
                    ability.TYPE,
                    -ability.BASE);
                // Sub stat.
                ability = asset.SubStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(
                    ability.TYPE,
                    -ability.BASE);
                break;
            default:
                break;
        }

        DisplayEquipAbilities();
    }

    #region GET
    public static GearSO GetAsset
        (
        GearController.Rarity rarity
        , GearController.GearType gType
        )
        => ResourceUtility.GetResourceByType<GearSO>(
            GetAssetPath(rarity, gType));

    private static string GetAssetPath
        (
        GearController.Rarity rarity
        , GearController.GearType gType
        )
        => GEAR_SO_PRELINK + rarity.ToString()
        + gType.ToString() + GEAR_SO_POSTLINK;

    public GearSO SelectedAsset => selectedAsset;

    private bool IsGearEquipOnDisplay
        (
        GearController.Rarity outer
        , GearController.GearType inner
        )
        => gearButtons_Equip[(int)outer, (int)inner] != null;

    private Button GetGearButton
        (
        GearSO asset,
        GearLocation location
        )
    {
        Button button = Instantiate(
                        asset.BUTTON,
                        GetComponentInParent<Canvas>().transform);
        button.GetComponent<GearController>().Init(asset, location);
        button.transform.Find("Icon_Gear").GetComponent<Image>().sprite = asset.ICON;
        button.onClick.AddListener(
            () => GearButtonOnClick(button.GetComponent<GearController>().Asset));

        return button;
    }

    private string ConcatFormatByType
        (
        float value,
        GearSO.StatType type
        )
    {
        string result = "";

        switch (type)
        {
            case GearSO.StatType.HP:
            case GearSO.StatType.DEF:
            case GearSO.StatType.MATK:
            case GearSO.StatType.SATK:
                result = $"{Mathf.FloorToInt(value)}";
                break;
            case GearSO.StatType.CTKR:
            case GearSO.StatType.EVDR:
                result = $"{(value * 100).ToString("F1")}%";
                break;
            default:
                break;
        }

        return result;
    }
    #endregion
}
