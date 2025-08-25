using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤/장비 UI 버튼의 드래그 앤 드롭 처리:
/// - 드래그 시작 시 캔버스 최상위로 올리고 스크롤 비활성화
/// - 드래그 중 위치 갱신
/// - 드롭 지점 유효성 검사, 실패 시 원위치 복귀
/// - 드래그 중 장비 프리뷰/상세 UI 갱신
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

        // 드래그 중 스크롤 방지를 위해 캔버스 내 모든 ScrollRect 캐시
        scrollRects = _Canvas.transform.GetComponentsInChildren<ScrollRect>();
    }

    /// <summary>
    /// 드래그 시작: 레이캐스트 차단 해제, 스크롤 비활성, 캔버스 최상위로 이동, 프리뷰 갱신.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType(AudioPlayerPoolManager.SFXType.Click);

        originalParent = transform.parent;
        _CanvasGroup.blocksRaycasts = false;

        foreach (var rect in scrollRects) rect.enabled = false;

        // 드래그 중 UI가 가려지지 않도록 캔버스 최상위로 이동
        transform.SetParent(_Canvas.transform);

        // 프리뷰/상세 UI 갱신
        var gearCtrl = GetComponent<GearController>();
        var asset = gearCtrl.Asset;
        var location = gearCtrl.Location;

        GearWindowController.Inst.ResetPreviewTextColor();
        GearWindowController.Inst.PreviewGearAbilities(asset, location);
        GearWindowController.Inst.UpdateDetailUI(asset);
    }

    /// <summary>
    /// 드래그 중: 입력 델타만큼 위치 이동.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        _Rect.anchoredPosition += eventData.delta / _Canvas.scaleFactor;
    }

    /// <summary>
    /// 드래그 종료: 레이캐스트 복구, 스크롤 복구, 드롭 유효성 검사 후 실패 시 원위치 복귀.
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

            // 스크롤뷰 그리드 레이아웃 강제 리빌드(배치 복구)
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);

            GearWindowController.Inst.ResetPreviewTextColor();
        }
    }
}
