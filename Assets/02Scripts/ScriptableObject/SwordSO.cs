using UnityEngine;

/// <summary>
/// 검 무기의 속성/대미지 배율/이펙트 프리팹 및 회전값을 보관하는 ScriptableObject.
/// 런타임에서 효과 재생과 대미지 계산 시 참조됩니다.
/// </summary>
[CreateAssetMenu(menuName = "Scriptable Object/Sword Asset")]
public class SwordSO : ScriptableObject
{
    [Header("Elemental (무속성은 'Count')")]
    [SerializeField] private ElementalManager.ElementalType elType;

    [Space(20)]
    [Header("Damage Rate")]
    [SerializeField] private float minDamageRate;      // 최소 대미지 배율(공격 타입별 가중치에 사용)

    [Space(20)]
    [Header("Effect")]
    [SerializeField] private GameObject slashEffect;   // 베기 이펙트 프리팹
    [SerializeField] private float effectRotX_NAtk;    // 일반 공격 이펙트 X축 회전 보정
    [SerializeField] private float effectRotX_SAtk;    // 강공격 이펙트 X축 회전 보정

    #region GET
    public ElementalManager.ElementalType ElType => elType;
    public float MinDamageRate => minDamageRate;
    public GameObject SlashEffect => slashEffect;
    public float EffectRotXNAtk => effectRotX_NAtk;
    public float EffectRotXSAtk => effectRotX_SAtk;
    #endregion
}
