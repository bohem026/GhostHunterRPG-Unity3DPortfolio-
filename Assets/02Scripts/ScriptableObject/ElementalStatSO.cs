using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Scriptable Object/Elemental Stat Asset")]
public class ElementalStatSO : ScriptableObject
{
    [Header("STAT: BASE")]
    [SerializeField] private ElementalManager.EffectType efType;
    [SerializeField] private ElementalManager.ElementalType elType;
    [SerializeField] private float baseDMGR;   // 대미지
    [SerializeField] private float baseDUR;    // 지속 시간
    [SerializeField] private float baseITV;    // 공격 간 간격
    [SerializeField] private float baseODDS;   // 발동 확률

    [Header("STAT: PER LEVEL")]
    [SerializeField] private float perDMGR;
    [SerializeField] private float perDUR;
    [SerializeField] private float perITV;
    [SerializeField] private float perODDS;

    /// <summary>Spell 타입 몬스터 전용 이펙트(페인트볼) 프리셋.</summary>
    [Header("PAINTBALL")]
    [SerializeField] private GameObject paintball;
    [SerializeField] private Texture2D[] paintballTextures;

    #region GET
    public ElementalManager.EffectType GetEFTYPE() => efType;
    public ElementalManager.ElementalType GetELTYPE() => elType;

    /// <summary>레벨에 따른 대미지 비율 반환.</summary>
    public float GetDMGR(int level) => baseDMGR + perDMGR * level;

    /// <summary>레벨에 따른 지속 시간 반환.</summary>
    public float GetDUR(int level) => baseDUR + perDUR * level;

    /// <summary>레벨에 따른 타격 간격 반환.</summary>
    public float GetITV(int level) => baseITV + perITV * level;

    /// <summary>레벨에 따른 발동 확률 반환.</summary>
    public float GetODDS(int level) => baseODDS + perODDS * level;

    /// <summary>무작위 텍스처를 적용한 페인트볼 프리팹 반환.</summary>
    public GameObject GetPaintball()
    {
        paintball.GetComponent<RawImage>().texture =
            paintballTextures[Random.Range(0, paintballTextures.Length)];
        return paintball;
    }
    #endregion
}
