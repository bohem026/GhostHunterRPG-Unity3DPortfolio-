using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���� ���� ��ġ ��� ��ƿ��Ƽ(����/���� ����, ���� �� �� �ΰ� Ȯ�� ���).
/// ��� ��� �޼���� �����̸�, ���� ������ �����մϴ�.
/// </summary>
public class Calculator : MonoBehaviour
{
    // --- Constant: Damage base offsets
    private const float MELEE_DAMAGE_OFFSET = 1f;
    private const float SPELL_DAMAGE_OFFSET = 1f;

    // --- Constant: Elemental effectiveness scaling
    private const float ELEMENTAL_EFFECTIVENESS_OFFSET = 0.01f;

    /// <summary>���� �� ����ġ(ODDS�� �������� �������������� 0.01 ������ ���� ���).</summary>
    private enum ElementalEffectiveness
    {
        Effective = 200, // 2.00
        Resistant = 5,   // 0.05
        Normal = 75,     // 0.75
        Same = 25        // 0.25
    }

    // --- Elemental cycles: A(��-����-��), B(��-����)
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

    /// <summary>���� ����(ǥ�� Ÿ��, ��).</summary>
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
    /// ���� ���� ���.
    /// 1) ȸ�� ���� �� 2) �⺻ ����(��/�� ����, ������, ��ų���) �� 3) ġ�� ���� �� 4) ����ġ(STBR)�� ���� �л�.
    /// ���� �� �÷��̾�� �ҷ��� MP ���ʽ� ȹ��.
    /// </summary>
    public static DamageInfo MeleeDamage(StatController self, StatController target, float rate = 1f)
    {
        DamageInfo result;

        // (1) ȸ�� ����: ��� ȸ������ �ڽ� ġ����� ������ �����ϸ�
        float target_evdr = target.EVDR * (self.CTKR == 0 ? 1 : Mathf.Clamp(target.EVDR / self.CTKR, 0.5f, 1f));
        if (CheckOdds(target_evdr))
        {
            Debug.Log("[Evaded]");
            return new DamageInfo(DamageTextController.DamageType.Evaded);
        }

        // [���ʽ�] �÷��̾��� ���� ���� �� MP �ҷ� ȸ��
        if (self.GetAsset.Owner == BaseStatSO.OWNER.Player)
            self.MPCurrent = Mathf.Clamp(self.MPCurrent + 0.1f, 0f, self.MP);

        // (2) �⺻ ���� ���
        float damage_normal = self.MATK * (self.MATK / target.DEF) * MELEE_DAMAGE_OFFSET * rate;
        float damage_critical = damage_normal * 1.33f;

        // (3) ġ�� ����: �ڽ� ġ����� ��� ȸ�ǿ��� ������ �����ϸ�
        float self_ctkr = self.CTKR * (target.EVDR == 0 ? 1 : Mathf.Clamp(self.CTKR / target.EVDR, 0.5f, 1f));
        if (CheckOdds(self_ctkr))
        {
            result = new DamageInfo(DamageTextController.DamageType.Critical, RandomizedDamage(damage_critical));
            Debug.Log($"[{result.Type}] {result.Value}");
            return result;
        }

        // (4) ����ġ(STBR)�� ���� �л�
        damage_normal = Random.Range(damage_normal * self.STBR, damage_normal);
        result = new DamageInfo(DamageTextController.DamageType.Normal, RandomizedDamage(damage_normal));
        Debug.Log($"[{result.Type}] {result.Value}");
        return result;
    }

    /// <summary>
    /// ���� ���� ���.
    /// 1) ȸ�� ���� �� 2) �⺻ ����(��/�� ����, ������, ��ų���) �� 3) ġ�� ����.
    /// </summary>
    public static DamageInfo SpellDamage(StatController self, StatController target, float rate = 1f)
    {
        DamageInfo result;

        // (1) ȸ�� ����
        float target_evdr = target.EVDR * (self.CTKR == 0 ? 1 : Mathf.Clamp(target.EVDR / self.CTKR, 0.5f, 1f));
        if (CheckOdds(target_evdr))
        {
            Debug.Log("[Evaded]");
            return new DamageInfo(DamageTextController.DamageType.Evaded);
        }

        // (2) �⺻ ���� ���
        float damage_normal = self.SATK * (self.SATK / target.DEF) * SPELL_DAMAGE_OFFSET * rate;
        float damage_critical = damage_normal * 1.15f;

        // (3) ġ�� ����
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
    /// ���� �ΰ� ȿ��(DoT/����� ��) �ߵ� ���� ���� Ȯ�� ���.
    /// - ���� �Ӽ�: Same ����ġ
    /// - A��(��/����/��) ����: ��ȯ����� ȿ��/���� ����
    /// - B��(��/����) ����: ���湫��: Normal, ���ӡ浶: Resistant
    /// - �� �� ����: Normal
    /// </summary>
    public static bool CheckElementalAttackable(ElementalController self, StatController target)
    {
        ElementalManager.ElementalType attacker = self.GetAsset.GetELTYPE();
        ElementalManager.ElementalType defender = target.ELResist;

        float scaledOdds = self.GetAsset.GetODDS(0) * ELEMENTAL_EFFECTIVENESS_OFFSET;

        // ���� �Ӽ�
        if (attacker == defender)
        {
            scaledOdds *= (int)ElementalEffectiveness.Same;
            return CheckOdds(scaledOdds);
        }

        bool isAttackerInA = System.Array.Exists(elCirculationA, e => e == attacker);
        bool isAttackerInB = System.Array.Exists(elCirculationB, e => e == attacker);
        bool isDefenderInA = System.Array.Exists(elCirculationA, e => e == defender);
        bool isDefenderInB = System.Array.Exists(elCirculationB, e => e == defender);

        // A�� ���� �� / B�� ���� �� / �� �� Normal
        if (isAttackerInA && isDefenderInA)
            scaledOdds *= (int)GetEffectivenessFromA(attacker, defender);
        else if (isAttackerInB && isDefenderInB)
            scaledOdds *= (int)GetEffectivenessFromB(attacker);
        else
            scaledOdds *= (int)ElementalEffectiveness.Normal;

        return CheckOdds(scaledOdds);
    }

    /// <summary>
    /// B��(��, ����) ��: ���湫�� = Normal, ���ӡ浶 = Resistant.
    /// </summary>
    private static ElementalEffectiveness GetEffectivenessFromB(ElementalManager.ElementalType attacker) =>
        attacker == ElementalManager.ElementalType.Poison
            ? ElementalEffectiveness.Normal
            : ElementalEffectiveness.Resistant;

    /// <summary>
    /// A��(��-����-��) ��: ��ȯ �迭���� ���� �ε����� Resistant, �� �ܴ� Effective.
    /// (�ҡ����: Resistant / �ҡ��: Effective) ��.
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

    /// <summary>Ȯ�� ����(0~1 ���� Ŭ���� �� ���� ��).</summary>
    private static bool CheckOdds(float odds) =>
        Random.Range(0f, 1f) < Mathf.Clamp(odds, 0f, 1f);

    /// <summary>���� ���� �л�(��6.6%) �� �ø�.</summary>
    private static float RandomizedDamage(float value) =>
        Mathf.CeilToInt(Random.Range(value * 0.866f, value * 1.066f));

    #endregion
}
