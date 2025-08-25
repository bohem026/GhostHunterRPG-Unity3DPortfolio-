using UnityEngine;

/// <summary>
/// 입력 장치(마우스/터치)를 하나의 포인터 인터페이스로 추상화한 헬퍼.
/// - 포인터 위치/Down/Held/Up 상태를 플랫폼별로 통일해 제공합니다.
/// </summary>
public static class InputExtension
{
    /// <summary>
    /// 현재 포인터(마우스/첫 번째 터치) 화면 좌표를 반환합니다.
    /// 터치가 없으면 Vector2.zero.
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
    /// 포인터가 막 눌린 프레임인지 여부(마우스 좌클릭/터치 Began).
    /// </summary>
    public static bool PointerDown =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButtonDown(0);
#else
        Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif

    /// <summary>
    /// 포인터가 눌린 채 유지 중인지 여부(드래그 포함).
    /// </summary>
    public static bool PointerHeld =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButton(0);
#else
        Input.touchCount > 0 &&
        (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary);
#endif

    /// <summary>
    /// 포인터가 떼어진 프레임인지 여부(마우스 좌클릭 Up/터치 Ended).
    /// </summary>
    public static bool PointerUp =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButtonUp(0);
#else
        Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
#endif
}
