using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 관련 수치 계산 유틸리티(근접/마법 피해, 원소 상성 및 부가 확률 계산).
/// 모든 계산 메서드는 정적이며, 현재 동작을 유지합니다.
/// </summary>
public class Calculator : MonoBehaviour
{
    // --- Constant: Damage base offsets
    private const float MELEE_DAMAGE_OFFSET = 1f;
    private const float SPELL_DAMAGE_OFFSET = 1f;

    // --- Constant: Elemental effectiveness scaling
    private const float ELEMENTAL_EFFECTIVENESS_OFFSET = 0.01f;

    /// <summary>원소 상성 가중치(ODDS에 곱해지는 비율·정수값을 0.01 배율과 곱해 사용).</summary>
    private enum ElementalEffectiveness
    {
        Effective = 200, // 2.00
        Resistant = 5,   // 0.05
        Normal = 75,     // 0.75
        Same = 25        // 0.25
    }

    // --- Elemental cycles: A(불-얼음-빛), B(독-무속)
    private static ElementalManager.ElementalType[] elCirculationA =
    {
        ElementalManager.ElementalType.Fire,
        ElementalManager.ElementalType.Ice,
        ElementalManager.ElementalType.Light
    };

    private static ElementalManager.ElementalType[] elCirculationB =
    {
        ElementalManager.ElementalType.Poison,
        ElementalManager.ElementalType.Count
    };

    /// <summary>피해 정보(표시 타입, 값).</summary>
    public class DamageInfo
    {
        DamageTextController.DamageType type;
        float value;

        public DamageInfo(
            DamageTextController.DamageType type = DamageTextController.DamageType.Normal,
            float value = 0f)
        {
            this.type = type;
            this.value = value;
        }

        public DamageTextController.DamageType Type => type;
        public float Value => value;
    }

    #region DAMAGE

    /// <summary>
    /// 근접 피해 계산.
    /// 1) 회피 판정 → 2) 기본 피해(공/방 비율, 오프셋, 스킬계수) → 3) 치명 판정 → 4) 안정치(STBR)로 난수 분산.
    /// 적중 시 플레이어는 소량의 MP 보너스 획득.
    /// </summary>
    public static DamageInfo MeleeDamage(StatController self, StatController target, float rate = 1f)
    {
        DamageInfo result;

        // (1) 회피 판정: 대상 회피율을 자신 치명과의 비율로 스케일링
        float target_evdr = target.EVDR * (self.CTKR == 0 ? 1 : Mathf.Clamp(target.EVDR / self.CTKR, 0.5f, 1f));
        if (CheckOdds(target_evdr))
        {
            Debug.Log("[Evaded]");
            return new DamageInfo(DamageTextController.DamageType.Evaded);
        }

        // [보너스] 플레이어의 물리 적중 시 MP 소량 회복
        if (self.GetAsset.Owner == BaseStatSO.OWNER.Player)
            self.MPCurrent = Mathf.Clamp(self.MPCurrent + 0.1f, 0f, self.MP);

        // (2) 기본 피해 계산
        float damage_normal = self.MATK * (self.MATK / target.DEF) * MELEE_DAMAGE_OFFSET * rate;
        float damage_critical = damage_normal * 1.33f;

        // (3) 치명 판정: 자신 치명률을 대상 회피와의 비율로 스케일링
        float self_ctkr = self.CTKR * (target.EVDR == 0 ? 1 : Mathf.Clamp(self.CTKR / target.EVDR, 0.5f, 1f));
        if (CheckOdds(self_ctkr))
        {
            result = new DamageInfo(DamageTextController.DamageType.Critical, RandomizedDamage(damage_critical));
            Debug.Log($"[{result.Type}] {result.Value}");
            return result;
        }

        // (4) 안정치(STBR)로 난수 분산
        damage_normal = Random.Range(damage_normal * self.STBR, damage_normal);
        result = new DamageInfo(DamageTextController.DamageType.Normal, RandomizedDamage(damage_normal));
        Debug.Log($"[{result.Type}] {result.Value}");
        return result;
    }

