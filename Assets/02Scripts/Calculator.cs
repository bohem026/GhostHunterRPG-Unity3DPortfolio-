using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Calculator : MonoBehaviour
{
    //Damage offset: Melee, Spell
    private const float MELEE_DAMAGE_OFFSET = 1f;
    private const float SPELL_DAMAGE_OFFSET = 1f;

    //Damage offset: Elemental
    private const float ELEMENTAL_EFFECTIVENESS_OFFSET = 0.01f;
    private enum ElementalEffectiveness
    {
        Effective = 200,
        Resistant = 5,
        Normal = 75,
        Same = 25
    }

    private static ElementalManager.ElementalType[] elCirculationA
        = {ElementalManager.ElementalType.Fire
            ,ElementalManager.ElementalType.Ice
            ,ElementalManager.ElementalType.Light};
    private static ElementalManager.ElementalType[] elCirculationB
        = {ElementalManager.ElementalType.Poison
            ,ElementalManager.ElementalType.Count};

    #region STAT
    public static float BuffHP(StatController owner, float rate) => owner.HP *= (1 + rate);
    public static float BuffMP(StatController owner, float rate) => owner.MP *= (1 + rate);
    public static float BuffMATK(StatController owner, float rate) => owner.MATK *= (1 + rate);
    public static float BuffSATK(StatController owner, float rate) => owner.SATK *= (1 + rate);
    public static float BuffDEF(StatController owner, float rate) => owner.DEF *= (1 + rate);
    public static float BuffCTKR(StatController owner, float rate) => owner.CTKR *= (1 + rate);
    public static float BuffEVDR(StatController owner, float rate) => owner.EVDR *= (1 + rate);
    #endregion

    public class DamageInfo
    {
        DamageTextController.DamageType type;
        float value;

        public DamageInfo(DamageTextController.DamageType type = DamageTextController.DamageType.Normal, float value = 0f)
        {
            this.type = type;
            this.value = value;
        }

        public DamageTextController.DamageType Type => type;
        public float Value => value;
    }

    #region DAMAGE
    public static DamageInfo MeleeDamage
        (
        StatController self
        , StatController target
        , float rate = 1f
        )
    {
        DamageInfo result;

        //1. 대상 회피율 계산_ 대상 회피율 * (대상 회피율/ 자신 치명률)_ MAX(대상 회피율)
        float target_evdr = target.EVDR
            * (self.CTKR == 0
            ? 1
            : Mathf.Clamp(target.EVDR / self.CTKR, 0.5f, 1f));
        if (CheckOdds(target_evdr))
        {
            Debug.Log("[Evaded]");
            return new DamageInfo(DamageTextController.DamageType.Evaded);
        }

        //[추가] 물리 공격 적중 시 MP 보너스 획득
        if (self.GetAsset.Owner == BaseStatSO.OWNER.Player)
            self.MPCurrent = Mathf.Clamp(self.MPCurrent + 0.1f, 0f, self.MP);

        //2. 표준 대미지 계산_ 자신 공격력 * (자신 공격력/ 대상 방어력) * 물리 계수 * 스킬 계수
        float damage_normal = self.MATK * (self.MATK / target.DEF) * MELEE_DAMAGE_OFFSET * rate;
        float damage_critical = damage_normal * 1.33f;

        //3. 자신 치명률 계산_ 자신 치명률 * (자신 치명률/ 대상 회피율)_ MAX(자신 치명률)
        float self_ctkr = self.CTKR
            * (target.EVDR == 0
            ? 1
            : Mathf.Clamp(self.CTKR / target.EVDR, 0.5f, 1f));
        if (CheckOdds(self_ctkr))
        {
            result = new DamageInfo(DamageTextController.DamageType.Critical
                , RandomizedDamage(damage_critical));

            Debug.Log($"[{result.Type}] {result.Value}");
            return result;
        }

        //4. 자신 안정치 계산_ 1 - 자신 안정치
        damage_normal = Random.Range(damage_normal * self.STBR, damage_normal);
        result = new DamageInfo(DamageTextController.DamageType.Normal
                , RandomizedDamage(damage_normal));

        Debug.Log($"[{result.Type}] {result.Value}");
        return result;
    }

    public static DamageInfo SpellDamage
        (
        StatController self
        , StatController target
        , float rate = 1f
        )
    {
        DamageInfo result;

        //1. 대상 회피율 계산_ 대상 회피율 * (대상 회피율/ 자신 치명률)_ MAX(대상 회피율)
        float target_evdr = target.EVDR
            * (self.CTKR == 0
            ? 1
            : Mathf.Clamp(target.EVDR / self.CTKR, 0.5f, 1f));
        if (CheckOdds(target_evdr))
        {
            Debug.Log("[Evaded]");
            return new DamageInfo(DamageTextController.DamageType.Evaded);
        }

        //2. 표준 대미지 계산_ 자신 공격력 * (자신 공격력/ 대상 방어력) * 마법 계수 * 스킬 계수
        float damage_normal = self.SATK * (self.SATK / target.DEF) * SPELL_DAMAGE_OFFSET * rate;
        float damage_critical = damage_normal * 1.15f;

        //3. 자신 치명률 계산_ 자신 치명률 * (자신 치명률/ 대상 회피율)_ MAX(자신 치명률)
        float self_ctkr = self.CTKR
            * (target.EVDR == 0
            ? 1
            : Mathf.Clamp(self.CTKR / target.EVDR, 0.5f, 1f));
        if (CheckOdds(self_ctkr))
        {
            result = new DamageInfo(DamageTextController.DamageType.Critical
                , RandomizedDamage(damage_critical));

            Debug.Log($"[{result.Type}] {result.Value}");
            return result;
        }

        result = new DamageInfo(DamageTextController.DamageType.Normal
                , RandomizedDamage(damage_normal));

        Debug.Log($"[{result.Type}] {result.Value}");
        return result;
    }

    public static bool CheckElementalAttackable
        (
        ElementalController self
        , StatController target
        )
    {
        ElementalManager.ElementalType attacker = self.GetAsset.GetELTYPE();
        ElementalManager.ElementalType defender = target.ELResist;

        float scaledOdds = self.GetAsset.GetODDS(0/*Level*/)
            * ELEMENTAL_EFFECTIVENESS_OFFSET;

        //1. Check(ElementalEffectiveness: Same)
        if (attacker == defender)
        {
            scaledOdds *= (int)ElementalEffectiveness.Same;
            return CheckOdds(scaledOdds);
        }

        bool isAttackerInA = System.Array.Exists(
            elCirculationA, e => e == attacker);
        bool isAttackerInB = System.Array.Exists(
            elCirculationB, e => e == attacker);
        bool isDefenderInA = System.Array.Exists(
            elCirculationA, e => e == defender);
        bool isDefenderInB = System.Array.Exists(
            elCirculationB, e => e == defender);

        //2. Check(ElementalEffectiveness: Effective, Resist)
        if (isAttackerInA && isDefenderInA)
            scaledOdds *= (int)GetEffectivenessFromA(attacker, defender);
        else if (isAttackerInB && isDefenderInB)
            scaledOdds *= (int)GetEffectivenessFromB(attacker);
        //2. Check(ElementalEffectiveness: Normal)
        else
            scaledOdds *= (int)ElementalEffectiveness.Normal;

        return CheckOdds(scaledOdds);
    }

    /// <summary>
    /// 독, 무속성 간의 상성을 반환
    /// 독-> 무: 일반
    /// 무-> 독: 저항
    /// </summary>
    /// <param name="attacker">공격자 공격 속성</param>
    /// <param name="defender">방어자 방어 속성</param>
    /// <returns>상성</returns>
    private static ElementalEffectiveness GetEffectivenessFromB
        (ElementalManager.ElementalType attacker)
        => attacker == ElementalManager.ElementalType.Poison
        ? ElementalEffectiveness.Normal
        : ElementalEffectiveness.Resistant;

    /// <summary>
    /// 불, 얼음, 빛 속성 간의 상성을 반환
    /// (불-> 얼음: 저항), (불-> 빛: 효과)
    /// (얼음-> 불: 효과), (얼음-> 빛: 저항)
    /// (빛-> 불: 저항), (빛-> 얼음: 효과)
    /// </summary>
    /// <param name="attacker">공격자 공격 속성</param>
    /// <param name="defender">방어자 방어 속성</param>
    /// <returns>상성</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private static ElementalEffectiveness GetEffectivenessFromA
        (
        ElementalManager.ElementalType attacker
        , ElementalManager.ElementalType defender
        )
    {
        int attackerIdx = System.Array.IndexOf(elCirculationA, attacker);
        int defenderIdx = System.Array.IndexOf(elCirculationA, defender);

        if ((attackerIdx + 1) % elCirculationA.Length == defenderIdx)
            return ElementalEffectiveness.Resistant;
        else
            return ElementalEffectiveness.Effective;
    }

    private static bool CheckOdds(float odds) =>
        Random.Range(0f, 1f) < Mathf.Clamp(odds, 0f, 1f) ? true : false;

    private static float RandomizedDamage(float value) => Mathf.CeilToInt(Random.Range(
                    value * 0.866f
                    , value * 1.066f));
    #endregion
}
