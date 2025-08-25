using System.Collections;
using UnityEngine;

/// <summary>
/// �÷��̾��� Y�� ȸ���� �ݴ�Ǵ� ���� Y ȸ���� ������
/// ���� ������Ʈ(UI ��)�� �ð��� ��鸲�� ���Դϴ�.
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
    /// �÷��̾��� Y ȸ���� �ݴ�� ������ ���� ȸ���� �����մϴ�.
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
