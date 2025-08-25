using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    private const float COOLDOWN_AFTER_ATTACK = 1.5f; // 공격 종료 후 재시작까지 대기
    private const string SPAWN_POINT_NAME = "SpawnPoint";

    public enum MonType { Melee, Spell, Buff, Count /*Capacity*/ }
    public enum MonFSM { Normal, Hit, Pick }

    [Header("Monster Settings")]
    [SerializeField] private MonType type;     // 실제 타입은 StatSO에서 재지정됨

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

    // --- Components (runtime) ---
    private PlayerController _plyCtrl;
    private StatController _statCtrl;
    private ElementalController _elCtrl;
    private AttackLineController _alCtrl;
    private Rigidbody _rigidbody;
    private Animator _animator;

    // --- Hit Rect Targeting ---
    private RectTransform hrCurrentRect;

    // --- State ---
    private MonFSM state = MonFSM.Normal;
    [HideInInspector] public bool isTarget;

    // --- Attack Routine ---
    private float atkDelay = 0f;
    private bool isAtkRoutineRunning = false;
    private bool isFinAtkRoutineRunning = false;
    private float lastAttackEndTime = float.MinValue;

    private Coroutine prevWork;

    /// <summary>
    /// 스폰/재활성화 시 내부 상태를 초기화한다.
    /// </summary>
    private void OnEnable()
    {
        ResetAtkVariables();
        Init();
    }

    /// <summary>
    /// 매 프레임: 타깃팅/상태 초기화/공격 스케줄 갱신.
    /// </summary>
    private void Update()
    {
        UpdateVar();
        InitMonFSM();
        UpdateAtkRoutine();
    }

    /// <summary>
    /// 물리 업데이트: 플레이어 방향으로 서서히 회전.
    /// </summary>
    private void FixedUpdate()
    {
        TurnToPlayer();
    }

    #region Initialization

    /// <summary>
    /// 공격 루틴 관련 변수 초기화.
    /// </summary>
    private void ResetAtkVariables()
    {
        isAtkRoutineRunning = false;
        isFinAtkRoutineRunning = false;
        atkDelay = 0f;
    }

    /// <summary>
    /// 컴포넌트/스탯/물리 초기화 및 스폰 위치 설정.
    /// </summary>
    private void Init()
    {
        // Components
        _plyCtrl = GameManager.Inst._plyCtrl;
        _statCtrl = GetComponent<StatController>();
        _elCtrl = GetComponent<ElementalController>();
        _alCtrl = GetComponent<AttackLineController>();
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponentInChildren<Animator>();

        // Type: StatSO 기준으로 재설정
        type = _statCtrl.GetAsset.MonType;

        // Base stat: 스테이지 레벨 반영
        int level = StageManager.Inst.Asset.NUM_STAGE;
        _statCtrl.InitStat(level);

        // Spawn & 물리 토글
        transform.position = GameObject.Find(SPAWN_POINT_NAME).transform.position;
        Invoke(nameof(ResetKine), 1.66f);
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// 조준 상태 갱신 및 공격 타이밍 체크.
    /// </summary>
    private void UpdateVar()
    {
        isTarget = CheckIsTarget();

        if (isAtkRoutineRunning)
        {
            atkDelay += Time.deltaTime;

            // MP(스탯)의 값을 공격 대기 시간 임계치로 사용
            if (atkDelay >= _statCtrl.MP)
            {
                atkDelay = float.MinValue;
                _animator.CrossFade(atkAnimClip.name, 0.1f);

                if (!isFinAtkRoutineRunning)
                    StartCoroutine(FinishAtkRoutine());
            }
        }
    }

    /// <summary>
    /// 기본 FSM 진입(필요 시).
    /// </summary>
    private void InitMonFSM()
    {
        if (state != MonFSM.Normal) return;
        SetFSM(MonFSM.Normal);
    }

    /// <summary>
    /// 공격 스케줄 등록/재등록 관리.
    /// </summary>
    private void UpdateAtkRoutine()
    {
        // 최근 공격 종료 후 쿨다운
        if (Time.time - lastAttackEndTime < COOLDOWN_AFTER_ATTACK)
            return;

        // 이미 공격 중이거나 스케줄 제한에 걸리면 패스
        if (!MonsterManager.Inst.CheckIsAtkAble(this) || isAtkRoutineRunning)
            return;

        // 스케줄 등록 및 공격 상태 진입
        MonsterManager.Inst.AddToAtkRoutine(this);
        isAtkRoutineRunning = true;
    }

    /// <summary>
    /// 공격 애니 종료까지 라인 표시/투사체 발사/재스케줄을 처리.
    /// </summary>
    private IEnumerator FinishAtkRoutine()
    {
        isFinAtkRoutineRunning = true;

        // 공격 라인 표시 및 타깃 위치 확정
        Vector3 targetPosition = Vector3.zero;
        if (_alCtrl)
        {
            StartCoroutine(_alCtrl.DrawForDuration(atkAnimClip.length));
            yield return new WaitUntil(() => _alCtrl.IsEndStaticPositionInitialied);
            targetPosition = _alCtrl.EndStaticPosition;
        }

        // 스케줄에서 제거 및 쿨다운 기록
        isFinAtkRoutineRunning = false;
        MonsterManager.Inst.RmvFromAtkRoutine(this);
        lastAttackEndTime = Time.time;

        // 공격 루틴 변수 리셋 및 재등록 시도
        ResetAtkVariables();
        UpdateAtkRoutine();

        // --- 실제 공격 ---
        ProjectileController instantProjectile;
        switch (type)
        {
            case MonType.Melee:
                // 근접: 플레이어 방향으로 투사체(탄환) 발사
                instantProjectile = Instantiate(
                    projectilePrefab,
                    _alCtrl.StartALRoot.position,
                    _alCtrl.StartALRoot.rotation,
                    transform
                ).GetComponent<ProjectileController>();
                instantProjectile.Inst(this);
                break;

            case MonType.Spell:
                // 원거리: 라인 종점(예측 위치)로 투사체 발사
                instantProjectile = Instantiate(
                    projectilePrefab,
                    _alCtrl.StartALRoot.position,
                    _alCtrl.StartALRoot.rotation,
                    transform
                ).GetComponent<ProjectileController>();
                instantProjectile.Inst(this, targetPosition);
                break;

            case MonType.Buff:
                // 보조: 생존 중인 몬스터들을 치유하고 이펙트 표시
                GameObject effect = (GameObject)Resources.Load("Prefabs/Spell/MON_Heal");
                GameObject instantEffect = null;

                foreach (var item in MonsterManager.Inst.monsTotal)
                {
                    if (!item.gameObject.activeInHierarchy) continue;

                    instantEffect = Instantiate(effect, item.transform.position, item.transform.rotation, item.transform);
                    Destroy(instantEffect, 1.33f);

                    StatController target = item.GetComponent<StatController>();
                    target.HPCurrent = Mathf.Clamp(target.HPCurrent + target.HP * 0.33f, 0f, target.HP);
                }
                break;
        }
        // ------------------

        // 대기 애니로 복귀
        _animator.CrossFade("idle_up_down", 0.2f);
    }

    /// <summary>
    /// 플레이어에게 최종 데미지/상태 이상을 적용하고 히트 이펙트를 요청.
    /// </summary>
    public void ApplyDamageInfoToPlayer()
    {
        Calculator.DamageInfo info = null;
        ElementalManager.ElementalType elType = ElementalManager.ElementalType.Count;

        switch (type)
        {
            case MonType.Melee:
                // 물리 근접 데미지
                _plyCtrl.OnDamage(info = Calculator.MeleeDamage(
                    _statCtrl,
                    _plyCtrl.GetComponent<StatController>()));
                break;

            case MonType.Spell:
                // 화면 페인트볼 효과 + 마법 데미지
                GameObject obj = Instantiate(
                    _elCtrl.GetAsset.GetPaintball(),
                    Camera.main.GetComponentInChildren<Canvas>().transform
                );
                obj.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 0f);

                _plyCtrl.OnDamage(info = Calculator.SpellDamage(
                    _statCtrl,
                    _plyCtrl.GetComponent<StatController>()));

                // 확률적으로 원소 도트 부여
                if (Calculator.CheckElementalAttackable(_elCtrl, _plyCtrl.GetComponent<StatController>()))
                {
                    _elCtrl.InstElementalEffect(_plyCtrl.transform);
                    _plyCtrl.GetComponent<StatController>().OnELDamage(_elCtrl);
                }

                elType = _elCtrl.GetAsset.GetELTYPE();
                break;

            default:
                return;
        }

        // 히트 이펙트(숫자/타격) 요청
        if (info.Type == DamageTextController.DamageType.Normal ||
            info.Type == DamageTextController.DamageType.Critical)
        {
            StartCoroutine(HitEffectPoolManager.Inst.InstHitEffect(
                _plyCtrl.transform, elType, info.Type));
        }
    }

    #endregion

    #region FixedUpdate helpers

    /// <summary>
    /// 플레이어를 향해 부드럽게 회전.
    /// </summary>
    private void TurnToPlayer()
    {
        Vector3 dirVec = _plyCtrl.transform.position - transform.position;
        Quaternion newRotation = Quaternion.LookRotation(dirVec);
        _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, newRotation, Time.deltaTime * 2f);
    }

    #endregion

    #region Targeting

    /// <summary>
    /// 화면 기준 조준 사각형 안에 들어왔는지 검사.
    /// </summary>
    private bool CheckIsTarget()
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(transform.position);

        hrCurrentRect = _plyCtrl.GetHRCurrentRect();
        Vector2 hrRatio = UIUtility.UIRectToViewportRatio(hrCurrentRect);

        bool onScreen =
            screenPoint.x > 0.5f - hrRatio.x / 1.5f &&
            screenPoint.x < 0.5f + hrRatio.x / 1.5f &&
            screenPoint.y > 0.0f &&
            screenPoint.y < 0.5f + hrRatio.y / 1.5f &&
            screenPoint.z > 0.0f;

        return onScreen;
    }

    #endregion

    #region Damage & Death

    /// <summary>
    /// 피격 처리: 체력 감소, 피격 이펙트/데미지 텍스트 또는 사망 처리.
    /// </summary>
    public void OnDamage(Calculator.DamageInfo info)
    {
        _statCtrl.HPCurrent -= info.Value;

        if (_statCtrl.HPCurrent > 0f)
        {
            StartCoroutine(ApplyHitEffect());

            GameObject obj = DamageTextPoolManager.Inst.Get(info);
            var dtc = obj.GetComponent<DamageTextController>();
            prevWork = StartCoroutine(dtc.Init(
                DamageTextPoolManager.Inst.GetRoot(transform), 0f));
        }
        else
        {
            Dead();
        }
    }

    /// <summary>
    /// 사망 처리: 물리 토글, 스케줄 해제, 코루틴 정리, 처치 카운트 증가, 비활성화.
    /// </summary>
    public void Dead()
    {
        ResetKine();
        MonsterManager.Inst.RmvFromAtkRoutine(this);

        if (prevWork != null) StopCoroutine(prevWork);

        ++StageManager.Inst.KillCount;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 짧은 시간 피격 상태 머티리얼 적용.
    /// </summary>
    public IEnumerator ApplyHitEffect()
    {
        SetFSM(MonFSM.Hit);
        yield return new WaitForSeconds(0.2f);
        SetFSM(MonFSM.Normal);
    }

    #endregion

    #region FSM & Material

    /// <summary>
    /// FSM 전환 및 대응 머티리얼 적용.
    /// </summary>
    public void SetFSM(MonFSM state)
    {
        this.state = state;
        SetMat(GetMat(state));
    }

    public MonFSM GetCurFSM() => state;
    public MonType GetMonType() => type;

    /// <summary>
    /// 스킨드 메시 렌더러의 머티리얼을 교체.
    /// </summary>
    public void SetMat(Material mat)
    {
        _mesh.material = mat;
    }

    /// <summary>
    /// 상태/조준 여부에 따른 머티리얼 선택.
    /// </summary>
    public Material GetMat(MonFSM state)
    {
        if (state == MonFSM.Pick)
            return mats[(int)state * 2];
        else
            return mats[(int)state * 2 + (isTarget ? 1 : 0)];
    }

    #endregion

    #region Utility

    /// <summary>
    /// 리지드바디의 키네마틱 상태를 토글한다.
    /// </summary>
    private void ResetKine()
    {
        _rigidbody.isKinematic = !_rigidbody.isKinematic;
    }

    #endregion

    #region GET
    public bool IsAttack => isFinAtkRoutineRunning;
    #endregion
}
