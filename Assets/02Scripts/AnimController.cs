using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class AnimController : MonoBehaviour
{
    [HideInInspector]
    public enum AnimState
    {
        // Please set enum name of 'AnimState' as corresponding animation trigger's name.
        Idle,       // Basic move blend tree
        NAttack0,
        NAttack1,
        SAttack,
        SpSelf,
        SpCast,
        SpPmpt
    }
    [HideInInspector]
    public AnimState m_PreState = AnimState.Idle;

    //--- [Components]
    PlayerController _plyCtrl = null;
    Animator _animator = null;
    AnimatorOverrideController _animOverrideCtrl = null;
    StatController _statCtrl = null;
    WeaponController _wpnCtrl = null;
    MeleeController _meleeCtrl = null;
    SpellController _spellCtrl = null;

    //--- [Melee: NAttack]
    bool isMouse0Hold = false;

    //--- [Flag]
    bool isStillSA = false;
    private bool isClipChanged = false;

    /*Test*/
    [Space(20)]
    [Header("Animation clip")]
    public AnimationClip clipSpPmptNorm;
    public AnimationClip clipSpPmptFast;
    [Header("Audio clip")]
    public AudioClip sfxClip_NAttack;
    public AudioClip sfxClip_SAttack;
    /*Test*/

    // Start is called before the first frame update
    void Start()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _animator = _plyCtrl.GetComponent<Animator>();
        /*Test*/
        _animOverrideCtrl = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        /*Test*/
        _statCtrl = _plyCtrl.GetComponent<StatController>();
        _wpnCtrl = _plyCtrl.GetComponent<WeaponController>();
        _meleeCtrl = _wpnCtrl.GetMeleeController();
        _spellCtrl = _wpnCtrl.GetSpellController();
    }

    // Update is called once per frame
    void Update()
    {
        if (!_spellCtrl.IsInitialized) return;

        Play_NAtkAnim();
        Play_SAtkAnim();
        Play_SpSelfAnim();
        Play_SpCastAnim();
    }

    #region --- [Update func]

    public void Play_NAtkAnim()
    {
        if (_plyCtrl.isNAttack && m_PreState == AnimState.Idle)
        {
#if UNITY_ANDROID || UNITY_IOS                           
            _plyCtrl.isNAttack = false;
#endif
            isMouse0Hold = true;
            ChangeAnimState(AnimState.NAttack0);
        }
        else
        {
            isMouse0Hold = false;
        }
    }

    public void Play_SAtkAnim()
    {
        if (_plyCtrl.isSAttack && m_PreState == AnimState.Idle)
        {
#if UNITY_ANDROID || UNITY_IOS
            _plyCtrl.isSAttack = false;
#endif
            isStillSA = true;
            ChangeAnimState(AnimState.SAttack);
        }
    }

    CurrentSpellInfo QInfo;
    SpellEffectController QSpellEffect;
    public void Play_SpSelfAnim()
    {
        GlobalValue.SkillOrder Q = GlobalValue.SkillOrder.Q;
        QInfo = _spellCtrl.Info(Q);

        //Return if spell is not equipped.
        if (!QInfo.Effect) return;
        QSpellEffect = QInfo.Effect.GetComponent<SpellEffectController>();

        //Return if spell is unavailable.
        if (!UIManager.Inst.IsDelayOver(Q) ||
            _statCtrl.MPCurrent < QSpellEffect.Cost)
            return;

        if (_plyCtrl.isSpSelf && m_PreState == AnimState.Idle)
        {
            ChangeAnimState(AnimState.SpSelf);
        }
    }



    CurrentSpellInfo EInfo;
    SpellEffectController ESpellEffect;
    public void Play_SpCastAnim()
    {
        GlobalValue.SkillOrder E = GlobalValue.SkillOrder.E;
        EInfo = _spellCtrl.Info(E);

        //Return if spell is not equipped.
        if (!EInfo.Effect) return;
        ESpellEffect = EInfo.Effect.GetComponent<SpellEffectController>();

        //Return if spell is unavailable.
        if (!UIManager.Inst.IsDelayOver(E) ||
            _statCtrl.MPCurrent < ESpellEffect.Cost)
            return;

        if (_plyCtrl.isSpCast && m_PreState == AnimState.Idle)
        {
            //CAST
            if (ESpellEffect.Type == SpellEffectController.SpellType.Cast)
            {
                ChangeAnimState(AnimState.SpCast);
            }
            //PMPT
            else if (ESpellEffect.Type == SpellEffectController.SpellType.Pmpt)
            {
                if (MonsterManager.Inst.InstMonsAimed() == 0)
                {
                    Debug.Log("대상 없음");
                    return;
                }

                ChangeAnimState(AnimState.SpPmpt);
            }
        }
    }

    public void ChangeAnimState(AnimState newState)
    {
        if (_animator == null) return;
        //Check is same animation.
        if (m_PreState == newState) return;
        //Check is character dead.
        if (!_plyCtrl.inputEnabled)
        {
            _animator.SetFloat("VAxis", 0f);
            _animator.SetFloat("HAxis", 0f);
            _animator.Play(AnimState.Idle.ToString(), 0, 0f);
            return;
        }

        _animator.ResetTrigger(m_PreState.ToString());
        _animator.SetTrigger(newState.ToString());
        m_PreState = newState;

        /*Test*/
        if (m_PreState == AnimState.SpPmpt)
            ChangeSpPmptClip();
        /*Test*/
    }

    private void ChangeSpPmptClip()
    {
        CurrentSpellInfo info = _spellCtrl.Info(GlobalValue.SkillOrder.E);
        SpellEffectController spellEffect
            = info.Effect.GetComponent<SpellEffectController>();

        if (spellEffect.NeedNewClip)
        {
            if (isClipChanged) return;
            _animOverrideCtrl[clipSpPmptNorm.name] = clipSpPmptFast;
            isClipChanged = true;
        }
        else
        {
            if (!isClipChanged) return;
            _animOverrideCtrl[clipSpPmptNorm.name] = clipSpPmptNorm;
            isClipChanged = false;
        }

        _animator.runtimeAnimatorController = _animOverrideCtrl;
    }

    #endregion

    #region --- [Anim event func]

    //이펙트 및 피격 처리 메서드
    void Event_NAtk0Hit()
    {
        //Play SFX: NAttack0.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce
        (AudioPlayerPoolManager.SFXPoolType.Player,
        sfxClip_NAttack);

        AnimState state = AnimState.NAttack0;
        _meleeCtrl.InstSlashEffect(state);

        if (MonsterManager.Inst.MONS_LEFT)
        {
            foreach (MonsterController mon in MonsterManager.Inst.monsTotal)
            {
                if (mon.isTarget)
                {
                    ApplyDamageInfoByState(mon, state);
                }
            }
        }
    }

    //공격 종료 이벤트 처리 메서드
    void Event_NAtk0Finish()
    {
    }

    void Event_NAtk1Hit()
    {
        //Play SFX: NAttack1.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce
        (AudioPlayerPoolManager.SFXPoolType.Player,
        sfxClip_NAttack);

        AnimState state = AnimState.NAttack1;
        _meleeCtrl.InstSlashEffect(state);

        if (MonsterManager.Inst.MONS_LEFT)
        {
            foreach (MonsterController mon in MonsterManager.Inst.monsTotal)
            {
                if (mon.isTarget)
                {
                    ApplyDamageInfoByState(mon, state);
                }
            }
        }
    }

    void Event_NAtk1Finish()
    {
        //Skill 상태인데 Attack애니메이션 끝이 들어온 경우라면 제외시켜버린다.
        //공격 애니 중에 스킬 발동시 공격 끝나는 이벤트 함수가 들어와서 스킬이
        //취소되는 현상이 있을 수 있어서 예외 처리함
        //Skill상태일 때는 Skill상태로 끝나야 한다.
        if (m_PreState != AnimState.NAttack0)
            return;

        if (isMouse0Hold == true)
        {
            m_PreState = AnimState.NAttack1;
            ChangeAnimState(AnimState.NAttack0);
        }
        else
        {
            ChangeAnimState(AnimState.Idle);
        }
    }

    void Event_SAtkHit()
    {
        //Play SFX: SAttack.
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce
        (AudioPlayerPoolManager.SFXPoolType.Player,
        sfxClip_SAttack);

        AnimState state = AnimState.SAttack;
        _meleeCtrl.InstSlashEffect(m_PreState);

        if (MonsterManager.Inst.MONS_LEFT)
        {
            foreach (MonsterController mon in MonsterManager.Inst.monsTotal)
            {
                if (mon.isTarget)
                {
                    ApplyDamageInfoByState(mon, state);
                }
            }
        }
    }

    private void ApplyDamageInfoByState
        (
        MonsterController mon,
        AnimState state
        )
    {
        Calculator.DamageInfo info;

        //1. Apply damage value.
        mon.OnDamage(info = Calculator.MeleeDamage(
                        GetComponent<StatController>()
                        , mon.GetComponent<StatController>()
                        , _meleeCtrl.GetDamageRate(state)));

        //2. Instantiate hit effect.
        if (info.Type == DamageTextController.DamageType.Normal
            || info.Type == DamageTextController.DamageType.Critical)
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                                mon.transform
                                , _meleeCtrl.GetAsset.ElType
                                , info.Type));
    }

    void Event_SAtkFinish()
    {
        if (m_PreState != AnimState.SAttack)
            return;

        isStillSA = false;
        ChangeAnimState(AnimState.Idle);
    }

    void Event_SpSelfHit()
    {
        //_spellCtrl.InstSpellEffect();        
        _spellCtrl.InstSpellEffect(GlobalValue.SkillOrder.Q);
    }

    void Event_SpSelfFinish()
    {
        if (m_PreState != AnimState.SpSelf)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    void Event_SpPmptHit()
    {
        //_spellCtrl.InstSpellEffect();
        _spellCtrl.InstSpellEffect(GlobalValue.SkillOrder.E);
    }

    void Event_SpPmptFinish()
    {
        if (m_PreState != AnimState.SpPmpt)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    void Event_SpCastHit()
    {
        _spellCtrl.InstDelayedSpellEffect(GlobalValue.SkillOrder.E);
    }

    void Event_SpCastFinish()
    {
        if (m_PreState != AnimState.SpCast)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    #endregion

    public bool IsStillNA => isMouse0Hold;
    public bool IsStillSA => isStillSA;
}
