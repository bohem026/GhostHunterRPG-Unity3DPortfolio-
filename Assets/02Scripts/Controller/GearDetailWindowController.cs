using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GearDetailWindowController : MonoBehaviour
{
    private const string NAME_ICON = "Slot_Icon";
    private const string NAME_NAME = "Text_Name";
    private const string NAME_INTRO = "Text_Intro";

    public static GearDetailWindowController Inst;

    [SerializeField] private GameObject group_Detail;
    [SerializeField] private GameObject panel_Empty;

    private GameObject slot_Icon;
    private Text text_Name;
    private Text text_Intro;
    private bool isInitialized;

    /// <summary>
    /// ���� Ȱ��ȭ �� �ʱ�ȭ(�񵿱�) �Ǵ� ������ �� UI ����.
    /// </summary>
    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
        else
            ResetDetailUI();
    }

    /// <summary>
    /// �۷ι� ������ �ε带 ��ٷȴٰ� ���� ĳ�� �� ���� UI ����.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GlobalValue.Instance && GlobalValue.Instance.IsDataLoaded);

        Inst = this;

        InitVariables();
        UpdateDetailUI();

        isInitialized = true;
    }

    /// <summary>
    /// ���� Ʈ������/������Ʈ(������/�ؽ�Ʈ) ĳ��.
    /// </summary>
    private void InitVariables()
    {
        if (!slot_Icon)
            slot_Icon = group_Detail.transform.Find(NAME_ICON).gameObject;
        if (!text_Name)
            text_Name = group_Detail.transform.Find(NAME_NAME).GetComponent<Text>();
        if (!text_Intro)
            text_Intro = group_Detail.transform.Find(NAME_INTRO).GetComponent<Text>();
    }

    /// <summary>
    /// ���� ���� ���� ��� �� ���� �� �ε���(���� �켱) �ϳ��� ã�� �� UI�� ����.
    /// </summary>
    private void ResetDetailUI()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        // ���ʷ� ǥ���� ���� ��� ã�� ���� (���, ����) ��
        KeyValuePair<int, int> gearID = new KeyValuePair<int, int>(OUTER_LEN, INNER_LEN);

        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                if (GearWindowController.Inst.gearButtons_Equip[outer, inner])
                {
                    if (inner < gearID.Value)
                        gearID = new KeyValuePair<int, int>(outer, inner);
                }
            }
        }

        if (gearID.Key != OUTER_LEN && gearID.Value != INNER_LEN)
        {
            var Outer = (GearController.Rarity)gearID.Key;
            var Inner = (GearController.GearType)gearID.Value;

            UpdateDetailUI(GearWindowController.GetAsset(Outer, Inner));
        }
    }

    /// <summary>
    /// ���޵� ��� �����ͷ� ��â UI�� ����(������ �� �г� ǥ��).
    /// </summary>
    internal void UpdateDetailUI(GearSO gearAsset = null)
    {
        // ���� Ÿ�� ���� ��� ���� ��
        if (!gearAsset)
        {
            EnableGearDetailWindow(false);
        }
        else
        {
            EnableGearDetailWindow(true);

            // 1) ���� ������ ���� ����
            int COUNT = slot_Icon.transform.childCount;
            if (COUNT > 0)
            {
                for (int i = 0; i < COUNT; i++)
                    Destroy(slot_Icon.transform.GetChild(i).gameObject);
            }

            // 2) ������ ��ư �ν��Ͻ�(Ŭ��/�巡�� ��Ȱ��)
            Button gearButton = Instantiate(gearAsset.BUTTON, slot_Icon.transform);
            DeactivateButtonClick(gearButton);
            gearButton.transform.Find("Icon_Gear").GetComponent<Image>().sprite = gearAsset.ICON;

            // 3) �̸�/����
            text_Name.text = $"[���� ��] {gearAsset.NAME}";
            SetNameTextColor(gearAsset.OUTER);

            // 4) �Ұ�
            text_Intro.text = gearAsset.INTRO;
        }
    }

    /// <summary>
    /// ��޺� �̸� �ؽ�Ʈ ���� ����.
    /// </summary>
    private void SetNameTextColor(GearController.Rarity rarity)
    {
        Color color;
        switch (rarity)
        {
            case GearController.Rarity.Common:
                color = new Color(0.4431373f, 0.8392158f, 0.2980392f, 1f); break;
            case GearController.Rarity.Rare:
                color = new Color(0.8000001f, 0.345098f, 0.9843138f, 1f); break;
            case GearController.Rarity.Unique:
                color = new Color(0.9725491f, 0.8235295f, 0.3372549f, 1f); break;
            case GearController.Rarity.Legendary:
                color = new Color(1f, 0.6588235f, 0.254902f, 1f); break;
            default:
                return;
        }
        text_Name.color = color;
    }

    /// <summary>
    /// ��â �� ��� ��ư�� Ŭ��/�巡�׸� ��Ȱ��ȭ�ϰ� ���� ����.
    /// </summary>
    private void DeactivateButtonClick(Button button)
    {
        // 1) ��Ȱ�� ���� ��� ����
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        // 2) Ŭ�� ��Ȱ��
        button.interactable = false;
        // 3) �巡�� ��Ȱ��
        Destroy(button.GetComponent<UIDragHandler>());
    }

    /// <summary>
    /// ��� ��â/�� �г� ǥ�� ��ȯ.
    /// </summary>
    public void EnableGearDetailWindow(bool command)
    {
        if (command)
        {
            group_Detail.SetActive(true);
            panel_Empty.SetActive(false);
        }
        else
        {
            group_Detail.SetActive(false);
            panel_Empty.SetActive(true);
        }
    }
}
