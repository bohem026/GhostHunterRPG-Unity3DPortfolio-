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
        // 드롭 감지를 위해 Raycast 막음
        _CanvasGroup.blocksRaycasts = false;

        // 드래그 중 스크롤 막기
        foreach (var rect in scrollRects)
        {
            rect.enabled = false;
        }

        // 드래그 따라 이동하는 UI 보이도록 캔버스 최상위 자식으로 변경
        transform.SetParent(_Canvas.transform);

        //--- UI 변경
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
        // 드래그 따라 UI 이동
        _Rect.anchoredPosition += eventData.delta / _Canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 끝나면 다시 레이캐스트 허용
        _CanvasGroup.blocksRaycasts = true;

        // 드래그 끝나면 스크롤 활성화
        foreach (var rect in scrollRects)
        {
            rect.enabled = true;
        }

        // 드롭 대상 판단
        // 드롭 지점에 IDropHandler가 붙어 있다면 유효
        bool droppedOnValidTarget = false;
        if (eventData.pointerEnter != null)
        {
            if (eventData.pointerEnter.GetComponentInParent<IDropHandler>() != null)
            {
                droppedOnValidTarget = true;
            }
        }

        // 드롭에 실패했을 경우 원래 위치로 복귀
        if (!droppedOnValidTarget)
        {
            //Play SFX: Click.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnceByType
            (AudioPlayerPoolManager.SFXType.Click);

            transform.SetParent(originalParent);
            transform.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            //스크롤뷰의 그리드 레이아웃 그룹 리빌드
            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);

            /*Test*/
            GearWindowController.Inst.ResetPreviewTextColor();
            /*Test*/
        }
    }
}
