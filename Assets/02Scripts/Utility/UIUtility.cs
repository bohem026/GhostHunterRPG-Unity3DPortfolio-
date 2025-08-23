using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIUtility : MonoBehaviour
{
    /// <summary>
    /// 대상 ui의 현재 화면을 차지하는 w, h 비율을 반환합니다.
    /// </summary>
    /// <param name="uiRect">대상 ui의 RectTransform</param>
    /// <returns>w, h값을 가지는 Vector2</returns>
    public static Vector2 UIRectToViewportRatio(RectTransform uiRect)
    {
        //the world space width of an image.
        float uiWorldWidth = uiRect.rect.width * uiRect.lossyScale.x;
        float uiWorldHeight = uiRect.rect.height * uiRect.lossyScale.y;

        //camera's field of view from where the UI is located.
        float distance = Vector3.Distance(Camera.main.transform.position, uiRect.position);

        //Camera field of view h and w.
        float frustumHeight = 2f * distance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * Camera.main.aspect;

        float hrWidthRatio = uiWorldWidth / frustumWidth;
        float hrHeightRatio = uiWorldHeight / frustumHeight;

        return new Vector2(hrWidthRatio, hrHeightRatio);
    }
}
