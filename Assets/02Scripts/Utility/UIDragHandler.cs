using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// �κ�/��� UI ��ư�� �巡�� �� ��� ó��:
/// - �巡�� ���� �� ĵ���� �ֻ����� �ø��� ��ũ�� ��Ȱ��ȭ
/// - �巡�� �� ��ġ ����
/// - ��� ���� ��ȿ�� �˻�, ���� �� ����ġ ����
/// - �巡�� �� ��� ������/�� UI ����
/// </summary>
public class UIDragHandler : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    // Components
    private RectTransform _Rect;
    private Canvas _Canvas;
    private CanvasGroup _CanvasGroup;

    // State
    private Transform originalParent;
    private ScrollRect[] scrollRects;

    private void Awake()
    {
        _Rect = GetComponent<RectTransform>();
        _Canvas = GetComponentInParent<Canvas>();
        if (_CanvasGroup == null) _CanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // �巡�� �� ��ũ�� ������ ���� ĵ���� �� ��� ScrollRect ĳ��
        scrollRects = _Canvas.transform.GetComponentsInChildren<ScrollRect>();
    }

    /// <summary>
    /// �巡�� ����: ����ĳ��Ʈ ���� ����, ��ũ�� ��Ȱ��, ĵ���� �ֻ����� �̵�, ������ ����.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        originalParent = transform.parent;
        _CanvasGroup.blocksRaycasts = false;

        foreach (var rect in scrollRects) rect.enabled = false;

        // �巡�� �� UI�� �������� �ʵ��� ĵ���� �ֻ����� �̵�
        transform.SetParent(_Canvas.transform);

        // ������/�� UI ����
        var gearCtrl = GetComponent<GearController>();
        var asset = gearCtrl.Asset;
        var location = gearCtrl.Location;

        GearWindowController.Inst.ResetPreviewTextColor();
        GearWindowController.Inst.PreviewGearAbilities(asset, location);
        GearWindowController.Inst.UpdateDetailUI(asset);
    }

    /// <summary>
    /// �巡�� ��: �Է� ��Ÿ��ŭ ��ġ �̵�.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _Rect.anchoredPosition += eventData.delta / _Canvas.scaleFactor;
    }

    /// <summary>
    /// �巡�� ����: ����ĳ��Ʈ ����, ��ũ�� ����, ��� ��ȿ�� �˻� �� ���� �� ����ġ ����.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        _CanvasGroup.blocksRaycasts = true;
        foreach (var rect in scrollRects) rect.enabled = true;

        bool droppedOnValidTarget =
            eventData.pointerEnter != null &&
            eventData.pointerEnter.GetComponentInParent<IDropHandler>() != null;

        if (!droppedOnValidTarget)
        {
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

            transform.SetParent(originalParent);
            _Rect.anchoredPosition = Vector2.zero;

            // ��ũ�Ѻ� �׸��� ���̾ƿ� ���� ������(��ġ ����)
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);

            GearWindowController.Inst.ResetPreviewTextColor();
        }
    }
}
