using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationStabilizer : MonoBehaviour
{
    private PlayerController _plyCtrl;
    private bool InitSucceed;

    private void Start()
    {
        StartCoroutine(Init());
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!InitSucceed) return;

        FixRotation();
    }

    private void FixRotation()
    {
        Vector3 euler = transform.localEulerAngles;
        euler.y = -_plyCtrl.transform.eulerAngles.y;
        transform.localEulerAngles = euler;
    }

    IEnumerator Init()
    {
        //Wait until GameManager initialization succeed.
        yield return new WaitUntil(() => GameManager.Inst);
        InitSucceed = true;

        _plyCtrl = GameManager.Inst._plyCtrl;
    }
}
