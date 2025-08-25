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
    [SerializeField] private float cost;      // ������ ��� ����
    [SerializeField] private float duration;  // Self/Cast ���ӽð�
    [SerializeField] private bool needNewClip; // Pmpt ����(�ִ� ��ü �ʿ� ����)

    [Space(10)]
    [Header("DAMAGE")]
    [SerializeField] private float damageRate;
    [SerializeField] private float damageInterval; // Cast ƽ ����
    [SerializeField] private int damageCount;      // Pmpt ��Ÿ��
    private Dictionary<Collider, float> damageAbles;

    [Space(10)]
    [Header("SpellPicker")]
    [SerializeField] private GameObject spellPicker;

    private PlayerController _plyCtrl;
    private ElementalController _elCtrl;

    /// <summary>�÷��̾�/���� ��Ʈ�ѷ� ���� �� ���� �ڷᱸ�� �ʱ�ȭ.</summary>
    private void Awake()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _elCtrl = GetComponent<ElementalController>();
        damageAbles = new Dictionary<Collider, float>();
    }

    /// <summary>
    /// Cast: ���� �� 1ȸ ���� ����, ���� ����.
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
    /// Cast: ƽ ���� ���� �� �ֱ��� ���� ����.
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
    /// Cast: ���� ��Ż �� ���� ����.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Monster")) return;
        if (type == SpellType.Cast) damageAbles.Remove(other);
    }

    /// <summary>
    /// ���� ���ظ� ��� ���Ϳ� �����ϰ� ��Ʈ/���� ȿ���� ó���Ѵ�.
    /// </summary>
    public void ApplySDamageInfoToMonster(MonsterController mon, float rate)
    {
        Calculator.DamageInfo info;

        // 1) ���� ���/����
        mon.OnDamage(info = Calculator.SpellDamage(
            _plyCtrl.GetComponent<StatController>(),
            mon.GetComponent<StatController>(),
            rate));

        // 2) ��Ʈ ����Ʈ
        if (info.Type == DamageTextController.DamageType.Normal
            || info.Type == DamageTextController.DamageType.Critical)
        {
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                mon.transform,
                _elCtrl.GetAsset.GetELTYPE(),
                info.Type));
        }

        // 3) ���� ���� ���� Ʈ����(Ȯ��)
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
