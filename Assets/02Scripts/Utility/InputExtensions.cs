using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputExtension
{
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

    public static bool PointerDown =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButtonDown(0);
#else
        Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif

    public static bool PointerHeld =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButton(0);
#else
        Input.touchCount > 0 &&
        (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary);
#endif

    public static bool PointerUp =>
#if UNITY_EDITOR || UNITY_STANDALONE
        Input.GetMouseButtonUp(0);
#else
        Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
#endif
}

