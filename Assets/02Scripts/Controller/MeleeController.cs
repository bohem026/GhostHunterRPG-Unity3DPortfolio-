using UnityEngine;

public class MeleeController : MonoBehaviour
{
    public const float SLASH_DURATION = 0.5f;
    private const string ROOT_NAME = "SlashEffectRoot";

    [SerializeField] private SwordSO swordAsset; // 근접공격용 소드 스펙/이펙트 자산

    // --- Runtime refs ---
    private PlayerController _plyCtrl;
    private Transform slashEffectRoot;

    /// <summary>
    /// 플레이어/이펙트 루트 트랜스폼을 캐싱한다.
    /// </summary>
    private void Start()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        slashEffectRoot = _plyCtrl.transform.Find(ROOT_NAME);
    }

    /// <summary>
    /// 현재 공격 상태(평타/강공)에 맞춰 슬래시 이펙트를 생성하고
    /// 회전값을 계산해 재생한 뒤 일정 시간 후 파괴한다.
    /// </summary>
    public void InstSlashEffect(AnimController.AnimState state)
    {
        float effectRotX = 0f;

        // 공격 타입에 따른 X축 보정 각도 선택
        switch (state)
        {
            case AnimController.AnimState.NAttack0:
                effectRotX = swordAsset.EffectRotXNAtk;
                break;
            case AnimController.AnimState.NAttack1:
                effectRotX = -swordAsset.EffectRotXNAtk;
                break;
            case AnimController.AnimState.SAttack:
                effectRotX = swordAsset.EffectRotXSAtk;
                break;
            default:
                break;
        }

        // 이펙트 진행 방향 벡터(플레이어→이펙트 루트)
        Vector3 dirVec = new Vector3(
            slashEffectRoot.position.x - transform.position.x,
            0.0f,
            slashEffectRoot.position.z - transform.position.z
        );

        // LookRotation의 up 벡터에 공격 타입 보정값 적용
        GameObject obj = Instantiate(
            swordAsset.SlashEffect,
            slashEffectRoot.position,
            Quaternion.LookRotation(
                dirVec,
                new Vector3(dirVec.z > 0 ? -effectRotX : effectRotX, 1, 0)
            ),
            HitEffectPoolManager.Inst.transform
        );

        // 파티클 재생 및 수명 관리
        obj.GetComponent<ParticleSystem>().Play();
        GameObject.Destroy(obj, SLASH_DURATION);
    }

    #region GET
    /// <summary>
    /// 공격 타입에 따른 데미지 배율을 반환한다.
    /// </summary>
    public float GetDamageRate(AnimController.AnimState state)
    {
        float rate = 1f;

        switch (state)
        {
            case AnimController.AnimState.NAttack0:
                rate = swordAsset.MinDamageRate * 0.95f;
                break;
            case AnimController.AnimState.NAttack1:
                rate = swordAsset.MinDamageRate * 1.1f;
                break;
            case AnimController.AnimState.SAttack:
                rate = swordAsset.MinDamageRate * 1.66f;
                break;
            default:
                break;
        }

        return rate;
    }

    public SwordSO GetAsset => swordAsset;
    #endregion
}
