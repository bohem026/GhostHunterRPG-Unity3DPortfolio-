using UnityEngine;

public class UIUtility : MonoBehaviour
{
    /// <summary>
    /// 주어진 UI(RectTransform)가 현재 카메라 뷰포트에서 차지하는
    /// 가로/세로 비율을 계산합니다.
    /// </summary>
    /// <param name="uiRect">대상 UI의 RectTransform</param>
    /// <returns>가로, 세로 비율(0~1)의 Vector2</returns>
    public static Vector2 UIRectToViewportRatio(RectTransform uiRect)
    {
        // UI의 월드 기준 실제 폭/높이
        float uiWorldWidth = uiRect.rect.width * uiRect.lossyScale.x;
        float uiWorldHeight = uiRect.rect.height * uiRect.lossyScale.y;

        // 카메라와 UI 사이 거리
        float distance = Vector3.Distance(Camera.main.transform.position, uiRect.position);

        // 해당 거리에서의 카메라 투영 평면 크기
        float frustumHeight = 2f * distance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * Camera.main.aspect;

        // 뷰포트 대비 UI 비율(가로/세로)
        float hrWidthRatio = uiWorldWidth / frustumWidth;
        float hrHeightRatio = uiWorldHeight / frustumHeight;

        return new Vector2(hrWidthRatio, hrHeightRatio);
    }
}
