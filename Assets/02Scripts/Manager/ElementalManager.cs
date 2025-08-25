using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����(�Ӽ�) ����Ʈ �������� Ÿ�Ժ��� �ε�/����/�����ϴ� �Ŵ���.
/// </summary>
public class ElementalManager : MonoBehaviour
{
    // ���ҽ� ��� �����Ƚ�
    private const string ELEMENTAL_PRELINK = "Prefabs/Elemental/";

    // �̱���
    public static ElementalManager Inst;

    /// <summary>�÷��̾� ���� ���� ������ ����Ʈ Ÿ��(����/Ÿ��).</summary>
    public enum EffectType { Dot, Hit, Count /*Length*/ }

    /// <summary>���� �Ӽ� ����.</summary>
    public enum ElementalType { Fire, Ice, Light, Poison, Count /*Length*/ }

    // �������� �⺻ ����(�ܺο��� ����)
    public ElementalType STAGE_ELEMENT { get; set; }

    // [����Ʈ Ÿ��] �� ( [����] �� ������ ) ���� �����̳�
    private Dictionary<ElementalType, GameObject>[] EffectContainer;

    /// <summary>
    /// �̱��� ���۷����� �����Ѵ�.
    /// </summary>
    private void Awake()
    {
        Inst = this;
    }

    /// <summary>
    /// �����̳� �ʱ�ȭ �� ���ҽ��� �ε��Ѵ�.
    /// </summary>
    private void Start()
    {
        Init();
        LoadRcsToContainer();
    }

    /// <summary>
    /// ����Ʈ �����̳� �迭�� �����Ѵ�. (Ÿ�� ����ŭ ���� Ȯ��)
    /// </summary>
    private void Init()
    {
        EffectContainer = new Dictionary<ElementalType, GameObject>[(int)EffectType.Count];
    }

    /// <summary>
    /// ���ҽ��� ��� ��Ģ�� ���� �ε��� �����̳ʿ� �����Ѵ�.
    /// </summary>
    private void LoadRcsToContainer()
    {
        for (int i = 0; i < (int)EffectType.Count; i++)
        {
            EffectContainer[i] = new Dictionary<ElementalType, GameObject>();

            for (int j = 0; j < (int)ElementalType.Count; j++)
            {
                EffectType efType = (EffectType)i;
                ElementalType elType = (ElementalType)j;

                // ��� ��: "Prefabs/Elemental/Hit_Fire"
                string path = GetEffectPath(efType, elType);

                GameObject resource = ResourceUtility.GetResourceByType<GameObject>(path);
                EffectContainer[i][elType] = resource;
            }
        }
    }

    /// <summary>
    /// ���� ����Ʈ �������� ��ȯ�Ѵ�.
    /// </summary>
    public GameObject GetEffect(EffectType efType, ElementalType elType)
    {
        return EffectContainer[(int)efType][elType];
    }

    /// <summary>
    /// Ư�� ����Ʈ Ÿ�Կ� ���� ��ü [���ҡ�������] ��ųʸ��� ��ȯ�Ѵ�.
    /// </summary>
    public Dictionary<ElementalType, GameObject> GetEffectsByType(EffectType type)
    {
        if (type == EffectType.Count) return null;
        return EffectContainer[(int)type];
    }

    /// <summary>
    /// ��� ��Ģ�� �����Ѵ�. (��: "Prefabs/Elemental/Hit_Fire")
    /// </summary>
    private string GetEffectPath(EffectType efType, ElementalType elType)
    {
        string path = ELEMENTAL_PRELINK + efType + "_" + elType;
        return path;
    }
}
