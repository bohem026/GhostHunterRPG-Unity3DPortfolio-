using System.Collections;
using UnityEngine;

public class AttackLineController : MonoBehaviour
{
    private const string NAME_ROOT = "AttackLineRoot";
    private const float OFFSET_LAST = 1.33f;

    [SerializeField] private Transform startALRoot; // ���� ������(���� ��)

    // --- Components / runtime refs ---
    private PlayerController _plyCtrl;
    private MonsterController _monCtrl;
    private Transform endALRoot;     // ���� ����(�÷��̾��� AttackLineRoot)
    private LineRenderer lineRenderer;

    // --- State ---
    private Vector3 endStaticPosition;           // �������� �����Ǵ� ���� ��ġ(������ �ܻ�)
    private bool isEndStaticPositionInitialized; // ���� ���� ��ǥ�� ��ϵǾ����� ����
    private bool isLineStatic;                   // true�� ������ ������ ���� ���� ����

    /// <summary>
    /// ������Ʈ ĳ�� �� �⺻ ���� �ʱ�ȭ.
    /// </summary>
    private void OnEnable()
    {
        _monCtrl = GetComponent<MonsterController>();
        lineRenderer = GetComponent<LineRenderer>();
        if (!endALRoot) StartCoroutine(Init());

        // �ʱ� ����
        lineRenderer.enabled = false;
        endStaticPosition = Vector3.zero;
        isEndStaticPositionInitialized = false;
        isLineStatic = false;
    }

    /// <summary>
    /// ���� Ȱ�� �� ����/�� ��ǥ�� �����Ѵ�.
    /// </summary>
    private void Update()
    {
        if (!lineRenderer.enabled) return;

        // �������� ���ų� ������Ʈ�� ��Ȱ��ȭ�Ǹ� ���� ��Ȱ��
        if (startALRoot == null || !gameObject.activeInHierarchy)
        {
            lineRenderer.enabled = false;
        }

        // Ȱ�� ���¶�� ���� ��ǥ ����(������ ����/���� ��忡 ���� �б�)
        if (endALRoot != null)
        {
            lineRenderer.SetPosition(0, startALRoot.position);
            lineRenderer.SetPosition(
                1,
                isLineStatic ? endStaticPosition : endALRoot.position
            );
        }
    }

    /// <summary>
    /// GameManager�� Player�� ���/ȹ�� �� ���� Ʈ�������� ĳ��.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst);

        _plyCtrl = GameManager.Inst._plyCtrl;
        endALRoot = _plyCtrl.transform.Find(NAME_ROOT);
    }

    /// <summary>
    /// ������ ���� �ð���ŭ �ʷϻ����� ǥ���� ��,
    /// - ���� ����: ��� ����
    /// - ���� ����: ���������� ��� �� ���� ǥ�� �� ����
    /// </summary>
    public IEnumerator DrawForDuration(float duration)
    {
        isEndStaticPositionInitialized = false;
        isLineStatic = false;

        // 1) Ȱ��ȭ(�ʷϻ�, ���� ���� ����)
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.enabled = true;

        // ���� �ð��� ǥ��
        yield return new WaitForSeconds(duration);

        // 2) ���� ���� ��ǥ ���
        endStaticPosition = endALRoot.position;
        isEndStaticPositionInitialized = true;

        // 3) ������: ��� ����
        if (_monCtrl.GetMonType() == MonsterController.MonType.Spell)
        {
            lineRenderer.enabled = false;
            isLineStatic = false;
            yield break;
        }

        // 4) ������: ���������� ��ȯ �� ���� ���·� ��� ����
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        isLineStatic = true;

        yield return new WaitForSeconds(OFFSET_LAST);

        // 5) ����
        lineRenderer.enabled = false;
    }

    // --- Public getters (�ܺ� ������) ---
    public Transform StartALRoot => startALRoot;
    public Vector3 EndStaticPosition => endStaticPosition;
    public bool IsEndStaticPositionInitialied => isEndStaticPositionInitialized;
}
