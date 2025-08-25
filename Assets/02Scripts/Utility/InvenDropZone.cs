using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// �κ��丮 ��� ���� ��Ʈ�ѷ�.
/// - �巡�� �� ������� ��� �κ����� �ǵ����ų�(����) �������մϴ�.
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
    /// �巡�� �� ��� �� ȣ��˴ϴ�.
    /// - �κ����� ���: ����Ʈ ������
    /// - ���â���� ���: ��� ����
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
                // Ŭ�� ȿ���� �� �κ� ���ġ
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
                GearWindowController.Inst.DisplayInvenButtons();
                break;

            case GearWindowController.GearLocation.Equip:
                // Ŭ�� ȿ���� �� ����
                AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);
                UnequipGearButton(dropped.GetComponent<Button>());
                break;
        }
    }

    /// <summary>
    /// ��� �����ϰ� �κ����� �̵��մϴ�.
    /// </summary>
    public void UnequipGearButton(Button button)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        // ��ġ/���� ����
        button.transform.SetParent(parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>()); // ��ũ�Ѻ� �׸��� ���� ����
        gear.Location = GearWindowController.GearLocation.Inven;

        // �ɷ�ġ �� UI ����
        GearWindowController.Inst.UpdateEquipStat(gear.Asset, GlobalValue.GearCommand.Unequip);
        GearDetailWindowController.Inst.EnableGearDetailWindow(false);

        // �۷ι� ������ ����
        GlobalValue.Instance.ElapseGearCountByEnum(gear.Asset.OUTER, gear.Asset.INNER, GlobalValue.GearCommand.Unequip);
        GearWindowController.Inst.RemoveGearButton(button, GearWindowController.GearLocation.Equip);
        GearWindowController.Inst.AddGearButton(button, GearWindowController.GearLocation.Inven);

        // �κ� ���ġ �� �̸����� �÷� �ʱ�ȭ
        GearWindowController.Inst.DisplayInvenButtons();
        GearWindowController.Inst.ResetPreviewTextColor();
    }

    /// <summary>
    /// ��ũ��Ʈ���� �κ� �� ��ư ��ġ�� ���ġ�մϴ�.
    /// </summary>
    public void SetGearButtonPosition(Button button, int siblingIndex)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[!!ERROR!!] THIS IS NOT A GEAR BUTTON");
            return;
        }

        // ��ġ/���� ����
        button.transform.SetParent(parent);
        button.transform.SetSiblingIndex(siblingIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>()); // ��ũ�Ѻ� �׸��� ���� ����
        gear.Location = GearWindowController.GearLocation.Inven;

        // �̸����� �÷� �ʱ�ȭ
        GearWindowController.Inst.ResetPreviewTextColor();
    }
}
