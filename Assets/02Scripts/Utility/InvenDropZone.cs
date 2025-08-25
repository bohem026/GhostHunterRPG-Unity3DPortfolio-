using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 드롭 영역 컨트롤러.
/// - 드래그 앤 드롭으로 장비를 인벤으로 되돌리거나(해제) 재정렬합니다.
/// </summary>
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
    /// 드래그 앤 드롭 시 호출됩니다.
    /// - 인벤에서 드롭: 리스트 재정렬
    /// - 장비창에서 드롭: 장비 해제
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
                // 클릭 효과음 및 인벤 재배치
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
                GearWindowController.Inst.DisplayInvenButtons();
                break;

            case GearWindowController.GearLocation.Equip:
                // 클릭 효과음 및 해제
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
                UnequipGearButton(dropped.GetComponent<Button>());
                break;
        }
    }

    /// <summary>
    /// 장비를 해제하고 인벤으로 이동합니다.
    /// </summary>
    public void UnequipGearButton(Button button)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        // 위치/상태 갱신
        button.transform.SetParent(parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>()); // 스크롤뷰 그리드 강제 갱신
        gear.Location = GearWindowController.GearLocation.Inven;

        // 능력치 및 UI 갱신
        GearWindowController.Inst.UpdateEquipStat(gear.Asset, GlobalValue.GearCommand.Unequip);
        GearDetailWindowController.Inst.EnableGearDetailWindow(false);

        // 글로벌 데이터 갱신
        GlobalValue.Instance.ElapseGearCountByEnum(gear.Asset.OUTER, gear.Asset.INNER, GlobalValue.GearCommand.Unequip);
        GearWindowController.Inst.RemoveGearButton(button, GearWindowController.GearLocation.Equip);
        GearWindowController.Inst.AddGearButton(button, GearWindowController.GearLocation.Inven);

        // 인벤 재배치 및 미리보기 컬러 초기화
        GearWindowController.Inst.DisplayInvenButtons();
        GearWindowController.Inst.ResetPreviewTextColor();
    }

    /// <summary>
    /// 스크립트에서 인벤 내 버튼 위치를 재배치합니다.
    /// </summary>
    public void SetGearButtonPosition(Button button, int siblingIndex)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        // 위치/순서 갱신
        button.transform.SetParent(parent);
        button.transform.SetSiblingIndex(siblingIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>()); // 스크롤뷰 그리드 강제 갱신
        gear.Location = GearWindowController.GearLocation.Inven;

        // 미리보기 컬러 초기화
        GearWindowController.Inst.ResetPreviewTextColor();
    }
}
