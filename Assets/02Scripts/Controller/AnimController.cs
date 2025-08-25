using UnityEngine;

public class AnimController : MonoBehaviour
{
    [HideInInspector]
    public enum AnimState
    {
        // 애니메이터 트리거와 동일한 이름 유지
        Idle,       // 기본 이동 블렌드 트리
        NAttack0,
        NAttack1,
        SAttack,
        SpSelf,
        SpCast,
        SpPmpt
    }

    [HideInInspector] public AnimState m_PreState = AnimState.Idle;

    // --- Components ---
    private PlayerController _plyCtrl = null;
    private Animator _animator = null;
    private AnimatorOverrideController _animOverrideCtrl = null;
    private StatController _statCtrl = null;
    private WeaponController _wpnCtrl = null;
    private MeleeController _meleeCtrl = null;
    private SpellController _spellCtrl = null;

    // --- Melee: NAttack ---
    private bool isMouse0Hold = false;

    // --- Flags ---
    private bool isStillSA = false;
    private bool isClipChanged = false;

    [Space(20)]
    [Header("Animation clip")]
    public AnimationClip clipSpPmptNorm;
    public AnimationClip clipSpPmptFast;

    [Header("Audio clip")]
    public AudioClip sfxClip_NAttack;
    public AudioClip sfxClip_SAttack;

