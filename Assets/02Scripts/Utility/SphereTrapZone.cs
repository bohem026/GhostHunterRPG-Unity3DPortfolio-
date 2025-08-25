using System.Collections;
using UnityEngine;

/// <summary>
/// ���� Ʈ�� ����: �÷��̾ ���� �ݰ�(XZ ��� ����) �ȿ� ���ΰ�,
/// �������� ���� �� �ڵ����� ��Ȱ��ȭ�˴ϴ�.
/// </summary>
public class SphereTrapZone : MonoBehaviour
{
    PlayerController _PlyCtrl;
    Vector3 centerPosition;
    float radius;

    void OnEnable()
    {
        // �������� ���� ��ȣ�� ������ �ڵ� ��Ȱ��ȭ
        StartCoroutine(DisplaySelf());
    }

    void Start()
    {
        // �÷��̾�/�߽�/������ ĳ�� (���� ������ �ݿ�)
        _PlyCtrl = GameManager.Inst._plyCtrl;
        centerPosition = transform.position;

        SphereCollider collider = GetComponentInChildren<SphereCollider>();
        radius = collider.radius * collider.transform.lossyScale.x;
    }

    void Update()
    {
        // �÷��̾ �ݰ� ������ ����� CharacterController�� ��� ���� ��ġ ����
        Vector3 offset = _PlyCtrl.transform.position - centerPosition;
        float distXZ = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
        if (distXZ > radius)
        {
            Vector3 clampedPos = centerPosition + offset.normalized * radius;
            _PlyCtrl.GetComponent<CharacterController>().enabled = false;
            _PlyCtrl.transform.position = clampedPos;
            _PlyCtrl.GetComponent<CharacterController>().enabled = true;
        }
    }

    IEnumerator DisplaySelf()
    {
        // StageManager �غ� �� �������� ���� ��� �� ��Ȱ��ȭ
        yield return new WaitUntil(() => StageManager.Inst);
        yield return new WaitUntil(() => StageManager.Inst.IsEndOfStage);
        gameObject.SetActive(false);
    }

    /// <summary>�ܺο��� ��� Ʈ�� ��Ȱ��ȭ�� �ʿ��� �� ȣ��.</summary>
    public void DeactivateByForce()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
