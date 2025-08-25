using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Scriptable Object/Elemental Stat Asset")]
public class ElementalStatSO : ScriptableObject
{
    [Header("STAT: BASE")]
    [SerializeField] private ElementalManager.EffectType efType;
    [SerializeField] private ElementalManager.ElementalType elType;
    [SerializeField] private float baseDMGR;   // �����
    [SerializeField] private float baseDUR;    // ���� �ð�
    [SerializeField] private float baseITV;    // ���� �� ����
    [SerializeField] private float baseODDS;   // �ߵ� Ȯ��

    [Header("STAT: PER LEVEL")]
    [SerializeField] private float perDMGR;
    [SerializeField] private float perDUR;
    [SerializeField] private float perITV;
    [SerializeField] private float perODDS;

    /// <summary>Spell Ÿ�� ���� ���� ����Ʈ(����Ʈ��) ������.</summary>
    [Header("PAINTBALL")]
    [SerializeField] private GameObject paintball;
    [SerializeField] private Texture2D[] paintballTextures;

    #region GET
    public ElementalManager.EffectType GetEFTYPE() => efType;
    public ElementalManager.ElementalType GetELTYPE() => elType;

    /// <summary>������ ���� ����� ���� ��ȯ.</summary>
    public float GetDMGR(int level) => baseDMGR + perDMGR * level;

    /// <summary>������ ���� ���� �ð� ��ȯ.</summary>
    public float GetDUR(int level) => baseDUR + perDUR * level;

    /// <summary>������ ���� Ÿ�� ���� ��ȯ.</summary>
    public float GetITV(int level) => baseITV + perITV * level;

    /// <summary>������ ���� �ߵ� Ȯ�� ��ȯ.</summary>
    public float GetODDS(int level) => baseODDS + perODDS * level;

    /// <summary>������ �ؽ�ó�� ������ ����Ʈ�� ������ ��ȯ.</summary>
    public GameObject GetPaintball()
    {
        paintball.GetComponent<RawImage>().texture =
            paintballTextures[Random.Range(0, paintballTextures.Length)];
        return paintball;
    }
    #endregion
}
