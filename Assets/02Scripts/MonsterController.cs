using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    public enum MonType { Melee, Spell, Buff, Count/*Capacity*/ }
    public enum MonFSM { Normal, Hit, Pick }

    [Header("Monster Settings")]
    [SerializeField] private MonType type;
    //[SerializeField] private GameObject hitEffect;
    //사용자를 공격할 때 사용_ Melee 타입만 사용

    [Header("Material")]
    public Material[] mats;

    [Header("Animation clip")]
    public AnimationClip atkAnimClip;

    [Header("Audio clip")]
    public AudioClip sfxClip_Hit;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Space(20)]
    [Header("Component")]
    [SerializeField] private SkinnedMeshRenderer _mesh;

    // Components
    private PlayerController _plyCtrl;
    private StatController _statCtrl;
    private ElementalController _elCtrl;
    private AttackLineController _alCtrl;
    private Rigidbody _rigidbody;
    private Animator _animator;

    // Hit Rect Targeting
    private RectTransform hrCurrentRect;
    private float hrWidthRatio;
    private float hrHeightRatio;

    // State
    private MonFSM state = MonFSM.Normal;
    [HideInInspector] public bool isTarget;

    // Attack Routine
    private float atkDelay = 0f;
    private bool isAtkRoutineRunning = false;
    private bool isFinAtkRoutineRunning = false;
    private float lastAttackEndTime = float.MinValue;
    private const float COOLDOWN_AFTER_ATTACK = 1.5f;

    Coroutine prevWork;

    void OnEnable()
    {
        ResetAtkVariables();
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVar();
        InitMonFSM();
        UpdateAtkRoutine();
    }

    void FixedUpdate()
    {
        TurnToPlayer();
    }

    #region Initialization

    private void ResetAtkVariables()
    {
        isAtkRoutineRunning = false;
        isFinAtkRoutineRunning = false;
        atkDelay = 0f;
    }

    private void Init()
    {
        // Initialize components.
        _plyCtrl = GameManager.Inst._plyCtrl;
        _statCtrl = GetComponent<StatController>();
        _elCtrl = GetComponent<ElementalController>();
        _alCtrl = GetComponent<AttackLineController>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();

        // Initialize mon type.
        type = _statCtrl.GetAsset.MonType;

        // Initialize base stat.
        int level = StageManager.Inst.Asset.NUM_STAGE;
        _statCtrl.InitStat(level);

        // Initialize physics.
        transform.position = GameObject.Find("SpawnPoint").transform.position;
        Invoke("ResetKine", 1.66f);
    }

    #endregion

    #region Update Methods

    private void UpdateVar()
    {
        isTarget = CheckIsTarget();

        if (isAtkRoutineRunning)
        {
            atkDelay += Time.deltaTime;

            if (atkDelay >= _statCtrl.MP)
            {
                atkDelay = float.MinValue;
                _animator.CrossFade(atkAnimClip.name, 0.1f);

                if (!isFinAtkRoutineRunning)
                    StartCoroutine(FinishAtkRoutine());
            }
        }
    }

    private void InitMonFSM()
    {
        if (state != MonFSM.Normal) return;
        SetFSM(MonFSM.Normal);
    }

    private void UpdateAtkRoutine()
    {
        if (Time.time - lastAttackEndTime < COOLDOWN_AFTER_ATTACK)
            return;

        // Return if is on attack or full-scheduled or already on schedule.
        if (!MonsterManager.Inst.CheckIsAtkAble(this) || isAtkRoutineRunning)
            return;

        // Register on attack schedule.
        MonsterManager.Inst.AddToAtkRoutine(this);

        // Set to attack state.
        isAtkRoutineRunning = true;
    }

    private IEnumerator FinishAtkRoutine()
    {
        // Avoid duplicate coroutine calls.
        isFinAtkRoutineRunning = true;

        //***Draw attack line***
        Vector3 targetPosition = Vector3.zero;
        if (_alCtrl)
        {
            StartCoroutine(_alCtrl.DrawForDuration(atkAnimClip.length));

            //Wait until the attack animation ends.
            //yield return new WaitForSeconds(atkAnimClip.length);
            yield return new WaitUntil(() => _alCtrl.IsEndStaticPositionInitialied);
            targetPosition = _alCtrl.EndStaticPosition;
        }

        //Remove from attack schedule.
        isFinAtkRoutineRunning = false;
        MonsterManager.Inst.RmvFromAtkRoutine(this);

        //Waiting for attack.
        lastAttackEndTime = Time.time;

        //Re-assign to attack routine.
        ResetAtkVariables();
        UpdateAtkRoutine();

        //--- Attack!!
        ProjectileController instantProjectile;
        switch (type)
        {
            case MonType.Melee:
                //Shoot projectile - bullet.
                instantProjectile = Instantiate
                (projectilePrefab,
                _alCtrl.StartALRoot.position,
                _alCtrl.StartALRoot.rotation,
                transform)
                .GetComponent<ProjectileController>();
                instantProjectile.Inst(this);

                ////2. Instantiate elemental hit effect.
                //ApplyDamageInfoToPlayer();

                break;
            case MonType.Spell:
                //1. Shoot projectile - bullet.
                instantProjectile = Instantiate
                (projectilePrefab,
                _alCtrl.StartALRoot.position,
                _alCtrl.StartALRoot.rotation,
                transform)
                .GetComponent<ProjectileController>();
                instantProjectile.Inst(this, targetPosition);

                ////2. Instantiate paintball effect on screen.
                //GameObject obj = Instantiate(_elCtrl.GetAsset.GetPaintball()
                //    , Camera.main.GetComponentInChildren<Canvas>().transform);
                //obj.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 0f);

                ////3. Instantiate elemental hit effect.
                //ApplyDamageInfoToPlayer();

                break;
            case MonType.Buff:
                //1. Instantiate attack effect on self.
                /*Test*/
                GameObject effect = (GameObject)Resources.Load("Prefabs/Spell/MON_Heal");
                GameObject instantEffect = null;

                /*Test*/
                //2. Heal every mons alived.
                foreach (var item in MonsterManager.Inst.monsTotal)
                {
                    if (!item.gameObject.activeInHierarchy)
                        continue;

                    //2-1. Display effect.
                    instantEffect = Instantiate
                    (effect,
                    item.transform.position,
                    item.transform.rotation,
                    item.transform);
                    Destroy(instantEffect, 1.33f);

                    //2-2. Adjust current HP.
                    StatController target = item.GetComponent<StatController>();
                    target.HPCurrent = Mathf.Clamp
                    (target.HPCurrent + target.HP * 0.33f,
                    0f,
                    target.HP);
                }

                break;
        }
        //---

        //Return to idle state.
        _animator.CrossFade("idle_up_down", 0.2f);
    }

    public void ApplyDamageInfoToPlayer()
    {
        Calculator.DamageInfo info = null;
        ElementalManager.ElementalType elType
            = ElementalManager.ElementalType.Count;

        switch (type)
        {
            case MonType.Melee:
                //1. Apply damage value.
                _plyCtrl.OnDamage(info = Calculator.MeleeDamage(
                    _statCtrl
                    , _plyCtrl.GetComponent<StatController>()));

                break;
            case MonType.Spell:
                //1. Instantiate paintball effect on screen.
                GameObject obj = Instantiate(_elCtrl.GetAsset.GetPaintball()
                    , Camera.main.GetComponentInChildren<Canvas>().transform);
                obj.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 0f);

                //2-a. Apply damage value.
                _plyCtrl.OnDamage(info = Calculator.SpellDamage(
                    _statCtrl
                    , _plyCtrl.GetComponent<StatController>()));

                //2-b. Activate elemental damage loop by odds.
                if (Calculator.CheckElementalAttackable(
                    _elCtrl
                    , _plyCtrl.GetComponent<StatController>()))
                {
                    /*Test*/
                    //2. Instantiate elemental effect.
                    _elCtrl.InstElementalEffect(_plyCtrl.transform);
                    /*Test*/
                    _plyCtrl.GetComponent<StatController>().OnELDamage(_elCtrl);
                }

                elType = _elCtrl.GetAsset.GetELTYPE();

                break;
            default:
                return;
        }

        //3. Instantiate hit effect.
        if (info.Type == DamageTextController.DamageType.Normal
            || info.Type == DamageTextController.DamageType.Critical)
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                                _plyCtrl.transform
                                , elType
                                , info.Type));
    }

    #endregion

    #region FixedUpdate

    private void TurnToPlayer()
    {
        Vector3 dirVec = _plyCtrl.transform.position - transform.position;
        Quaternion newRotation = Quaternion.LookRotation(dirVec);
        _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, newRotation, Time.deltaTime * 2f);
    }

    #endregion

    #region Targeting

    private bool CheckIsTarget()
    {
        // Target's current position if screen's h and w are 1.
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);

        hrCurrentRect = _plyCtrl.GetHRCurrentRect();
        Vector2 hrRatio = UIUtility.UIRectToViewportRatio(hrCurrentRect);

        bool onScreen = screenPoint.x > 0.5f - hrRatio.x / 1.5f &&
                        screenPoint.x < 0.5f + hrRatio.x / 1.5f &&
                        screenPoint.y > 0.0f &&
                        screenPoint.y < 0.5f + hrRatio.y / 1.5f &&
                        screenPoint.z > 0.0f;

        return onScreen;
    }

    #endregion

    #region Damage & Death

    /// <summary>
    /// 물리, 마법 데미지 피격 메서드입니다.
    /// </summary>
    /// <param name="info">데미지 정보</param>
    public void OnDamage(Calculator.DamageInfo info)
    {
        _statCtrl.HPCurrent -= info.Value;

        if (_statCtrl.HPCurrent > 0f)
        {
            ////Play SFX: Hit.
            //AudioPlayerPoolManager.Instance.PlaySFXClipOnce
            //(AudioPlayerPoolManager.SFXPoolType.Player,
            //sfxClip_Hit);

            //Play hit effect.
            StartCoroutine(ApplyHitEffect());

            //Instantiate damage text.
            GameObject obj = DamageTextPoolManager.Inst.Get(info);
            DamageTextController dtc = obj.GetComponent<DamageTextController>();
            prevWork = StartCoroutine(dtc.Init(
                DamageTextPoolManager.Inst.GetRoot(transform)
                , 0f));
        }
        else
        {
            Dead();
        }
    }

    public void Dead()
    {
        //Change to dynamic mode.
        ResetKine();

        //Remove from attack routine.
        MonsterManager.Inst.RmvFromAtkRoutine(this);

        //Quit running damage text coroutine.
        if (prevWork != null) StopCoroutine(prevWork);

        //Elapse kill count.
        ++StageManager.Inst.KillCount;

        //Remove from pool.
        gameObject.SetActive(false);
    }

    public IEnumerator ApplyHitEffect()
    {
        SetFSM(MonFSM.Hit);
        yield return new WaitForSeconds(0.2f);
        SetFSM(MonFSM.Normal);
    }

    #endregion

    #region FSM & Material

    public void SetFSM(MonFSM state)
    {
        this.state = state;
        SetMat(GetMat(state));
    }

    public MonFSM GetCurFSM() => state;
    public MonType GetMonType() => type;

    public void SetMat(Material mat)
    {
        _mesh.material = mat;
    }

    public Material GetMat(MonFSM state)
    {
        if (state == MonFSM.Pick)
            return mats[(int)state * 2];
        else
            return mats[(int)state * 2 + (isTarget ? 1 : 0)];
    }

    #endregion

    #region Utility

    private void ResetKine()
    {
        if (_rigidbody.isKinematic)
            _rigidbody.isKinematic = false;
        else
            _rigidbody.isKinematic = true;
    }

    #endregion

    #region GET
    public bool IsAttack => isFinAtkRoutineRunning;
    #endregion
}