    /// <summary>
    /// 필요한 컴포넌트 캐싱 및 애니메이터 오버라이드 컨트롤러 생성.
    /// </summary>
    private void Start()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _animator = _plyCtrl.GetComponent<Animator>();
        _animOverrideCtrl = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _statCtrl = _plyCtrl.GetComponent<StatController>();
        _wpnCtrl = _plyCtrl.GetComponent<WeaponController>();
        _meleeCtrl = _wpnCtrl.GetMeleeController();
        _spellCtrl = _wpnCtrl.GetSpellController();
    }

    /// <summary>
    /// 입력 상태를 읽고 각 공격/스킬 애니메이션 트리거를 처리.
    /// </summary>
    private void Update()
    {
        if (!_spellCtrl.IsInitialized) return;

        Play_NAtkAnim();
        Play_SAtkAnim();
        Play_SpSelfAnim();
        Play_SpCastAnim();
    }

    #region --- [Update func]

    /// <summary>
    /// 일반 공격 입력 처리(연타 유지 포함).
    /// </summary>
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

    /// <summary>
    /// 강공격 입력 처리(한 번 발동).
    /// </summary>
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

    // 자기강화/즉시형(Q)
    private CurrentSpellInfo QInfo;
    private SpellEffectController QSpellEffect;

    /// <summary>
    /// Q(자기버프/즉시 시전) 스킬 입력 처리(쿨다운/MP 체크 포함).
    /// </summary>
    public void Play_SpSelfAnim()
    {
        var Q = GlobalValue.SkillOrder.Q;
        QInfo = _spellCtrl.Info(Q);

        // 미장착 스킬이면 중단
        if (!QInfo.Effect) return;
        QSpellEffect = QInfo.Effect.GetComponent<SpellEffectController>();

        // 쿨다운/MP 부족 시 중단
        if (!UIManager.Inst.IsDelayOver(Q) || _statCtrl.MPCurrent < QSpellEffect.Cost)
            return;

        if (_plyCtrl.isSpSelf && m_PreState == AnimState.Idle)
        {
            ChangeAnimState(AnimState.SpSelf);
        }
    }

    // 투사/조준형(E)
    private CurrentSpellInfo EInfo;
    private SpellEffectController ESpellEffect;

    /// <summary>
    /// E(캐스팅/조준) 스킬 입력 처리(유형에 따라 SpCast/SpPmpt 트리거).
    /// </summary>
    public void Play_SpCastAnim()
    {
        var E = GlobalValue.SkillOrder.E;
        EInfo = _spellCtrl.Info(E);

        // 미장착 스킬이면 중단
        if (!EInfo.Effect) return;
        ESpellEffect = EInfo.Effect.GetComponent<SpellEffectController>();

        // 쿨다운/MP 부족 시 중단
        if (!UIManager.Inst.IsDelayOver(E) || _statCtrl.MPCurrent < ESpellEffect.Cost)
            return;

        if (_plyCtrl.isSpCast && m_PreState == AnimState.Idle)
        {
            // CAST 타입
            if (ESpellEffect.Type == SpellEffectController.SpellType.Cast)
            {
                ChangeAnimState(AnimState.SpCast);
            }
            // PMPT 타입(대상 필요)
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

    /// <summary>
    /// 애니메이션 상태 전환(이전 트리거 리셋 및 입력 불가 시 예외 처리 포함).
    /// </summary>
    public void ChangeAnimState(AnimState newState)
    {
        if (_animator == null) return;
        if (m_PreState == newState) return; // 동일 상태면 무시

        // 캐릭터 사망/입력불가 시 강제로 Idle 재생
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

        // PMPT의 속도 조건에 따라 클립 교체 필요 시 반영
        if (m_PreState == AnimState.SpPmpt)
            ChangeSpPmptClip();
    }

    /// <summary>
    /// PMPT 시전 애니메이션을 조건에 따라 빠른/기본 클립으로 교체.
    /// </summary>
    private void ChangeSpPmptClip()
    {
        var info = _spellCtrl.Info(GlobalValue.SkillOrder.E);
        var spellEffect = info.Effect.GetComponent<SpellEffectController>();

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

    /// <summary>
    /// NAttack0 타격 이벤트: 효과/사운드/피해 적용.
    /// </summary>
    private void Event_NAtk0Hit()
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.Player, sfxClip_NAttack);

        var state = AnimState.NAttack0;
        _meleeCtrl.InstSlashEffect(state);

        if (MonsterManager.Inst.MONS_LEFT)
        {
            foreach (var mon in MonsterManager.Inst.monsTotal)
                if (mon.isTarget) ApplyDamageInfoByState(mon, state);
        }
    }

    private void Event_NAtk0Finish() { /* 필요 시 확장 포인트 */ }

    /// <summary>
    /// NAttack1 타격 이벤트: 효과/사운드/피해 적용.
    /// </summary>
    private void Event_NAtk1Hit()
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.Player, sfxClip_NAttack);

        var state = AnimState.NAttack1;
        _meleeCtrl.InstSlashEffect(state);

        if (MonsterManager.Inst.MONS_LEFT)
        {
            foreach (var mon in MonsterManager.Inst.monsTotal)
                if (mon.isTarget) ApplyDamageInfoByState(mon, state);
        }
    }

    /// <summary>
    /// NAttack1 종료 이벤트: 연타 유지 시 재진입, 아니면 Idle로 복귀.
    /// </summary>
    private void Event_NAtk1Finish()
    {
        // 스킬 중 공격 종료 이벤트가 들어오면 취소되지 않도록 보호
        if (m_PreState != AnimState.NAttack0)
            return;

        if (isMouse0Hold)
        {
            m_PreState = AnimState.NAttack1;
            ChangeAnimState(AnimState.NAttack0);
        }
        else
        {
            ChangeAnimState(AnimState.Idle);
        }
    }

    /// <summary>
    /// SAttack 타격 이벤트: 효과/사운드/피해 적용.
    /// </summary>
    private void Event_SAtkHit()
    {
        AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
            AudioPlayerPoolManager.SFXPoolType.Player, sfxClip_SAttack);

        var state = AnimState.SAttack;
        _meleeCtrl.InstSlashEffect(m_PreState);

        if (MonsterManager.Inst.MONS_LEFT)
        {
            foreach (var mon in MonsterManager.Inst.monsTotal)
                if (mon.isTarget) ApplyDamageInfoByState(mon, state);
        }
    }

    /// <summary>
    /// 근접 피해 계산 및 피격 연출 트리거.
    /// </summary>
    private void ApplyDamageInfoByState(MonsterController mon, AnimState state)
    {
        Calculator.DamageInfo info;

        // 1) 데미지 계산
        mon.OnDamage(info = Calculator.MeleeDamage(
            GetComponent<StatController>(),
            mon.GetComponent<StatController>(),
            _meleeCtrl.GetDamageRate(state)));

        // 2) 히트 이펙트(일반/크리티컬만)
        if (info.Type == DamageTextController.DamageType.Normal ||
            info.Type == DamageTextController.DamageType.Critical)
        {
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                mon.transform,
                _meleeCtrl.GetAsset.ElType,
                info.Type));
        }
    }

    /// <summary>
    /// SAttack 종료 이벤트: 상태 복귀.
    /// </summary>
    private void Event_SAtkFinish()
    {
        if (m_PreState != AnimState.SAttack)
            return;

        isStillSA = false;
        ChangeAnimState(AnimState.Idle);
    }

    /// <summary>
    /// SpSelf 타격 이벤트: Q 스킬 이펙트 생성.
    /// </summary>
    private void Event_SpSelfHit()
    {
        _spellCtrl.InstSpellEffect(GlobalValue.SkillOrder.Q);
    }

    /// <summary>
    /// SpSelf 종료 이벤트: 상태 복귀.
    /// </summary>
    private void Event_SpSelfFinish()
    {
        if (m_PreState != AnimState.SpSelf)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    /// <summary>
    /// SpPmpt 타격 이벤트: E 스킬 이펙트 생성(조준형).
    /// </summary>
    private void Event_SpPmptHit()
    {
        _spellCtrl.InstSpellEffect(GlobalValue.SkillOrder.E);
    }

    /// <summary>
    /// SpPmpt 종료 이벤트: 상태 복귀.
    /// </summary>
    private void Event_SpPmptFinish()
    {
        if (m_PreState != AnimState.SpPmpt)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    /// <summary>
    /// SpCast 타격 이벤트: E 지연형 스킬 이펙트 생성.
    /// </summary>
    private void Event_SpCastHit()
    {
        _spellCtrl.InstDelayedSpellEffect(GlobalValue.SkillOrder.E);
    }

    /// <summary>
    /// SpCast 종료 이벤트: 상태 복귀.
    /// </summary>
    private void Event_SpCastFinish()
    {
        if (m_PreState != AnimState.SpCast)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    #endregion

    // --- Properties ---
    public bool IsStillNA => isMouse0Hold;
    public bool IsStillSA => isStillSA;
}
