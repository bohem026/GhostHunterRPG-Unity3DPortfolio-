using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ų(�ֹ�) �������� �������� �ε�/�����ϰ�,
/// ���� ��ų ������ �´� �������� �����ϴ� �Ŵ���.
/// </summary>
public class SpellManager : MonoBehaviour
{
    public static SpellManager Inst;

    // �ִ� ��ų ����
    private static int MAX_SPELL_LV = 5;

    /// <summary>��ų ���� ���� ���(�ܺο��� ����� �� �־� ����).</summary>
    public enum SpellLvMode { UP, DOWN, RESET }

    [SerializeField] private GameObject[] SpellPrototypes;   // ��ų ���� ������(�̸� ��Ģ ��� �ε��� ����)
    [SerializeField] private List<GameObject>[] SpellList;   // [��ųID][����] ������ ���

    // ����� ��ųƮ�� ���� ����(�ܺ� ���� ���ɼ��� ����� ���� ����)
    public int[] CurSpellLvArray;

    // ���� �۾���
    private GameObject spell;
    private bool isInitialized;

    /// <summary>
    /// �̱��� ���� �� ��ų ����Ʈ �ʱ�ȭ.
    /// </summary>
    private void Awake()
    {
        if (isInitialized) return;

        Inst = this;
        InitSpellList();

        isInitialized = true;
    }

    /// <summary>
    /// ��ų ���/���� �迭 ���� �� ������ �ε�.
    /// </summary>
    private void InitSpellList()
    {
        // 1) �����̳� �ʱ�ȭ
        SpellList = new List<GameObject>[SpellPrototypes.Length];
        CurSpellLvArray = new int[SpellList.Length];
        for (int i = 0; i < SpellList.Length; i++)
        {
            SpellList[i] = new List<GameObject>();

            // -1: ���ر�, 0 �̻�: �ر�(���� ����). ���� ����� �⺻ 0���� ����.
            CurSpellLvArray[i] = 0;
        }

        // 2) ������Ÿ���� ID �������� ����
        Array.Sort(
            SpellPrototypes,
            (a, b) => a.GetComponent<SpellPrototypeSorter>().ID
                       .CompareTo(b.GetComponent<SpellPrototypeSorter>().ID)
        );

        // 3) ������ ������ �ε� �� ���� (��Ģ: "Prefabs/Spell/{�̸�}_Lv{����}")
        for (int outer = 0; outer < SpellPrototypes.Length; outer++)
        {
            for (int inner = 0; inner < MAX_SPELL_LV; inner++)
            {
                string path = $"Prefabs/Spell/{SpellPrototypes[outer].name}_Lv{inner}";
                spell = ResourceUtility.GetResourceByType<GameObject>(path);
                if (!spell) continue;

                // ���� �ε����� �°� ����(0 ���)
                SpellList[outer].Insert(inner, spell);
            }
        }
    }

    #region --- Public API ---
    /// <summary>
    /// ��ų ID�� �ش��ϴ� ���� ������ ��ų �������� ��ȯ�Ѵ�.
    /// (���� 0�̸� �ر� ������ �����Ͽ� null ��ȯ)
    /// </summary>
    public GameObject GetSpell(int ID)
    {
        if (ID >= GetSpellTypeCount())
            return null;

        // ��ų Ʈ������ ���� �ǵ��� ���� 0���� �������� ��Ÿ�� ����
        var meta = SpellList[ID][0].GetComponent<SpellEffectController>();
        var outer = meta.OUTER;
        var inner = meta.INNER;

        int level = GlobalValue.Instance.GetSkillLevelByEnum(outer, inner);

        if (level == 0) return null;
        return SpellList[ID][level - 1];
    }

    /// <summary>
    /// ��ų ����(Ÿ��)�� �� ������ ��ȯ�Ѵ�.
    /// </summary>
    public int GetSpellTypeCount()
    {
        return SpellPrototypes.Length;
    }
    #endregion
}
