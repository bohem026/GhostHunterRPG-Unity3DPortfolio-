using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Scriptable Object/Elemental Stat Asset")]
public class ElementalStatSO : ScriptableObject
{
    /*!!Note!!*/
    //�������� ���ġ ����(Per)
    //��ų [�Ӽ� ����]�� �����ͼ� �ջ��� ��!

    [Header("STAT: BASE")]
    [SerializeField] private ElementalManager.EffectType efType;
    [SerializeField] private ElementalManager.ElementalType elType;
    [SerializeField] private float baseDMGR;     // �����
    [SerializeField] private float baseDUR;     // ���� �ð�
    [SerializeField] private float baseITV;     // ���� �� ����
    [SerializeField] private float baseODDS;     // Ȯ��

    [Header("STAT: PER LEVEL")]
    [SerializeField] private float perDMGR;
    [SerializeField] private float perDUR;
    [SerializeField] private float perITV;
    [SerializeField] private float perODDS;

    /// <summary>
    /// Spell Ÿ�� ���� �����Դϴ�.
    /// </summary>
    [Header("PAINTBALL")]
    [SerializeField] private GameObject paintball;
    [SerializeField] private Texture2D[] paintballTextures;

    #region Get
    public ElementalManager.EffectType GetEFTYPE() => efType;
    public ElementalManager.ElementalType GetELTYPE() => elType;
    public float GetDMGR(int level) => baseDMGR + perDMGR * level;
    public float GetDUR(int level) => baseDUR + perDUR * level;
    public float GetITV(int level) => baseITV + perITV * level;
    public float GetODDS(int level) => baseODDS + perODDS * level;
    public GameObject GetPaintball()
    {
        paintball.GetComponent<RawImage>().texture = paintballTextures[Random.Range(0, paintballTextures.Length)];
        return paintball;
    }
    #endregion
}
