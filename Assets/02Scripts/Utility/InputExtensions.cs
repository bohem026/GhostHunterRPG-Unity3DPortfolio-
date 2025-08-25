using UnityEngine;

/// <summary>
/// �Է� ��ġ(���콺/��ġ)�� �ϳ��� ������ �������̽��� �߻�ȭ�� ����.
/// - ������ ��ġ/Down/Held/Up ���¸� �÷������� ������ �����մϴ�.
/// </summary>
public static class InputExtension
{
    /// <summary>
    /// ���� ������(���콺/ù ��° ��ġ) ȭ�� ��ǥ�� ��ȯ�մϴ�.
    /// ��ġ�� ������ Vector2.zero.
    /// </summary>
    public static Vector2 PointerPosition
    {
        get
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return Input.mousePosition;
#elif UNITY_IOS || UNITY_ANDROID
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            else
                return Vector2.zero;
#else
            return Vector2.zero;
#endif
        }
    }

    /// <summary>
    /// �����Ͱ� �� ���� ���������� ����(���콺 ��Ŭ��/��ġ Began).
    /// </summary>
    public static bool PointerDown =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButtonDown(0);
#else
        Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif

    /// <summary>
    /// �����Ͱ� ���� ä ���� ������ ����(�巡�� ����).
    /// </summary>
    public static bool PointerHeld =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButton(0);
#else
        Input.touchCount > 0 &&
        (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary);
#endif

    /// <summary>
    /// �����Ͱ� ������ ���������� ����(���콺 ��Ŭ�� Up/��ġ Ended).
    /// </summary>
    public static bool PointerUp =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButtonUp(0);
#else
        Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
#endif
}
