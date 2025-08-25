using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodOverlayController : MonoBehaviour
{
    // �ǰ� �������̰� ȭ�鿡 ���� �ð�(��)
    public const float BLOOD_OVERLAY_LIFETIME = 0.15f;

    [Space(20)]
    [SerializeField] private GameObject bloodOverlayMain;
    [SerializeField] private GameObject[] bloodOverlays;

    /// <summary>
    /// ���� ��������(���� ���� ���)�� ����.
    /// </summary>
    public int CountBloodOverlays()
    {
        return bloodOverlays.Length;
    }

    /// <summary>
    /// ���� + �ε��� ��� ���� �������� ��Ʈ ��ȯ.
    /// ȣ�� ������ ��ȿ�� �ε����� �����ؾ� ��.
    /// </summary>
    public GameObject[] GetBloodOverlay(int idx)
    {
        return new GameObject[] { bloodOverlayMain, bloodOverlays[idx] };
    }
}
