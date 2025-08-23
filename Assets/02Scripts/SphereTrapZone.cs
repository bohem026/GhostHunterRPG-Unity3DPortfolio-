using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SphereTrapZone : MonoBehaviour
{
    [Space(10)]
    [Header("DURATION")]
    [SerializeField] private float DURATION;

    PlayerController _PlyCtrl;
    Vector3 centerPosition;
    float radius;

    void Start()
    {
        //Player
        _PlyCtrl = GameManager.Inst._plyCtrl;
        //Position: center
        centerPosition = transform.position;
        //Radius
        SphereCollider collider = GetComponentInChildren<SphereCollider>();
        radius = collider.radius * collider.transform.lossyScale.x;
    }

    void OnEnable()
    {
        StartCoroutine(DisplaySelf());
    }

    void Update()
    {
        // ĳ���Ͱ� ����� �� ��� ������ �������� �ǵ��� (����)
        Vector3 offset = _PlyCtrl.transform.position - centerPosition;
        // offset�� ���� ���� ���(y�� ����)
        float temp = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
        if (temp > radius)
        {
            Vector3 clampedPos = centerPosition + offset.normalized * radius;
            _PlyCtrl.GetComponent<CharacterController>().enabled = false;
            _PlyCtrl.transform.position = clampedPos;
            _PlyCtrl.GetComponent<CharacterController>().enabled = true;
        }
    }

    IEnumerator DisplaySelf()
    {
        yield return new WaitUntil(() => StageManager.Inst);
        yield return new WaitUntil(() => StageManager.Inst.IsEndOfStage);

        gameObject.SetActive(false);
    }

    //private void Deactivate()
    //    => gameObject.SetActive(false);

    public void DeactivateByForce()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
