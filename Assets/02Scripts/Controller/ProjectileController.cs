using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    /// <summary>발사체 비행 방식.</summary>
    private enum ProjectileType { Arched, Straight, Count /*Length*/ }

    [SerializeField] private ProjectileType Type;
    [SerializeField] private float speed;

    private MonsterController shooter;
    private Rigidbody _rigid;
    private Vector3 staticTargetPosition = Vector3.zero;

    // ===== 외부에서 호출하는 생성/발사 API =====

    /// <summary>
    /// (포물선용) 고정된 목표 지점을 지정해 발사합니다.
    /// </summary>
    public void Inst(MonsterController shooter, Vector3 staticTargetPosition)
    {
        this.shooter = shooter;
        this.staticTargetPosition = staticTargetPosition;
        Debug.Log($"ME{transform.position}TARGET{staticTargetPosition}");
        Shoot();
    }

    /// <summary>
    /// (직선형 등) 정면 방향으로 발사합니다.
    /// </summary>
    public void Inst(MonsterController shooter)
    {
        this.shooter = shooter;
        Shoot();
    }

    // ===== 내부 동작 =====

    /// <summary>
    /// Rigidbody를 이용해 타입에 맞는 초기 속도/힘을 부여하고,
    /// 일정 시간 후 스스로 제거합니다.
    /// </summary>
    private void Shoot()
    {
        _rigid = GetComponent<Rigidbody>();

        switch (Type)
        {
            // 포물선: 목표 지점 + 약간의 상향 편차로 힘을 가함
            case ProjectileType.Arched:
                Vector3 dirVec = staticTargetPosition - transform.position;
                dirVec += Vector3.up * Random.Range(0.8f, 1f);
                _rigid.AddForce(dirVec.normalized * speed, ForceMode.Impulse);
                _rigid.AddTorque(Vector3.back * -10f, ForceMode.Impulse);
                break;

            // 직선: 전방 속도 부여
            case ProjectileType.Straight:
                _rigid.velocity = transform.forward * speed;
                break;
        }

        // 안전장치: 5초 후 자동 제거
        Destroy(gameObject, 5f);
    }

    // ===== 유니티 물리 이벤트 =====

    /// <summary>
    /// (포물선형) 비트리거 콜라이더 충돌 처리.
    /// 플레이어/벽에 닿으면 제거.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// (직선형) 트리거 충돌 처리.
    /// 플레이어에 닿으면 데미지 적용 후 제거.
    /// 포물선형이 'Shield_Destroyer' 레이어에 닿으면 차단 처리.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            shooter.ApplyDamageInfoToPlayer();
            Destroy(gameObject);
        }

        if (Type == ProjectileType.Arched &&
            other.gameObject.layer == LayerMask.NameToLayer("Shield_Destroyer"))
        {
            Debug.Log("BLOCKED!!");
            Destroy(gameObject);
        }
    }
}
