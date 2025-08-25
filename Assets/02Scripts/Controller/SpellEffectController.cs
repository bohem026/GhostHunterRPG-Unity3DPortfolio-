using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellEffectController : MonoBehaviour
{
    public enum SpellType { Self, Cast, Pmpt }

    [Header("GLOBAL ID")]
    [SerializeField] private SkillWindowController.SKType outer;
    [SerializeField] private SkillWindowController.ELType inner;

    [Space(10)]
    [Header("BASIC STAT")]
    [SerializeField] private SpellType type;
    [SerializeField] private int id;
    [SerializeField] private int level;       // MAX LEVEL(Self: 5, Cast: 3)
    [SerializeField] private float cost;      // 정수값 사용 권장
    [SerializeField] private float duration;  // Self/Cast 지속시간
    [SerializeField] private bool needNewClip; // Pmpt 전용(애니 교체 필요 여부)

    [Space(10)]
    [Header("DAMAGE")]
    [SerializeField] private float damageRate;
    [SerializeField] private float damageInterval; // Cast 틱 간격
    [SerializeField] private int damageCount;      // Pmpt 다타수
    private Dictionary<Collider, float> damageAbles;

    [Space(10)]
    [Header("SpellPicker")]
    [SerializeField] private GameObject spellPicker;

    private PlayerController _plyCtrl;
    private ElementalController _elCtrl;

    /// <summary>플레이어/원소 컨트롤러 참조 및 내부 자료구조 초기화.</summary>
    private void Awake()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _elCtrl = GetComponent<ElementalController>();
        damageAbles = new Dictionary<Collider, float>();
    }

    /// <summary>
    /// Cast: 진입 시 1회 피해 적용, 추적 시작.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Monster")) return;

        if (type == SpellType.Cast)
        {
            ApplySDamageInfoToMonster(other.GetComponent<MonsterController>(), damageRate);
            damageAbles[other] = 0f;
        }
    }

    /// <summary>
    /// Cast: 틱 간격 누적 후 주기적 피해 적용.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (type != SpellType.Cast) return;
        if (!damageAbles.ContainsKey(other)) return;

        damageAbles[other] += Time.deltaTime;
        if (damageAbles[other] >= damageInterval)
        {
            damageAbles[other] = 0f;
            ApplySDamageInfoToMonster(other.GetComponent<MonsterController>(), damageRate);
        }
    }

    /// <summary>
    /// Cast: 범위 이탈 시 추적 해제.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Monster")) return;
        if (type == SpellType.Cast) damageAbles.Remove(other);
    }

    /// <summary>
    /// 스펠 피해를 대상 몬스터에 적용하고 히트/원소 효과를 처리한다.
    /// </summary>
    public void ApplySDamageInfoToMonster(MonsterController mon, float rate)
    {
        Calculator.DamageInfo info;

        // 1) 피해 계산/적용
        mon.OnDamage(info = Calculator.SpellDamage(
            _plyCtrl.GetComponent<StatController>(),
            mon.GetComponent<StatController>(),
            rate));

        // 2) 히트 이펙트
        if (info.Type == DamageTextController.DamageType.Normal
            || info.Type == DamageTextController.DamageType.Critical)
        {
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                mon.transform,
                _elCtrl.GetAsset.GetELTYPE(),
                info.Type));
        }

        // 3) 원소 지속 피해 트리거(확률)
        if (Calculator.CheckElementalAttackable(_elCtrl, mon.GetComponent<StatController>()))
        {
            mon.GetComponent<StatController>().OnELDamage(_elCtrl);
        }
    }

    public SkillWindowController.SKType OUTER => outer;
    public SkillWindowController.ELType INNER => inner;
    public SpellType Type => type;
    public int ID => id;
    public int GetSpellLevel => level;
    public float Cost => cost;
    public float Duration => duration;
    public bool NeedNewClip => needNewClip;
    public float DamageRate => damageRate;
    public float DamageInterval => damageInterval;
    public int DamageCount => damageCount;
    public GameObject SpellPicker => type == SpellType.Cast ? spellPicker : null;
}
