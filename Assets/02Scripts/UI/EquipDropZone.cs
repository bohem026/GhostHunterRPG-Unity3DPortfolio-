using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipDropZone : MonoBehaviour, IDropHandler
{
    [Header("ROOT")]
    [SerializeField] private Transform parent;

    [Header("SLOT")]
    [SerializeField] private RectTransform[] gearSlots;

    /// <summary>
    /// 드래그 앤 드랍으로 장비를 장착하는 이벤트 함수입니다.
    /// </summary>
    /// <param name="eventData">드랍 이벤트</param>
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (!dropped) return;
        GearController droppedGear = dropped.GetComponent<GearController>();
        if (!droppedGear) return;

        GearWindowController.GearLocation preLocation = droppedGear.Location;
        switch (preLocation)
        {
            case GearWindowController.GearLocation.Inven:
                //Play SFX: Click.
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
                (AudioPlayerPoolManager.SFXType.Confirm);
                EquipGearButton(dropped.GetComponent<Button>());
                break;
            case GearWindowController.GearLocation.Equip:
                //Play SFX: Click.
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
                (AudioPlayerPoolManager.SFXType.Click);
                SetGearButtonPosition(dropped.GetComponent<Button>());
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 장비를 장착하는 함수입니다.
    /// </summary>
    /// <param name="button">대상</param>
    public void EquipGearButton(Button button)
    {
        GearController gear;
        if (!(gear = button.GetComponent<GearController>()))
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        //--- Remove same type of gears from equip.
        int gearCount = 0;
        if ((gearCount = gearSlots[(int)gear.Asset.INNER].childCount) > 1)
        {
            for (int i = 1; i < gearCount; i++)
            {
                Button equippedButton = gearSlots[(int)gear.Asset.INNER]
                    .GetChild(i)
                    .GetComponent<Button>();
                GearController equippedGear
                    = equippedButton.GetComponent<GearController>();

                //1. Adjust position.
                equippedGear.Location = GearWindowController.GearLocation.Inven;
                //2. Remove same type of gear from equip.
                GearWindowController.Inst.RemoveGearButton(
                    equippedButton,
                    GearWindowController.GearLocation.Equip);
                //3. Insert to equip gear list.
                GearWindowController.Inst.AddGearButton(
                    equippedButton,
                    GearWindowController.GearLocation.Inven);
                //4. Rearrange inven.
                GearWindowController.Inst.DisplayInvenButtons();
                //5. Adjust equip gear ability.
                GearWindowController.Inst.UpdateEquipStat(
                    equippedGear.Asset,
                    GlobalValue.GearCommand.Unequip);
            }
        }
        //---

        //--- Equip gear.
        //1. Adjust position.
        button.transform.SetParent(gearSlots[(int)gear.Asset.INNER]);
        button.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        gear.Location = GearWindowController.GearLocation.Equip;
        //2. Adjust equip gear ability.
        GearWindowController.Inst.UpdateEquipStat(
            gear.Asset,
            GlobalValue.GearCommand.Equip);
        //3. Update detail UIs.
        GearDetailWindowController.Inst.UpdateDetailUI(gear.Asset);
        //---

        //--- Equip succeed.
        GearController.Rarity Outer = gear.Asset.OUTER;
        GearController.GearType Inner = gear.Asset.INNER;

        //1. Adjust global gear count.
        GlobalValue.Instance.ElapseGearCountByEnum(
            Outer,
            Inner,
            GlobalValue.GearCommand.Equip);
        //2. Remove from inven gear list.
        GearWindowController.Inst.RemoveGearButton(
            button,
            GearWindowController.GearLocation.Inven);
        //3. Insert to equip gear list.
        GearWindowController.Inst.AddGearButton(
            button,
            GearWindowController.GearLocation.Equip);
        //4. Reset UIs.
        GearWindowController.Inst.ResetPreviewTextColor();
        //---
    }

    /// <summary>
    /// 스크립트 상에서 버튼의 위치를 초기화 하는 함수입니다.
    /// </summary>
    /// <param name="button">대상</param>
    public void SetGearButtonPosition(Button button)
    {
        GearController gear;
        if (!(gear = button.GetComponent<GearController>()))
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        // 1. Repositioning.
        button.transform.SetParent(gearSlots[(int)gear.Asset.INNER]);
        button.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        gear.Location = GearWindowController.GearLocation.Equip;
        // 2. Reset UIs.
        GearWindowController.Inst.ResetPreviewTextColor();
    }
}
