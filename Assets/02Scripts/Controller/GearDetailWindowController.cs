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
    /// 최초 활성화 시 초기화(비동기) 또는 재진입 시 UI 리셋.
    /// </summary>
    private void OnEnable()
    {
        if (!isInitialized)
            StartCoroutine(Init());
        else
            ResetDetailUI();
    }

    /// <summary>
    /// 글로벌 데이터 로드를 기다렸다가 참조 캐싱 및 최초 UI 갱신.
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
    /// 하위 트랜스폼/컴포넌트(아이콘/텍스트) 캐싱.
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
    /// 현재 착용 중인 장비 중 가장 앞 인덱스(종류 우선) 하나를 찾아 상세 UI를 갱신.
    /// </summary>
    private void ResetDetailUI()
    {
        int OUTER_LEN = (int)GearController.Rarity.Count;
        int INNER_LEN = (int)GearController.GearType.Count;

        // 최초로 표시할 착용 장비를 찾기 위한 (등급, 종류) 쌍
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
    /// 전달된 장비 데이터로 상세창 UI를 갱신(없으면 빈 패널 표시).
    /// </summary>
    internal void UpdateDetailUI(GearSO gearAsset = null)
    {
        // 동일 타입 착용 장비가 없을 때
        if (!gearAsset)
        {
            EnableGearDetailWindow(false);
        }
        else
        {
            EnableGearDetailWindow(true);

            // 1) 기존 아이콘 슬롯 정리
            int COUNT = slot_Icon.transform.childCount;
            if (COUNT > 0)
            {
                for (int i = 0; i < COUNT; i++)
                    Destroy(slot_Icon.transform.GetChild(i).gameObject);
            }

            // 2) 아이콘 버튼 인스턴스(클릭/드래그 비활성)
            Button gearButton = Instantiate(gearAsset.BUTTON, slot_Icon.transform);
            DeactivateButtonClick(gearButton);
            gearButton.transform.Find("Icon_Gear").GetComponent<Image>().sprite = gearAsset.ICON;

            // 3) 이름/색상
            text_Name.text = $"[착용 중] {gearAsset.NAME}";
            SetNameTextColor(gearAsset.OUTER);

            // 4) 소개
            text_Intro.text = gearAsset.INTRO;
        }
    }

    /// <summary>
    /// 등급별 이름 텍스트 색상 지정.
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
    /// 상세창 내 장비 버튼의 클릭/드래그를 비활성화하고 색상 유지.
    /// </summary>
    private void DeactivateButtonClick(Button button)
    {
        // 1) 비활성 색상 흰색 유지
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white;
        button.colors = colors;
        // 2) 클릭 비활성
        button.interactable = false;
        // 3) 드래그 비활성
        Destroy(button.GetComponent<UIDragHandler>());
    }

    /// <summary>
    /// 장비 상세창/빈 패널 표시 전환.
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
