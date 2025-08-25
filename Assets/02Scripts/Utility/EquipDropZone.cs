using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ��� �����: �κ��丮���� �巡�� �� ������� ����/���ġ�� ó���մϴ�.
/// </summary>
public class EquipDropZone : MonoBehaviour, IDropHandler
{
    [Header("SLOT")]
    [SerializeField] private RectTransform[] gearSlots;

    /// <summary>
    /// �巡���� ��� ��ư�� ��ӵǾ��� �� ȣ��˴ϴ�.
    /// �κ����� ���� ����, ���â���� ���� ��ġ ���ġ�� �����մϴ�.
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
    /// �κ��丮 ��� ��ư�� ��� ���Կ� �����մϴ�.
    /// ���� Ÿ���� �̹� ������ ������ ���� �� �κ��丮�� �ǵ�����, �ɷ�ġ/UI�� �����մϴ�.
    /// </summary>
    public void EquipGearButton(Button button)
    {
        GearController gear = button.GetComponent<GearController>();
        if (!gear)
        {
            Debug.Log("[EquipDropZone] Not a gear button.");
            return;
        }

        // ���� Ÿ�� ��� �̹� �����Ǿ� ������ ���� ����(���� �� 0���� ���̽� UI ����)
        int count = gearSlots[(int)gear.Asset.INNER].childCount;
        if (count > 1)
        {
            for (int i = 1; i < count; i++)
            {
                Button equippedButton = gearSlots[(int)gear.Asset.INNER].GetChild(i).GetComponent<Button>();
                GearController equippedGear = equippedButton.GetComponent<GearController>();

                // �κ��丮�� �̵� �� ����Ʈ ����
                equippedGear.Location = GearWindowController.GearLocation.Inven;
                GearWindowController.Inst.RemoveGearButton(equippedButton, GearWindowController.GearLocation.Equip);
                GearWindowController.Inst.AddGearButton(equippedButton, GearWindowController.GearLocation.Inven);
                GearWindowController.Inst.DisplayInvenButtons();

                // ���� ���� �ɷ�ġ �ݿ�
                GearWindowController.Inst.UpdateEquipStat(equippedGear.Asset, GlobalValue.GearCommand.Unequip);
            }
        }

        // �ű� ����
        button.transform.SetParent(gearSlots[(int)gear.Asset.INNER]);
        button.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        gear.Location = GearWindowController.GearLocation.Equip;

        // �ɷ�ġ/�� UI ����
        GearWindowController.Inst.UpdateEquipStat(gear.Asset, GlobalValue.GearCommand.Equip);
        GearDetailWindowController.Inst.UpdateDetailUI(gear.Asset);

        // �۷ι� ���� ��Ȳ/����Ʈ/�̸����� ���� �ʱ�ȭ
        GlobalValue.Instance.ElapseGearCountByEnum(gear.Asset.OUTER, gear.Asset.INNER, GlobalValue.GearCommand.Equip);
        GearWindowController.Inst.RemoveGearButton(button, GearWindowController.GearLocation.Inven);
        GearWindowController.Inst.AddGearButton(button, GearWindowController.GearLocation.Equip);
        GearWindowController.Inst.ResetPreviewTextColor();
    }

    /// <summary>
    /// �̹� ���â�� �ִ� ��ư�� �ش� ���� ��ġ�� �����մϴ�(������ �ƴ�).
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
