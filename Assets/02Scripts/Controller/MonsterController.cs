using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    private const float COOLDOWN_AFTER_ATTACK = 1.5f; // ���� ���� �� ����۱��� ���
    private const string SPAWN_POINT_NAME = "SpawnPoint";

    public enum MonType { Melee, Spell, Buff, Count /*Capacity*/ }
    public enum MonFSM { Normal, Hit, Pick }

    [Header("Monster Settings")]
    [SerializeField] private MonType type;     // ���� Ÿ���� StatSO���� ��������

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
    /// ����/��Ȱ��ȭ �� ���� ���¸� �ʱ�ȭ�Ѵ�.
    /// </summary>
    private void OnEnable()
    {
        ResetAtkVariables();
        Init();
    }

    /// <summary>
    /// �� ������: Ÿ����/���� �ʱ�ȭ/���� ������ ����.
    /// </summary>
    private void Update()
    {
        UpdateVar();
        InitMonFSM();
        UpdateAtkRoutine();
    }

    /// <summary>
    /// ���� ������Ʈ: �÷��̾� �������� ������ ȸ��.
    /// </summary>
    private void FixedUpdate()
    {
        TurnToPlayer();
    }

    #region Initialization

    /// <summary>
    /// ���� ��ƾ ���� ���� �ʱ�ȭ.
    /// </summary>
    private void ResetAtkVariables()
    {
        isAtkRoutineRunning = false;
        isFinAtkRoutineRunning = false;
        atkDelay = 0f;
    }

    /// <summary>
    /// ������Ʈ/����/���� �ʱ�ȭ �� ���� ��ġ ����.
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

        // Type: StatSO �������� �缳��
        type = _statCtrl.GetAsset.MonType;

        // Base stat: �������� ���� �ݿ�
        int level = StageManager.Inst.Asset.NUM_STAGE;
        _statCtrl.InitStat(level);

        // Spawn & ���� ���
        transform.position = GameObject.Find(SPAWN_POINT_NAME).transform.position;
        Invoke(nameof(ResetKine), 1.66f);
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// ���� ���� ���� �� ���� Ÿ�̹� üũ.
    /// </summary>
    private void UpdateVar()
    {
        isTarget = CheckIsTarget();

        if (isAtkRoutineRunning)
        {
            atkDelay += Time.deltaTime;

            // MP(����)�� ���� ���� ��� �ð� �Ӱ�ġ�� ���
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
    /// �⺻ FSM ����(�ʿ� ��).
    /// </summary>
    private void InitMonFSM()
    {
        if (state != MonFSM.Normal) return;
        SetFSM(MonFSM.Normal);
    }

    /// <summary>
    /// ���� ������ ���/���� ����.
    /// </summary>
    private void UpdateAtkRoutine()
    {
        // �ֱ� ���� ���� �� ��ٿ�
        if (Time.time - lastAttackEndTime < COOLDOWN_AFTER_ATTACK)
            return;

        // �̹� ���� ���̰ų� ������ ���ѿ� �ɸ��� �н�
        if (!MonsterManager.Inst.CheckIsAtkAble(this) || isAtkRoutineRunning)
            return;

        // ������ ��� �� ���� ���� ����
        MonsterManager.Inst.AddToAtkRoutine(this);
        isAtkRoutineRunning = true;
    }

    /// <summary>
    /// ���� �ִ� ������� ���� ǥ��/����ü �߻�/�罺������ ó��.
    /// </summary>
    private IEnumerator FinishAtkRoutine()
    {
        isFinAtkRoutineRunning = true;

        // ���� ���� ǥ�� �� Ÿ�� ��ġ Ȯ��
        Vector3 targetPosition = Vector3.zero;
        if (_alCtrl)
        {
            StartCoroutine(_alCtrl.DrawForDuration(atkAnimClip.length));
            yield return new WaitUntil(() => _alCtrl.IsEndStaticPositionInitialied);
            targetPosition = _alCtrl.EndStaticPosition;
        }

        // �����ٿ��� ���� �� ��ٿ� ���
        isFinAtkRoutineRunning = false;
        MonsterManager.Inst.RmvFromAtkRoutine(this);
        lastAttackEndTime = Time.time;

        // ���� ��ƾ ���� ���� �� ���� �õ�
        ResetAtkVariables();
        UpdateAtkRoutine();

        // --- ���� ���� ---
        ProjectileController instantProjectile;
        switch (type)
        {
            case MonType.Melee:
                // ����: �÷��̾� �������� ����ü(źȯ) �߻�
                instantProjectile = Instantiate(
                    projectilePrefab,
                    _alCtrl.StartALRoot.position,
                    _alCtrl.StartALRoot.rotation,
                    transform
                ).GetComponent<ProjectileController>();
                instantProjectile.Inst(this);
                break;

            case MonType.Spell:
                // ���Ÿ�: ���� ����(���� ��ġ)�� ����ü �߻�
                instantProjectile = Instantiate(
                    projectilePrefab,
                    _alCtrl.StartALRoot.position,
                    _alCtrl.StartALRoot.rotation,
                    transform
                ).GetComponent<ProjectileController>();
                instantProjectile.Inst(this, targetPosition);
                break;

            case MonType.Buff:
                // ����: ���� ���� ���͵��� ġ���ϰ� ����Ʈ ǥ��
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

        // ��� �ִϷ� ����
        _animator.CrossFade("idle_up_down", 0.2f);
    }

    /// <summary>
    /// �÷��̾�� ���� ������/���� �̻��� �����ϰ� ��Ʈ ����Ʈ�� ��û.
    /// </summary>
    public void ApplyDamageInfoToPlayer()
    {
        Calculator.DamageInfo info = null;
        ElementalManager.ElementalType elType = ElementalManager.ElementalType.Count;

        switch (type)
        {
            case MonType.Melee:
                // ���� ���� ������
                _plyCtrl.OnDamage(info = Calculator.MeleeDamage(
                    _statCtrl,
                    _plyCtrl.GetComponent<StatController>()));
                break;

            case MonType.Spell:
                // ȭ�� ����Ʈ�� ȿ�� + ���� ������
                GameObject obj = Instantiate(
                    _elCtrl.GetAsset.GetPaintball(),
                    Camera.main.GetComponentInChildren<Canvas>().transform
                );
                obj.GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 0f);

                _plyCtrl.OnDamage(info = Calculator.SpellDamage(
                    _statCtrl,
                    _plyCtrl.GetComponent<StatController>()));

                // Ȯ�������� ���� ��Ʈ �ο�
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

        // ��Ʈ ����Ʈ(����/Ÿ��) ��û
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
    /// �÷��̾ ���� �ε巴�� ȸ��.
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
    /// ȭ�� ���� ���� �簢�� �ȿ� ���Դ��� �˻�.
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
    /// �ǰ� ó��: ü�� ����, �ǰ� ����Ʈ/������ �ؽ�Ʈ �Ǵ� ��� ó��.
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
    /// ��� ó��: ���� ���, ������ ����, �ڷ�ƾ ����, óġ ī��Ʈ ����, ��Ȱ��ȭ.
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
    /// ª�� �ð� �ǰ� ���� ��Ƽ���� ����.
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
    /// FSM ��ȯ �� ���� ��Ƽ���� ����.
    /// </summary>
    public void SetFSM(MonFSM state)
    {
        this.state = state;
        SetMat(GetMat(state));
    }

    public MonFSM GetCurFSM() => state;
    public MonType GetMonType() => type;

    /// <summary>
    /// ��Ų�� �޽� �������� ��Ƽ������ ��ü.
    /// </summary>
    public void SetMat(Material mat)
    {
        _mesh.material = mat;
    }

    /// <summary>
    /// ����/���� ���ο� ���� ��Ƽ���� ����.
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
    /// ������ٵ��� Ű�׸�ƽ ���¸� ����Ѵ�.
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