    /// <summary>
    /// 마법 피해 계산.
    /// 1) 회피 판정 → 2) 기본 피해(공/방 비율, 오프셋, 스킬계수) → 3) 치명 판정.
    /// </summary>
    public static DamageInfo SpellDamage(StatController self, StatController target, float rate = 1f)
    {
        DamageInfo result;

        // (1) 회피 판정
        float target_evdr = target.EVDR * (self.CTKR == 0 ? 1 : Mathf.Clamp(target.EVDR / self.CTKR, 0.5f, 1f));
        if (CheckOdds(target_evdr))
        {
            Debug.Log("[Evaded]");
            return new DamageInfo(DamageTextController.DamageType.Evaded);
        }

        // (2) 기본 피해 계산
        float damage_normal = self.SATK * (self.SATK / target.DEF) * SPELL_DAMAGE_OFFSET * rate;
        float damage_critical = damage_normal * 1.15f;

        // (3) 치명 판정
        float self_ctkr = self.CTKR * (target.EVDR == 0 ? 1 : Mathf.Clamp(self.CTKR / target.EVDR, 0.5f, 1f));
        if (CheckOdds(self_ctkr))
        {
            result = new DamageInfo(DamageTextController.DamageType.Critical, RandomizedDamage(damage_critical));
            Debug.Log($"[{result.Type}] {result.Value}");
            return result;
        }

        result = new DamageInfo(DamageTextController.DamageType.Normal, RandomizedDamage(damage_normal));
        Debug.Log($"[{result.Type}] {result.Value}");
        return result;
    }

    /// <summary>
    /// 원소 부가 효과(DoT/디버프 등) 발동 가능 여부 확률 계산.
    /// - 같은 속성: Same 가중치
    /// - A군(불/얼음/빛) 내부: 순환관계로 효과/저항 결정
    /// - B군(독/무속) 내부: 독→무속: Normal, 무속→독: Resistant
    /// - 그 외 조합: Normal
    /// </summary>
    public static bool CheckElementalAttackable(ElementalController self, StatController target)
    {
        ElementalManager.ElementalType attacker = self.GetAsset.GetELTYPE();
        ElementalManager.ElementalType defender = target.ELResist;

        float scaledOdds = self.GetAsset.GetODDS(0) * ELEMENTAL_EFFECTIVENESS_OFFSET;

        // 같은 속성
        if (attacker == defender)
        {
            scaledOdds *= (int)ElementalEffectiveness.Same;
            return CheckOdds(scaledOdds);
        }

        bool isAttackerInA = System.Array.Exists(elCirculationA, e => e == attacker);
        bool isAttackerInB = System.Array.Exists(elCirculationB, e => e == attacker);
        bool isDefenderInA = System.Array.Exists(elCirculationA, e => e == defender);
        bool isDefenderInB = System.Array.Exists(elCirculationB, e => e == defender);

        // A군 내부 상성 / B군 내부 상성 / 그 외 Normal
        if (isAttackerInA && isDefenderInA)
            scaledOdds *= (int)GetEffectivenessFromA(attacker, defender);
        else if (isAttackerInB && isDefenderInB)
            scaledOdds *= (int)GetEffectivenessFromB(attacker);
        else
            scaledOdds *= (int)ElementalEffectiveness.Normal;

        return CheckOdds(scaledOdds);
    }

    /// <summary>
    /// B군(독, 무속) 상성: 독→무속 = Normal, 무속→독 = Resistant.
    /// </summary>
    private static ElementalEffectiveness GetEffectivenessFromB(ElementalManager.ElementalType attacker) =>
        attacker == ElementalManager.ElementalType.Poison
            ? ElementalEffectiveness.Normal
            : ElementalEffectiveness.Resistant;

    /// <summary>
    /// A군(불-얼음-빛) 상성: 순환 배열에서 다음 인덱스는 Resistant, 그 외는 Effective.
    /// (불→얼음: Resistant / 불→빛: Effective) 등.
    /// </summary>
    private static ElementalEffectiveness GetEffectivenessFromA(
        ElementalManager.ElementalType attacker,
        ElementalManager.ElementalType defender)
    {
        int attackerIdx = System.Array.IndexOf(elCirculationA, attacker);
        int defenderIdx = System.Array.IndexOf(elCirculationA, defender);

        return (attackerIdx + 1) % elCirculationA.Length == defenderIdx
            ? ElementalEffectiveness.Resistant
            : ElementalEffectiveness.Effective;
    }

    /// <summary>확률 판정(0~1 범위 클램프 후 랜덤 비교).</summary>
    private static bool CheckOdds(float odds) =>
        Random.Range(0f, 1f) < Mathf.Clamp(odds, 0f, 1f);

    /// <summary>피해 난수 분산(±6.6%) 후 올림.</summary>
    private static float RandomizedDamage(float value) =>
        Mathf.CeilToInt(Random.Range(value * 0.866f, value * 1.066f));

    #endregion
}
