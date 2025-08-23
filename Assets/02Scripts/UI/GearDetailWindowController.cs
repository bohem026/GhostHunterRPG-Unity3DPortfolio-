using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GearWindowController;

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

    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
        else
            ResetDetailUI();
    }


    IEnumerator Init()
    {
        yield return new WaitUntil(()
            => GlobalValue.Instance
            && GlobalValue.Instance.IsDataLoaded);

        Inst = this;

        InitVariables();
        UpdateDetailUI();

        isInitialized = true;
    }

    private void InitVariables()
    {
        if (!slot_Icon)
            slot_Icon = group_Detail.transform.Find(NAME_ICON)
                        .gameObject;
        if (!text_Name)
            text_Name = group_Detail.transform.Find(NAME_NAME)
                        .GetComponent<Text>();
        if (!text_Intro)
            text_Intro = group_Detail.transform.Find(NAME_INTRO)
                        .GetComponent<Text>();
    }

    private void ResetDetailUI()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;
        // For finding first-displayed equipped gear.
        KeyValuePair<int, int> gearID
            = new KeyValuePair<int, int>(OUTER_LEN, INNER_LEN);

        for (int outer = 0; outer < OUTER_LEN; outer++)
        {
            for (int inner = 0; inner < INNER_LEN; inner++)
            {
                if (GearWindowController.Inst
                    .gearButtons_Equip[outer, inner])
                {
                    if (inner < gearID.Value)
                        gearID = new KeyValuePair<int, int>(outer, inner);
                }
            }
        }

        if (gearID.Key != OUTER_LEN
           && gearID.Value != INNER_LEN)
        {
            GearController.Rarity Outer = (GearController.Rarity)gearID.Key;
            GearController.GearType Inner = (GearController.GearType)gearID.Value;

            UpdateDetailUI(GearWindowController.GetAsset(Outer, Inner));
        }
    }

    internal void UpdateDetailUI(GearSO gearAsset = null)
    {
        // If there's no same type of gear equipped.
        if (!gearAsset)
        {
            EnableGearDetailWindow(false);
        }
        // If there's same type of gear equipped.
        else
        {
            EnableGearDetailWindow(true);

            // 1. Remove remained button.
            int COUNT = 0;
            if ((COUNT = slot_Icon.transform.childCount) > 0)
            {
                for (int index = 0; index < COUNT; index++)
                {
                    Destroy(slot_Icon.transform.GetChild(index).gameObject);
                }
            }
            // 2. Display button.
            Button gearButton = Instantiate(gearAsset.BUTTON, slot_Icon.transform);
            DeactivateButtonClick(gearButton);
            gearButton.transform.Find("Icon_Gear")
                .GetComponent<Image>().sprite = gearAsset.ICON;
            // 3. Display gear name.
            text_Name.text = $"[Âø¿ë Áß] {gearAsset.NAME}";
            SetNameTextColor(gearAsset.OUTER);
            // 4. Display gear intro.
            text_Intro.text = gearAsset.INTRO;
        }
    }

    private void SetNameTextColor(GearController.Rarity rarity)
    {
        Color color;

        switch (rarity)
        {
            case GearController.Rarity.Common:
                color = new Color(0.4431373f, 0.8392158f, 0.2980392f, 1f);
                break;
            case GearController.Rarity.Rare:
                color = new Color(0.8000001f, 0.345098f, 0.9843138f, 1f);
                break;
            case GearController.Rarity.Unique:
                color = new Color(0.9725491f, 0.8235295f, 0.3372549f, 1f);
                break;
            case GearController.Rarity.Legendary:
                color = new Color(1f, 0.6588235f, 0.254902f, 1f);
                break;
            default:
                return;
        }

        text_Name.color = color;
    }

    private void DeactivateButtonClick(Button button)
    {
        // 1. Unset disabled color.
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        // 2. Disable click.
        button.interactable = false;
        // 3. Disable drag.
        Destroy(button.GetComponent<UIDragHandler>());
    }

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
