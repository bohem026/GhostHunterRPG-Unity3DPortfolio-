using System.Collections;
using UnityEngine;

/// <summary>
/// 구형 트랩 영역: 플레이어를 지정 반경(XZ 평면 기준) 안에 가두고,
/// 스테이지 종료 시 자동으로 비활성화됩니다.
/// </summary>
public class SphereTrapZone : MonoBehaviour
{
    PlayerController _PlyCtrl;
    Vector3 centerPosition;
    float radius;

    void OnEnable()
    {
        // 스테이지 종료 신호를 감지해 자동 비활성화
        StartCoroutine(DisplaySelf());
    }

    void Start()
    {
        // 플레이어/중심/반지름 캐싱 (월드 스케일 반영)
        _PlyCtrl = GameManager.Inst._plyCtrl;
        centerPosition = transform.position;

        SphereCollider collider = GetComponentInChildren<SphereCollider>();
        radius = collider.radius * collider.transform.lossyScale.x;
    }

    void Update()
    {
        // 플레이어가 반경 밖으로 벗어나면 CharacterController를 잠시 끄고 위치 보정
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
        // StageManager 준비 및 스테이지 종료 대기 → 비활성화
        yield return new WaitUntil(() => StageManager.Inst);
        yield return new WaitUntil(() => StageManager.Inst.IsEndOfStage);
        gameObject.SetActive(false);
    }

    /// <summary>외부에서 즉시 트랩 비활성화가 필요할 때 호출.</summary>
    public void DeactivateByForce()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
