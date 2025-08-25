using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어의 Y축 회전에 반대되는 로컬 Y 회전을 적용해
/// 부착 오브젝트(UI 등)의 시각적 흔들림을 줄입니다.
/// </summary>
public class RotationStabilizer : MonoBehaviour
{
    private PlayerController _plyCtrl;
    private bool InitSucceed;

    private void Start()
    {
        StartCoroutine(Init());
    }

    private void LateUpdate()
    {
        if (!InitSucceed) return;
        FixRotation();
    }

    /// <summary>
    /// 플레이어의 Y 회전을 반대로 적용해 로컬 회전을 보정합니다.
    /// </summary>
    private void FixRotation()
    {
        Vector3 euler = transform.localEulerAngles;
        euler.y = -_plyCtrl.transform.eulerAngles.y;
        transform.localEulerAngles = euler;
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst);

        InitSucceed = true;
        _plyCtrl = GameManager.Inst._plyCtrl;
    }
}
