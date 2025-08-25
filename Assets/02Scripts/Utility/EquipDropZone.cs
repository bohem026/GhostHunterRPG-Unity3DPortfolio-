using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장비 드롭존: 인벤토리에서 드래그 앤 드롭으로 장착/재배치를 처리합니다.
/// </summary>
public class EquipDropZone : MonoBehaviour, IDropHandler
{
    [Header("SLOT")]
    [SerializeField] private RectTransform[] gearSlots;

    /// <summary>
    /// 드래그한 장비 버튼이 드롭되었을 때 호출됩니다.
    /// 인벤에서 오면 장착, 장비창에서 오면 위치 재배치만 수행합니다.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (!dropped) return;

        GearController droppedGear = dropped.GetComponent<GearController>();
        if (!droppedGear) return;

        switch (droppedGear.Location)
        {
            case GearWindowController.GearLocation.Inven:
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Confirm);
                EquipGearButton(dropped.GetComponent<Button>());
                break;

            case GearWindowController.GearLocation.Equip:
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
                SetGearButtonPosition(dropped.GetComponent<Button>());
                break;
        }
    }

    /// <summary>
    /// 인벤토리 장비 버튼을 장비 슬롯에 장착합니다.
    /// 같은 타입이 이미 장착돼 있으면 해제 후 인벤토리로 되돌리고, 능력치/UI를 갱신합니다.
    /// </summary>
    public void EquipGearButton(Button button)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[EquipDropZone] Not a gear button.");
            return;
        }

        // 같은 타입 장비가 이미 장착되어 있으면 전부 해제(슬롯 내 0번은 베이스 UI 가정)
        int count = gearSlots[(int)gear.Asset.INNER].childCount;
        if (count > 1)
        {
            for (int i = 1; i < count; i++)
            {
                Button equippedButton = gearSlots[(int)gear.Asset.INNER].GetChild(i).GetComponent<Button>();
                GearController equippedGear = equippedButton.GetComponent<GearController>();

                // 인벤토리로 이동 및 리스트 갱신
                equippedGear.Location = GearWindowController.GearLocation.Inven;
                GearWindowController.Inst.RemoveGearButton(equippedButton, GearWindowController.GearLocation.Equip);
                GearWindowController.Inst.AddGearButton(equippedButton, GearWindowController.GearLocation.Inven);
                GearWindowController.Inst.DisplayInvenButtons();

                // 장착 해제 능력치 반영
                GearWindowController.Inst.UpdateEquipStat(equippedGear.Asset, GlobalValue.GearCommand.Unequip);
            }
        }

        // 신규 장착
        button.transform.SetParent(gearSlots[(int)gear.Asset.INNER]);
        button.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        gear.Location = GearWindowController.GearLocation.Equip;

        // 능력치/상세 UI 갱신
        GearWindowController.Inst.UpdateEquipStat(gear.Asset, GlobalValue.GearCommand.Equip);
        GearDetailWindowController.Inst.UpdateDetailUI(gear.Asset);

        // 글로벌 보유 현황/리스트/미리보기 색상 초기화
        GlobalValue.Instance.ElapseGearCountByEnum(gear.Asset.OUTER, gear.Asset.INNER, GlobalValue.GearCommand.Equip);
        GearWindowController.Inst.RemoveGearButton(button, GearWindowController.GearLocation.Inven);
        GearWindowController.Inst.AddGearButton(button, GearWindowController.GearLocation.Equip);
        GearWindowController.Inst.ResetPreviewTextColor();
    }

    /// <summary>
    /// 이미 장비창에 있는 버튼을 해당 슬롯 위치로 정렬합니다(재장착 아님).
    /// </summary>
    public void SetGearButtonPosition(Button button)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[EquipDropZone] Not a gear button.");
            return;
        }

        button.transform.SetParent(gearSlots[(int)gear.Asset.INNER]);
        button.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        gear.Location = GearWindowController.GearLocation.Equip;

        GearWindowController.Inst.ResetPreviewTextColor();
    }
}
