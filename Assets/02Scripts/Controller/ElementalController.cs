using System.Collections;
using UnityEngine;

/// <summary>
/// 원소(속성) 이펙트의 리소스를 조회/재생하고
/// DOT(지속 피해) 타이밍을 관리한다.
/// </summary>
public class ElementalController : MonoBehaviour
{
    private const string ROOT_NAME = "ElementalEffectRoot";

    [SerializeField] private ElementalStatSO elementalStatAsset;

    // 이펙트 프리팹 및 재생 중 인스턴스
    private GameObject elementalEffect;
    private GameObject displayingEffect;

    // DOT 타이밍
    private float damageDelta = 0f;
    private bool attackFlag = false;

    /// <summary>
    /// 시작 시 매니저 준비를 기다렸다가 이펙트 리소스를 캐싱한다.
    /// </summary>
    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// DOT(지속 피해) 간격을 누적하고 트리거 시점을 판단한다.
    /// (※ 현재 자산 존재 시 조기 반환하는 조건이 있어 의도 확인 필요)
    /// </summary>
    private void Update()
    {
        if (elementalStatAsset != null) return;
        if (!attackFlag) return;

        damageDelta += Time.deltaTime;
        if (damageDelta >= elementalStatAsset.GetITV(0 /* Level */))
        {
            damageDelta = 0f;
            // TODO: HitEffectPoolManager 등에서 DOT 타격/이펙트 처리
        }
    }

    /// <summary>
    /// ElementalManager 준비 대기 → 효과 타입/원소에 맞는 이펙트 프리팹 캐싱.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => ElementalManager.Inst);

        if (elementalStatAsset != null)
        {
            var efType = elementalStatAsset.GetEFTYPE();
            var elType = elementalStatAsset.GetELTYPE();
            elementalEffect = ElementalManager.Inst.GetEffect(efType, elType);
        }
    }

    /// <summary>
    /// 대상의 루트에 원소 이펙트를 재생한다(기존 이펙트 정리 후 시작).
    /// </summary>
    public void InstElementalEffect(Transform target)
    {
        ClearRoot(target);
        StartCoroutine(PlayElementalEffect(target));
    }

    /// <summary>
    /// 대상의 원소 이펙트 루트 하위 오브젝트를 모두 제거한다.
    /// </summary>
    private void ClearRoot(Transform target)
    {
        Transform root = GetRoot(target);
        foreach (Transform child in root)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 이펙트를 인스턴스화하여 지정 시간 동안 표시 후 정리한다.
    /// (DOT 로직은 Update의 delta 누적으로 별도 처리)
    /// </summary>
    private IEnumerator PlayElementalEffect(Transform target)
    {
        displayingEffect = Instantiate(elementalEffect, GetRoot(target));
        yield return new WaitForSeconds(elementalStatAsset.GetDUR(0));
        Destroy(displayingEffect);
    }

    // --- Helpers / Properties ---
    private Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    public ElementalStatSO GetAsset => elementalStatAsset;
}
