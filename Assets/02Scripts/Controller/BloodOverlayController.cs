using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodOverlayController : MonoBehaviour
{
    // 피격 오버레이가 화면에 남는 시간(초)
    public const float BLOOD_OVERLAY_LIFETIME = 0.15f;

    [Space(20)]
    [SerializeField] private GameObject bloodOverlayMain;
    [SerializeField] private GameObject[] bloodOverlays;

    /// <summary>
    /// 서브 오버레이(랜덤 선택 대상)의 개수.
    /// </summary>
    public int CountBloodOverlays()
    {
        return bloodOverlays.Length;
    }

    /// <summary>
    /// 메인 + 인덱스 기반 서브 오버레이 세트 반환.
    /// 호출 측에서 유효한 인덱스를 보장해야 함.
    /// </summary>
    public GameObject[] GetBloodOverlay(int idx)
    {
        return new GameObject[] { bloodOverlayMain, bloodOverlays[idx] };
    }
}
