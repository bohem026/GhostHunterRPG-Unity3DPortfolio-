using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GearWindowController : MonoBehaviour
{
    private const string GEAR_SO_PRELINK = "ScriptableObjects/Gear/";
    private const string GEAR_SO_POSTLINK = "Asset";

    public static GearWindowController Inst;

    public enum GearLocation { Inven, Equip, Count /*Length*/ }

    [Header("HEADER")]
    [SerializeField] private Button button_Back;

    [Header("BODY")]
    [SerializeField] private ScrollRect scrollRect_Inven;
    [SerializeField] private GameObject[] filterButtons;
    [SerializeField] private InvenDropZone dropZone_Inven;
    [SerializeField] private GameObject panel_Empty;
    [Space(10)]
    [SerializeField] private EquipDropZone dropZone_Equip;
    public GameObject[] gearAbilities;       // 능력치 라인 UI(StatType 순)
    public List<Button>[,] gearButtons_Inven; // 인벤토리 버튼 그리드
    public Button[,] gearButtons_Equip;       // 착용 버튼 그리드(1칸)

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Detail;

    private GearController.GearType selectedFilterType = GearController.GearType.Count;
    private GearSO selectedAsset;
    private bool isInitialized;
    private bool isGearButtonsInitialized;

    /// <summary>
    /// 최초 진입 시 초기화(1회).
    /// </summary>
    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
    }

    /// <summary>
    /// 글로벌 데이터 로드 대기 → 헤더/바디 초기화.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);

        Inst = this;

        InitHeader();
        InitBody();

        isInitialized = true;
    }

    /// <summary>
    /// 상단 버튼 등 헤더 UI 초기화.
    /// </summary>
    private void InitHeader()
    {
        button_Back.onClick.AddListener(() =>
        {
            // SFX: 클릭
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// 내부 변수/버튼 배열 준비 → 착용/인벤 UI 구축.
    /// </summary>
    private void InitBody()
    {
        InitVariable();
        StartCoroutine(InitAllEquipUI());
        StartCoroutine(InitAllInvenUI());
    }

    /// <summary>
    /// 버튼 그리드/정렬 기준/스크롤 초기화.
    /// </summary>
    private void InitVariable()
    {
        if (!isGearButtonsInitialized)
        {
            InitGearButtons();
            isGearButtonsInitialized = true;
        }

        // 능력치 라인 UI를 StatType 순으로 정렬
        gearAbilities = ResourceUtility.SortByEnum<GearAbilitySorter, GearSO.StatType>(gearAbilities, e => e.Type);

        // 인벤 처음으로 스크롤
        scrollRect_Inven.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// 인벤/착용 버튼 그리드(등급×종류) 생성.
    /// </summary>
    private void InitGearButtons()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        gearButtons_Inven = new List<Button>[OUTER_LEN, INNER_LEN];
        gearButtons_Equip = new Button[OUTER_LEN, INNER_LEN];

        for (int o = 0; o < OUTER_LEN; o++)
        {
            for (int i = 0; i < INNER_LEN; i++)
            {
                gearButtons_Inven[o, i] = new List<Button>();
                gearButtons_Equip[o, i] = null;
            }
        }
    }

    /// <summary>
    /// 저장된 착용 장비를 슬롯에 복원하고 능력치/상세창 갱신.
    /// </summary>
    private IEnumerator InitAllEquipUI()
    {
        yield return new WaitUntil(() => isGearButtonsInitialized);

        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        // 첫 표시용(종류 우선) 후보
        KeyValuePair<int, int> gearID = new KeyValuePair<int, int>(OUTER_LEN, INNER_LEN);

        // 착용 장비 버튼 복원
        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                var Outer = (GearController.Rarity)outer;
                var Inner = (GearController.GearType)inner;

                if (GlobalValue.Instance.GetGearCountByEnum(Outer, Inner, GearLocation.Equip) > 0)
                {
                    AddGearButton(Outer, Inner, GearLocation.Equip);
                    if (inner < gearID.Value) gearID = new KeyValuePair<int, int>(outer, inner);
                }
            }
        }

        DisplayEquipButtons();     // 슬롯 위치 정렬
        DisplayEquipAbilities();   // 능력치 텍스트 갱신
        ResetPreviewTextColor();   // 미리보기 색상 초기화

        // 상세창 초기화(첫 후보가 있으면)
        if (gearID.Key != OUTER_LEN && gearID.Value != INNER_LEN)
        {
            var Outer = (GearController.Rarity)gearID.Key;
            var Inner = (GearController.GearType)gearID.Value;
            GearDetailWindowController.Inst.UpdateDetailUI(GetAsset(Outer, Inner));
        }
    }

    /// <summary>
    /// 착용 능력치 라인 텍스트 갱신.
    /// </summary>
    private void DisplayEquipAbilities()
    {
        for (int idx = 0; idx < (int)GearSO.StatType.Count; idx++)
        {
            float value = GlobalValue.Instance.GetEquipStatByEnum((GearSO.StatType)idx);
            gearAbilities[idx].transform.GetChild(1).GetComponent<Text>().text =
                ConcatFormatByType(value, (GearSO.StatType)idx);
        }
    }

    /// <summary>
    /// 현재 착용 버튼들을 슬롯 위치에 재배치.
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
                    dropZone_Equip.SetGearButtonPosition(gearButtons_Equip[outer, inner]);
                }
            }
        }
    }

    /// <summary>
    /// (초기화용) 등급/종류 기준 장비 버튼 인스턴스 생성 후 지정 위치에 등록.
    /// </summary>
    public Button AddGearButton(GearController.Rarity outer, GearController.GearType inner, GearLocation location)
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
        }

        return button;
    }

    /// <summary>
    /// (드래그 이동용) 버튼 자체를 받아 인벤/착용 그리드에 추가.
    /// </summary>
    public void AddGearButton(Button gearButton, GearLocation location)
    {
        var gear = gearButton.GetComponent<GearController>();
        var outer = gear.Asset.OUTER;
        var inner = gear.Asset.INNER;

        switch (location)
        {
            case GearLocation.Inven:
                gearButtons_Inven[(int)outer, (int)inner].Add(gearButton);
                break;
            case GearLocation.Equip:
                gearButtons_Equip[(int)outer, (int)inner] = gearButton;
                break;
        }
    }

    /// <summary>
    /// 그리드에서 버튼을 제거(인벤: 리스트 제거, 착용: 슬롯 비움).
    /// </summary>
    public void RemoveGearButton(Button gearButton, GearLocation location)
    {
        var gear = gearButton.GetComponent<GearController>();
        var outer = gear.Asset.OUTER;
        var inner = gear.Asset.INNER;

        switch (location)
        {
            case GearLocation.Inven:
                gearButtons_Inven[(int)outer, (int)inner].Remove(gearButton);
                break;
            case GearLocation.Equip:
                gearButtons_Equip[(int)outer, (int)inner] = null;
                break;
        }
    }

    /// <summary>
    /// 인벤토리 UI 구성(필터 리스너 추가, 수집 장비 로드).
    /// </summary>
    private IEnumerator InitAllInvenUI()
    {
        yield return new WaitUntil(() => isGearButtonsInitialized);

        AddFilterButtonListener(); // 필터 버튼 리스너

        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        // 수집 장비를 인벤 그리드에 채움
        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                var Outer = (GearController.Rarity)outer;
                var Inner = (GearController.GearType)inner;

                int count = GlobalValue.Instance.GetGearCountByEnum(Outer, Inner, GearLocation.Inven);
                while (count-- > 0)
                {
                    AddGearButton(Outer, Inner, GearLocation.Inven);
                }
            }
        }

        // 기본 필터 적용(첫 버튼)
        FilterButtonOnClick(0);
    }

    /// <summary>
    /// 필터 버튼을 타입 순으로 정렬 후 클릭 리스너 부착.
    /// </summary>
    private void AddFilterButtonListener()
    {
        ResourceUtility.SortByEnum<GearFilterButtonController, GearController.GearType>(filterButtons, e => e.Sort());

        int LEN = (int)GearController.GearType.Count + 1;
        for (int i = 0; i < LEN; i++)
        {
            int captured = i;
            filterButtons[captured].GetComponent<Button>().onClick.AddListener(
                () => FilterButtonOnClick(captured));
        }
    }

    /// <summary>
    /// 필터 버튼 클릭 처리: 선택 표시/선택 타입 갱신/인벤 재배치.
    /// </summary>
    private void FilterButtonOnClick(int index)
    {
        // SFX: 클릭
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        int LEN = (int)GearController.GearType.Count + 1;
        int sortedIndex = (index + LEN - 1) % LEN;

        // 1) 버튼 선택 표기
        foreach (var go in filterButtons)
            go.GetComponent<GearFilterButtonController>().Select(false);
        filterButtons[index].GetComponent<GearFilterButtonController>().Select(true);

        // 2) 필터 타입 지정(Count 포함 순환)
        selectedFilterType = (GearController.GearType)sortedIndex;

        // 3) 인벤 버튼 재배치
        DisplayInvenButtons();
    }

    /// <summary>
    /// 인벤토리 버튼을 필터 기준으로 상단 정렬/가시성 갱신.
    /// </summary>
    public void DisplayInvenButtons()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        int count = 0;
        int siblingIndex = 0;

        List<Button> filteredButtons = new List<Button>();
        List<Button> otherButtons = new List<Button>();

        // 등급 역순 정렬(레어리티 높은 순)
        for (int outer = OUTER_LEN - 1; outer >= 0; outer--)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                count += gearButtons_Inven[outer, inner].Count;

                foreach (var item in gearButtons_Inven[outer, inner])
                {
                    if (item == null) continue;

                    // 1) 필터링 그룹 분리
                    if (selectedFilterType != GearController.GearType.Count)
                    {
                        if (inner == (int)selectedFilterType) filteredButtons.Add(item);
                        else otherButtons.Add(item);
                    }
                    else
                    {
                        // 필터 미적용: 전부 활성 + 순차 배치
                        item.gameObject.SetActive(true);
                        dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
                    }
                }
            }
        }

        // 2) 필터 대상: 상단 배치 + 활성
        if (filteredButtons.Count > 0)
        {
            foreach (var item in filteredButtons)
            {
                item.gameObject.SetActive(true);
                dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
            }
        }

        // 3) 그 외: 하단 배치 + 비활성(가시성만)
        if (otherButtons.Count > 0)
        {
            foreach (var item in otherButtons)
            {
                item.gameObject.SetActive(false);
                dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
            }
        }

        panel_Empty.SetActive(count == 0);
    }

    /// <summary>
    /// 인벤 버튼 클릭: 능력치 미리보기/상세창 갱신.
    /// </summary>
    public void GearButtonOnClick(GearSO asset)
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
        ResetPreviewTextColor();
        PreviewGearAbilities(asset);
        UpdateDetailUI(asset);
    }

    /// <summary>
    /// 장비 교체 시 예상 능력치 변화를 텍스트 색으로 표시.
    /// </summary>
    public void PreviewGearAbilities(GearSO asset, GearLocation from = GearLocation.Inven)
    {
        Button selectedButton;
        GearController selectedGear;

        if (from == GearLocation.Inven)
        {
            int inner = (int)asset.INNER;
            for (int outer = 0; outer < (int)GearController.Rarity.Count; outer++)
            {
                if (!(selectedButton = gearButtons_Equip[outer, inner])) continue;

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

    /// <summary>
    /// 미리보기 색상 결정 로직(녹색: 증가, 빨강: 감소, 노랑: 동일).
    /// </summary>
    private void UpdatePreviewTextColor(GearSO self, GearSO equipped = null, GearLocation from = GearLocation.Inven)
    {
        GearSO.GearStat selfAbility;
        GearSO.GearStat equippedAbility;

        if (from == GearLocation.Inven)
        {
            if (!equipped)
            {
                // main/sub 신규 장착 → 상승(초록)
                selfAbility = self.MainStat; SetPreviewTextColor(selfAbility.TYPE, true);
                selfAbility = self.SubStat; SetPreviewTextColor(selfAbility.TYPE, true);
            }
            else
            {
                // main
                selfAbility = self.MainStat;
                equippedAbility = equipped.MainStat;
                if (selfAbility.BASE > equippedAbility.BASE) SetPreviewTextColor(selfAbility.TYPE, true);
                else if (selfAbility.BASE < equippedAbility.BASE) SetPreviewTextColor(selfAbility.TYPE, false);
                else if (selfAbility.TYPE != GearSO.StatType.Count)
                    gearAbilities[(int)selfAbility.TYPE].transform.GetChild(1).GetComponent<Text>().color = Color.yellow;

                // sub
                selfAbility = self.SubStat;
                equippedAbility = equipped.SubStat;
                if (selfAbility.BASE > equippedAbility.BASE) SetPreviewTextColor(selfAbility.TYPE, true);
                else if (selfAbility.BASE < equippedAbility.BASE) SetPreviewTextColor(selfAbility.TYPE, false);
                else if (selfAbility.TYPE != GearSO.StatType.Count)
                    gearAbilities[(int)selfAbility.TYPE].transform.GetChild(1).GetComponent<Text>().color = Color.yellow;
            }
        }
        else
        {
            // 착용 해제 미리보기 → 하락(빨강)
            selfAbility = self.MainStat; SetPreviewTextColor(selfAbility.TYPE, false);
            selfAbility = self.SubStat; SetPreviewTextColor(selfAbility.TYPE, false);
        }
    }

    /// <summary>
    /// 능력치 라인 텍스트 색 지정(초록/빨강).
    /// </summary>
    public void SetPreviewTextColor(GearSO.StatType type, bool upgrade)
    {
        if (!IsValidStatType(type))
        {
            Debug.LogWarning($"[GearWindowController] Invalid StatType: {type}");
            return;
        }

        int index = (int)type;
        var textComponent = gearAbilities[index].transform.GetChild(1).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError($"Missing Text component in gearAbilities[{index}]");
            return;
        }

        textComponent.color = upgrade ? Color.green : Color.red;
    }

    /// <summary>
    /// 유효한 StatType(HP~Count-1) 범위 확인.
    /// </summary>
    private bool IsValidStatType(GearSO.StatType statType)
    {
        return statType >= GearSO.StatType.HP && statType < GearSO.StatType.Count;
    }

    /// <summary>
    /// 모든 능력치 라인 텍스트 색을 기본값(흰색)으로 복구.
    /// </summary>
    public void ResetPreviewTextColor()
    {
        for (int i = 0; i < gearAbilities.Length; i++)
        {
            gearAbilities[i].transform.GetChild(1).GetComponent<Text>().color = Color.white;
        }
    }

    /// <summary>
    /// 선택한 장비 타입과 동일한 ‘착용 중’ 장비를 상세창에 표시.
    /// </summary>
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
    }

    /// <summary>
    /// 실제 능력치 누적에 장비 변경을 반영(장착/해제).
    /// </summary>
    public void UpdateEquipStat(GearSO asset, GlobalValue.GearCommand command)
    {
        GearSO.GearStat ability;

        switch (command)
        {
            case GlobalValue.GearCommand.Equip:
                ability = asset.MainStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(ability.TYPE, ability.BASE);
                ability = asset.SubStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(ability.TYPE, ability.BASE);
                break;

            case GlobalValue.GearCommand.Unequip:
                ability = asset.MainStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(ability.TYPE, -ability.BASE);
                ability = asset.SubStat;
                GlobalValue.Instance.ElapseEquipStatByEnum(ability.TYPE, -ability.BASE);
                break;
        }

        DisplayEquipAbilities();
    }

    #region GET / Helpers

    public static GearSO GetAsset(GearController.Rarity rarity, GearController.GearType gType)
        => ResourceUtility.GetResourceByType<GearSO>(GetAssetPath(rarity, gType));

    private static string GetAssetPath(GearController.Rarity rarity, GearController.GearType gType)
        => GEAR_SO_PRELINK + rarity.ToString() + gType.ToString() + GEAR_SO_POSTLINK;

    public GearSO SelectedAsset => selectedAsset;

    private bool IsGearEquipOnDisplay(GearController.Rarity outer, GearController.GearType inner)
        => gearButtons_Equip[(int)outer, (int)inner] != null;

    /// <summary>
    /// 프리팹 버튼을 생성해 초기 세팅(아이콘/클릭 이벤트/메타) 후 반환.
    /// </summary>
    private Button GetGearButton(GearSO asset, GearLocation location)
    {
        Button button = Instantiate(asset.BUTTON, GetComponentInParent<Canvas>().transform);
        button.GetComponent<GearController>().Init(asset, location);
        button.transform.Find("Icon_Gear").GetComponent<Image>().sprite = asset.ICON;
        button.onClick.AddListener(() => GearButtonOnClick(button.GetComponent<GearController>().Asset));
        return button;
    }

    /// <summary>
    /// StatType별 표시 포맷 구성(수치/퍼센트).
    /// </summary>
    private string ConcatFormatByType(float value, GearSO.StatType type)
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
        }
        return result;
    }

    #endregion
}
