using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InvenDropZone : MonoBehaviour, IDropHandler
{
    public static InvenDropZone Inst;

    [Header("ROOT")]
    [SerializeField] private Transform parent;

    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// 드래그 앤 드랍으로 버튼을 배치하는 이벤트 함수입니다.
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
                (AudioPlayerPoolManager.SFXType.Click);
                GearWindowController.Inst.DisplayInvenButtons();
                break;
            case GearWindowController.GearLocation.Equip:
                //Play SFX: Click.
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
                (AudioPlayerPoolManager.SFXType.Click);
                UnequipGearButton(dropped.GetComponent<Button>());
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 장비를 해제하는 함수입니다.
    /// </summary>
    /// <param name="button">대상</param>
    public void UnequipGearButton(Button button)
    {
        GearController gear;
        if (!(gear = button.GetComponent<GearController>()))
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        //--- Unequip gear.
        //1. Adjust position.
        button.transform.SetParent(parent);
        //스크롤뷰 그리드 기능 강제 초기화
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        gear.Location = GearWindowController.GearLocation.Inven;
        //2. Adjust equip gear ability.
        GearWindowController.Inst.UpdateEquipStat(
            gear.Asset,
            GlobalValue.GearCommand.Unequip);
        //3. Update detail UIs.
        GearDetailWindowController.Inst.EnableGearDetailWindow(false);
        //---

        //--- Unequip succeed.
        GearController.Rarity Outer = gear.Asset.OUTER;
        GearController.GearType Inner = gear.Asset.INNER;

        //1. Adjust global gear count.
        GlobalValue.Instance.ElapseGearCountByEnum(
            Outer,
            Inner,
            GlobalValue.GearCommand.Unequip);
        //2. Remove from equip gear list.
        GearWindowController.Inst.RemoveGearButton(
            button,
            GearWindowController.GearLocation.Equip);
        //3. Insert to inven gear list.
        GearWindowController.Inst.AddGearButton(
            button,
            GearWindowController.GearLocation.Inven);
        //4. Rearrange inven.
        GearWindowController.Inst.DisplayInvenButtons();
        //5. Reset UIs.
        GearWindowController.Inst.ResetPreviewTextColor();
        //---
    }

    /// <summary>
    /// 스크립트 상에서 버튼의 위치를 초기화 하는 함수입니다.
    /// </summary>
    /// <param name="button">대상</param>
    public void SetGearButtonPosition
        (
        Button button,
        int siblingIndex
        )
    {
        GearController gear;
        if (!(gear = button.GetComponent<GearController>()))
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        // 1. Repositioning.
        button.transform.SetParent(parent);
        /*자식 순서 변경*/
        button.transform.SetSiblingIndex(siblingIndex);
        //스크롤뷰 그리드 기능 강제 초기화
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        gear.Location = GearWindowController.GearLocation.Inven;
        // 2. Reset UIs.
        GearWindowController.Inst.ResetPreviewTextColor();
    }
}
