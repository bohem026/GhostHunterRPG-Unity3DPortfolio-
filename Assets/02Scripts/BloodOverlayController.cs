using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BloodOverlayController : MonoBehaviour
{
    public const float BLOOD_OVERLAY_LIFETIME = 0.15f;

    [Space(20)]
    [SerializeField] private GameObject bloodOverlayMain;
    [SerializeField] private GameObject[] bloodOverlays;

    public int CountBloodOverlays()
    {
        return bloodOverlays.Length;
    }

    public GameObject[] GetBloodOverlay(int idx)
    {
        return new GameObject[] { bloodOverlayMain, bloodOverlays[idx] };
    }

    /*Test*/
    public GameObject GetBloodOverlay()
    {
        return bloodOverlayMain;
    }
    /*Test*/
}
