using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform _Rect;
    private Canvas _Canvas;
    private CanvasGroup _CanvasGroup;

    private Transform originalParent;
    private ScrollRect[] scrollRects;

    private void Awake()
    {
        _Rect = GetComponent<RectTransform>();
        _Canvas = GetComponentInParent<Canvas>();
        if (_CanvasGroup == null)
            _CanvasGroup = gameObject.AddComponent<CanvasGroup>();

        scrollRects = _Canvas.transform.GetComponentsInChildren<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Play SFX: Click.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
        (AudioPlayerPoolManager.SFXType.Click);

        originalParent = transform.parent;
        // ��� ������ ���� Raycast ����
        _CanvasGroup.blocksRaycasts = false;

        // �巡�� �� ��ũ�� ����
        foreach (var rect in scrollRects)
        {
            rect.enabled = false;
        }

        // �巡�� ���� �̵��ϴ� UI ���̵��� ĵ���� �ֻ��� �ڽ����� ����
        transform.SetParent(_Canvas.transform);

        //--- UI ����
        GearSO asset = GetComponent<GearController>().Asset;
        GearWindowController.GearLocation location
            = GetComponent<GearController>().Location;

        GearWindowController.Inst.ResetPreviewTextColor();
        GearWindowController.Inst.PreviewGearAbilities(asset, location);
        GearWindowController.Inst.UpdateDetailUI(asset);
        //---
    }

    public void OnDrag(PointerEventData eventData)
    {
        // �巡�� ���� UI �̵�
        _Rect.anchoredPosition += eventData.delta / _Canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // �巡�� ������ �ٽ� ����ĳ��Ʈ ���
        _CanvasGroup.blocksRaycasts = true;

        // �巡�� ������ ��ũ�� Ȱ��ȭ
        foreach (var rect in scrollRects)
        {
            rect.enabled = true;
        }

        // ��� ��� �Ǵ�
        // ��� ������ IDropHandler�� �پ� �ִٸ� ��ȿ
        bool droppedOnValidTarget = false;
        if (eventData.pointerEnter != null)
        {
            if (eventData.pointerEnter.GetComponentInParent<IDropHandler>() != null)
            {
                droppedOnValidTarget = true;
            }
        }

        // ��ӿ� �������� ��� ���� ��ġ�� ����
        if (!droppedOnValidTarget)
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            transform.SetParent(originalParent);
            transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            //��ũ�Ѻ��� �׸��� ���̾ƿ� �׷� ������
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);

            /*Test*/
            GearWindowController.Inst.ResetPreviewTextColor();
            /*Test*/
        }
    }
}
