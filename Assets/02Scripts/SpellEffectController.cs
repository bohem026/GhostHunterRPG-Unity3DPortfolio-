using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Animations.SpringBones.GameObjectExtensions;
using Unity.VisualScripting;
using UnityEngine;
using static SpellEffectController;
//using static UnityEditor.PlayerSettings;

public class SpellEffectController : MonoBehaviour
{
    public enum SpellType
    { Self, Cast, Pmpt }

    [Header("GLOBAL ID")]
    [SerializeField] private SkillWindowController.SKType outer;
    [SerializeField] private SkillWindowController.ELType inner;

    [Space(10)]
    [Header("BASIC STAT")]
    [SerializeField] private SpellType type;
    [SerializeField] private int id;
    [SerializeField] private int level;             // MAX LEVEL(Self: 5, Cast: 3)
    [SerializeField] private float cost;            // !! Integer value only !!
    [SerializeField] private float duration;        // For cast, self type.
    [SerializeField] private bool needNewClip;      // !! Pmpt type only !!

    [Space(10)]
    [Header("DAMAGE")]
    [SerializeField] private float damageRate;
    [SerializeField] private float damageInterval;  // For cast type.
    [SerializeField] private int damageCount;       // For prompt type.
    Dictionary<Collider, float> damageAbles;

    //[Space(30)]
    //[Header("ELEMENT")]
    //[SerializeField] ScriptableObject elementSO;

    [Space(10)]
    [Header("SpellPicker")]
    [SerializeField] private GameObject spellPicker;

    /*Test*/
    PlayerController _plyCtrl;
    ElementalController _elCtrl;
    /*Test*/

    // Start is called before the first frame update
    void Awake()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _elCtrl = this.GetComponent<ElementalController>();

        damageAbles = new Dictionary<Collider, float>();
    }

    // Damage System: CAST_ Trigger Enter
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Monster")) return;

        if (type == SpellType.Cast)
            ApplySDamageInfoToMonster(
                other.GetComponent<MonsterController>()
                , damageRate);

        switch (type)
        {
            case SpellType.Cast:
            case SpellType.Pmpt:
                damageAbles[other] = 0f;
                break;
        }
    }

    // Damage System: CAST_ Trigger Stay
    private void OnTriggerStay(Collider other)
    {
        switch (type)
        {
            case SpellType.Cast:
                if (!damageAbles.ContainsKey(other)) return;

                // Timer
                damageAbles[other] += Time.deltaTime;

                if (damageAbles[other] >= damageInterval)
                {
                    damageAbles[other] = 0f;

                    ApplySDamageInfoToMonster(
                        other.GetComponent<MonsterController>()
                        , damageRate);
                }
                break;
            case SpellType.Pmpt:
            case SpellType.Self:
                break;
        }
    }

    // Damage System: CAST_ Trigger Exit
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Monster"))
            return;

        switch (type)
        {
            case SpellType.Cast:
            case SpellType.Pmpt:
                damageAbles.Remove(other);
                break;
            case SpellType.Self:
                break;
        }
    }

    /*!!NOTE!!*/
    //스펠 대미지 비율 매개변수(rate) 필요한가?
    public void ApplySDamageInfoToMonster
        (
        MonsterController mon
        , float rate
        )
    {
        Calculator.DamageInfo info;

        //1. Apply damage value.
        mon.OnDamage(info = Calculator.SpellDamage(
            this._plyCtrl.GetComponent<StatController>()
            , mon.GetComponent<StatController>()
            , rate));

        //2. Instantiate hit effect.
        if (info.Type == DamageTextController.DamageType.Normal
            || info.Type == DamageTextController.DamageType.Critical)
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                                mon.transform
                                , _elCtrl.GetAsset.GetELTYPE()
                                , info.Type));

        //3. Activate elemental damage loop by odds.
        if (Calculator.CheckElementalAttackable(
            _elCtrl
            , mon.GetComponent<StatController>()))
            mon.GetComponent<StatController>().OnELDamage(_elCtrl);
    }

    #region --- [GET]
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

    public GameObject SpellPicker =>
        type == SpellType.Cast ? spellPicker : null;
    #endregion
}
