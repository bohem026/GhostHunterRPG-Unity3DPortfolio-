using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Scriptable Object/Elemental Stat Asset")]
public class ElementalStatSO : ScriptableObject
{
    /*!!Note!!*/
    //레벨마다 상승치 제거(Per)
    //스킬 [속성 연구]값 가져와서 합산할 것!

    [Header("STAT: BASE")]
    [SerializeField] private ElementalManager.EffectType efType;
    [SerializeField] private ElementalManager.ElementalType elType;
    [SerializeField] private float baseDMGR;     // 대미지
    [SerializeField] private float baseDUR;     // 지속 시간
    [SerializeField] private float baseITV;     // 공격 간 간격
    [SerializeField] private float baseODDS;     // 확률

    [Header("STAT: PER LEVEL")]
    [SerializeField] private float perDMGR;
    [SerializeField] private float perDUR;
    [SerializeField] private float perITV;
    [SerializeField] private float perODDS;

    /// <summary>
    /// Spell 타입 몬스터 전용입니다.
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
