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
    public GameObject[] gearAbilities;       // �ɷ�ġ ���� UI(StatType ��)
    public List<Button>[,] gearButtons_Inven; // �κ��丮 ��ư �׸���
    public Button[,] gearButtons_Equip;       // ���� ��ư �׸���(1ĭ)

    [Header("SUB WINDOW")]
    [SerializeField] private GameObject window_Detail;

    private GearController.GearType selectedFilterType = GearController.GearType.Count;
    private GearSO selectedAsset;
    private bool isInitialized;
    private bool isGearButtonsInitialized;

    /// <summary>
    /// ���� ���� �� �ʱ�ȭ(1ȸ).
    /// </summary>
    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
    }

    /// <summary>
    /// �۷ι� ������ �ε� ��� �� ���/�ٵ� �ʱ�ȭ.
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
    /// ��� ��ư �� ��� UI �ʱ�ȭ.
    /// </summary>
    private void InitHeader()
    {
        button_Back.onClick.AddListener(() =>
        {
            // SFX: Ŭ��
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// ���� ����/��ư �迭 �غ� �� ����/�κ� UI ����.
    /// </summary>
    private void InitBody()
    {
        InitVariable();
        StartCoroutine(InitAllEquipUI());
        StartCoroutine(InitAllInvenUI());
    }

    /// <summary>
    /// ��ư �׸���/���� ����/��ũ�� �ʱ�ȭ.
    /// </summary>
    private void InitVariable()
    {
        if (!isGearButtonsInitialized)
        {
            InitGearButtons();
            isGearButtonsInitialized = true;
        }

        // �ɷ�ġ ���� UI�� StatType ������ ����
        gearAbilities = ResourceUtility.SortByEnum<GearAbilitySorter, GearSO.StatType>(gearAbilities, e => e.Type);

        // �κ� ó������ ��ũ��
        scrollRect_Inven.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// �κ�/���� ��ư �׸���(��ޡ�����) ����.
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
    /// ����� ���� ��� ���Կ� �����ϰ� �ɷ�ġ/��â ����.
    /// </summary>
    private IEnumerator InitAllEquipUI()
    {
        yield return new WaitUntil(() => isGearButtonsInitialized);

        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        // ù ǥ�ÿ�(���� �켱) �ĺ�
        KeyValuePair<int, int> gearID = new KeyValuePair<int, int>(OUTER_LEN, INNER_LEN);

        // ���� ��� ��ư ����
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

        DisplayEquipButtons();     // ���� ��ġ ����
        DisplayEquipAbilities();   // �ɷ�ġ �ؽ�Ʈ ����
        ResetPreviewTextColor();   // �̸����� ���� �ʱ�ȭ

        // ��â �ʱ�ȭ(ù �ĺ��� ������)
        if (gearID.Key != OUTER_LEN && gearID.Value != INNER_LEN)
        {
            var Outer = (GearController.Rarity)gearID.Key;
            var Inner = (GearController.GearType)gearID.Value;
            GearDetailWindowController.Inst.UpdateDetailUI(GetAsset(Outer, Inner));
        }
    }

    /// <summary>
    /// ���� �ɷ�ġ ���� �ؽ�Ʈ ����.
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
    /// ���� ���� ��ư���� ���� ��ġ�� ���ġ.
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
    /// (�ʱ�ȭ��) ���/���� ���� ��� ��ư �ν��Ͻ� ���� �� ���� ��ġ�� ���.
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
    /// (�巡�� �̵���) ��ư ��ü�� �޾� �κ�/���� �׸��忡 �߰�.
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
    /// �׸��忡�� ��ư�� ����(�κ�: ����Ʈ ����, ����: ���� ���).
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
    /// �κ��丮 UI ����(���� ������ �߰�, ���� ��� �ε�).
    /// </summary>
    private IEnumerator InitAllInvenUI()
    {
        yield return new WaitUntil(() => isGearButtonsInitialized);

        AddFilterButtonListener(); // ���� ��ư ������

        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        // ���� ��� �κ� �׸��忡 ä��
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

        // �⺻ ���� ����(ù ��ư)
        FilterButtonOnClick(0);
    }

    /// <summary>
    /// ���� ��ư�� Ÿ�� ������ ���� �� Ŭ�� ������ ����.
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
    /// ���� ��ư Ŭ�� ó��: ���� ǥ��/���� Ÿ�� ����/�κ� ���ġ.
    /// </summary>
    private void FilterButtonOnClick(int index)
    {
        // SFX: Ŭ��
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        int LEN = (int)GearController.GearType.Count + 1;
        int sortedIndex = (index + LEN - 1) % LEN;

        // 1) ��ư ���� ǥ��
        foreach (var go in filterButtons)
            go.GetComponent<GearFilterButtonController>().Select(false);
        filterButtons[index].GetComponent<GearFilterButtonController>().Select(true);

        // 2) ���� Ÿ�� ����(Count ���� ��ȯ)
        selectedFilterType = (GearController.GearType)sortedIndex;

        // 3) �κ� ��ư ���ġ
        DisplayInvenButtons();
    }

    /// <summary>
    /// �κ��丮 ��ư�� ���� �������� ��� ����/���ü� ����.
    /// </summary>
    public void DisplayInvenButtons()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        int count = 0;
        int siblingIndex = 0;

        List<Button> filteredButtons = new List<Button>();
        List<Button> otherButtons = new List<Button>();

        // ��� ���� ����(���Ƽ ���� ��)
        for (int outer = OUTER_LEN - 1; outer >= 0; outer--)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                count += gearButtons_Inven[outer, inner].Count;

                foreach (var item in gearButtons_Inven[outer, inner])
                {
                    if (item == null) continue;

                    // 1) ���͸� �׷� �и�
                    if (selectedFilterType != GearController.GearType.Count)
                    {
                        if (inner == (int)selectedFilterType) filteredButtons.Add(item);
                        else otherButtons.Add(item);
                    }
                    else
                    {
                        // ���� ������: ���� Ȱ�� + ���� ��ġ
                        item.gameObject.SetActive(true);
                        dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
                    }
                }
            }
        }

        // 2) ���� ���: ��� ��ġ + Ȱ��
        if (filteredButtons.Count > 0)
        {
            foreach (var item in filteredButtons)
            {
                item.gameObject.SetActive(true);
                dropZone_Inven.SetGearButtonPosition(item, siblingIndex++);
            }
        }

        // 3) �� ��: �ϴ� ��ġ + ��Ȱ��(���ü���)
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
    /// �κ� ��ư Ŭ��: �ɷ�ġ �̸�����/��â ����.
    /// </summary>
    public void GearButtonOnClick(GearSO asset)
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
        ResetPreviewTextColor();
        PreviewGearAbilities(asset);
        UpdateDetailUI(asset);
    }

    /// <summary>
    /// ��� ��ü �� ���� �ɷ�ġ ��ȭ�� �ؽ�Ʈ ������ ǥ��.
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
    /// �̸����� ���� ���� ����(���: ����, ����: ����, ���: ����).
    /// </summary>
    private void UpdatePreviewTextColor(GearSO self, GearSO equipped = null, GearLocation from = GearLocation.Inven)
    {
        GearSO.GearStat selfAbility;
        GearSO.GearStat equippedAbility;

        if (from == GearLocation.Inven)
        {
            if (!equipped)
            {
                // main/sub �ű� ���� �� ���(�ʷ�)
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
            // ���� ���� �̸����� �� �϶�(����)
            selfAbility = self.MainStat; SetPreviewTextColor(selfAbility.TYPE, false);
            selfAbility = self.SubStat; SetPreviewTextColor(selfAbility.TYPE, false);
        }
    }

    /// <summary>
    /// �ɷ�ġ ���� �ؽ�Ʈ �� ����(�ʷ�/����).
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
    /// ��ȿ�� StatType(HP~Count-1) ���� Ȯ��.
    /// </summary>
    private bool IsValidStatType(GearSO.StatType statType)
    {
        return statType >= GearSO.StatType.HP && statType < GearSO.StatType.Count;
    }

    /// <summary>
    /// ��� �ɷ�ġ ���� �ؽ�Ʈ ���� �⺻��(���)���� ����.
    /// </summary>
    public void ResetPreviewTextColor()
    {
        for (int i = 0; i < gearAbilities.Length; i++)
        {
            gearAbilities[i].transform.GetChild(1).GetComponent<Text>().color = Color.white;
        }
    }

    /// <summary>
    /// ������ ��� Ÿ�԰� ������ ������ �ߡ� ��� ��â�� ǥ��.
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
    /// ���� �ɷ�ġ ������ ��� ������ �ݿ�(����/����).
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
    /// ������ ��ư�� ������ �ʱ� ����(������/Ŭ�� �̺�Ʈ/��Ÿ) �� ��ȯ.
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
    /// StatType�� ǥ�� ���� ����(��ġ/�ۼ�Ʈ).
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
