using UnityEngine;

public class AnimController : MonoBehaviour
{
    [HideInInspector]
    public enum AnimState
    {
        // �ִϸ����� Ʈ���ſ� ������ �̸� ����
        Idle,       // �⺻ �̵� ���� Ʈ��
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
    /// �ʿ��� ������Ʈ ĳ�� �� �ִϸ����� �������̵� ��Ʈ�ѷ� ����.
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
    /// �Է� ���¸� �а� �� ����/��ų �ִϸ��̼� Ʈ���Ÿ� ó��.
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
    /// �Ϲ� ���� �Է� ó��(��Ÿ ���� ����).
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
    /// ������ �Է� ó��(�� �� �ߵ�).
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

    // �ڱⰭȭ/�����(Q)
    private CurrentSpellInfo QInfo;
    private SpellEffectController QSpellEffect;

    /// <summary>
    /// Q(�ڱ����/��� ����) ��ų �Է� ó��(��ٿ�/MP üũ ����).
    /// </summary>
    public void Play_SpSelfAnim()
    {
        var Q = GlobalValue.SkillOrder.Q;
        QInfo = _spellCtrl.Info(Q);

        // ������ ��ų�̸� �ߴ�
        if (!QInfo.Effect) return;
        QSpellEffect = QInfo.Effect.GetComponent<SpellEffectController>();

        // ��ٿ�/MP ���� �� �ߴ�
        if (!UIManager.Inst.IsDelayOver(Q) || _statCtrl.MPCurrent < QSpellEffect.Cost)
            return;

        if (_plyCtrl.isSpSelf && m_PreState == AnimState.Idle)
        {
            ChangeAnimState(AnimState.SpSelf);
        }
    }

    // ����/������(E)
    private CurrentSpellInfo EInfo;
    private SpellEffectController ESpellEffect;

    /// <summary>
    /// E(ĳ����/����) ��ų �Է� ó��(������ ���� SpCast/SpPmpt Ʈ����).
    /// </summary>
    public void Play_SpCastAnim()
    {
        var E = GlobalValue.SkillOrder.E;
        EInfo = _spellCtrl.Info(E);

        // ������ ��ų�̸� �ߴ�
        if (!EInfo.Effect) return;
        ESpellEffect = EInfo.Effect.GetComponent<SpellEffectController>();

        // ��ٿ�/MP ���� �� �ߴ�
        if (!UIManager.Inst.IsDelayOver(E) || _statCtrl.MPCurrent < ESpellEffect.Cost)
            return;

        if (_plyCtrl.isSpCast && m_PreState == AnimState.Idle)
        {
            // CAST Ÿ��
            if (ESpellEffect.Type == SpellEffectController.SpellType.Cast)
            {
                ChangeAnimState(AnimState.SpCast);
            }
            // PMPT Ÿ��(��� �ʿ�)
            else if (ESpellEffect.Type == SpellEffectController.SpellType.Pmpt)
            {
                if (MonsterManager.Inst.InstMonsAimed() == 0)
                {
                    Debug.Log("��� ����");
                    return;
                }
                ChangeAnimState(AnimState.SpPmpt);
            }
        }
    }

    /// <summary>
    /// �ִϸ��̼� ���� ��ȯ(���� Ʈ���� ���� �� �Է� �Ұ� �� ���� ó�� ����).
    /// </summary>
    public void ChangeAnimState(AnimState newState)
    {
        if (_animator == null) return;
        if (m_PreState == newState) return; // ���� ���¸� ����

        // ĳ���� ���/�ԷºҰ� �� ������ Idle ���
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

        // PMPT�� �ӵ� ���ǿ� ���� Ŭ�� ��ü �ʿ� �� �ݿ�
        if (m_PreState == AnimState.SpPmpt)
            ChangeSpPmptClip();
    }

    /// <summary>
    /// PMPT ���� �ִϸ��̼��� ���ǿ� ���� ����/�⺻ Ŭ������ ��ü.
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
    /// NAttack0 Ÿ�� �̺�Ʈ: ȿ��/����/���� ����.
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

    private void Event_NAtk0Finish() { /* �ʿ� �� Ȯ�� ����Ʈ */ }

    /// <summary>
    /// NAttack1 Ÿ�� �̺�Ʈ: ȿ��/����/���� ����.
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
    /// NAttack1 ���� �̺�Ʈ: ��Ÿ ���� �� ������, �ƴϸ� Idle�� ����.
    /// </summary>
    private void Event_NAtk1Finish()
    {
        // ��ų �� ���� ���� �̺�Ʈ�� ������ ��ҵ��� �ʵ��� ��ȣ
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
    /// SAttack Ÿ�� �̺�Ʈ: ȿ��/����/���� ����.
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
    /// ���� ���� ��� �� �ǰ� ���� Ʈ����.
    /// </summary>
    private void ApplyDamageInfoByState(MonsterController mon, AnimState state)
    {
        Calculator.DamageInfo info;

        // 1) ������ ���
        mon.OnDamage(info = Calculator.MeleeDamage(
            GetComponent<StatController>(),
            mon.GetComponent<StatController>(),
            _meleeCtrl.GetDamageRate(state)));

        // 2) ��Ʈ ����Ʈ(�Ϲ�/ũ��Ƽ�ø�)
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
    /// SAttack ���� �̺�Ʈ: ���� ����.
    /// </summary>
    private void Event_SAtkFinish()
    {
        if (m_PreState != AnimState.SAttack)
            return;

        isStillSA = false;
        ChangeAnimState(AnimState.Idle);
    }

    /// <summary>
    /// SpSelf Ÿ�� �̺�Ʈ: Q ��ų ����Ʈ ����.
    /// </summary>
    private void Event_SpSelfHit()
    {
        _spellCtrl.InstSpellEffect(GlobalValue.SkillOrder.Q);
    }

    /// <summary>
    /// SpSelf ���� �̺�Ʈ: ���� ����.
    /// </summary>
    private void Event_SpSelfFinish()
    {
        if (m_PreState != AnimState.SpSelf)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    /// <summary>
    /// SpPmpt Ÿ�� �̺�Ʈ: E ��ų ����Ʈ ����(������).
    /// </summary>
    private void Event_SpPmptHit()
    {
        _spellCtrl.InstSpellEffect(GlobalValue.SkillOrder.E);
    }

    /// <summary>
    /// SpPmpt ���� �̺�Ʈ: ���� ����.
    /// </summary>
    private void Event_SpPmptFinish()
    {
        if (m_PreState != AnimState.SpPmpt)
            return;

        ChangeAnimState(AnimState.Idle);
    }

    /// <summary>
    /// SpCast Ÿ�� �̺�Ʈ: E ������ ��ų ����Ʈ ����.
    /// </summary>
    private void Event_SpCastHit()
    {
        _spellCtrl.InstDelayedSpellEffect(GlobalValue.SkillOrder.E);
    }

    /// <summary>
    /// SpCast ���� �̺�Ʈ: ���� ����.
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
