using UnityEngine;

public class UIUtility : MonoBehaviour
{
    /// <summary>
    /// �־��� UI(RectTransform)�� ���� ī�޶� ����Ʈ���� �����ϴ�
    /// ����/���� ������ ����մϴ�.
    /// </summary>
    /// <param name="uiRect">��� UI�� RectTransform</param>
    /// <returns>����, ���� ����(0~1)�� Vector2</returns>
    public static Vector2 UIRectToViewportRatio(RectTransform uiRect)
    {
        // UI�� ���� ���� ���� ��/����
        float uiWorldWidth = uiRect.rect.width * uiRect.lossyScale.x;
        float uiWorldHeight = uiRect.rect.height * uiRect.lossyScale.y;

        // ī�޶�� UI ���� �Ÿ�
        float distance = Vector3.Distance(Camera.main.transform.position, uiRect.position);

        // �ش� �Ÿ������� ī�޶� ���� ��� ũ��
        float frustumHeight = 2f * distance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * Camera.main.aspect;

        // ����Ʈ ��� UI ����(����/����)
        float hrWidthRatio = uiWorldWidth / frustumWidth;
        float hrHeightRatio = uiWorldHeight / frustumHeight;

        return new Vector2(hrWidthRatio, hrHeightRatio);
    }
}
