using System.Collections;
using UnityEngine;

public class AttackLineController : MonoBehaviour
{
    private const string NAME_ROOT = "AttackLineRoot";
    private const float OFFSET_LAST = 1.33f;

    [SerializeField] private Transform startALRoot; // 라인 시작점(몬스터 쪽)

    // --- Components / runtime refs ---
    private PlayerController _plyCtrl;
    private MonsterController _monCtrl;
    private Transform endALRoot;     // 라인 끝점(플레이어의 AttackLineRoot)
    private LineRenderer lineRenderer;

    // --- State ---
    private Vector3 endStaticPosition;           // 마지막에 고정되는 끝점 위치(근접용 잔상)
    private bool isEndStaticPositionInitialized; // 끝점 고정 좌표가 기록되었는지 여부
    private bool isLineStatic;                   // true면 끝점이 고정된 정지 라인 상태

    /// <summary>
    /// 컴포넌트 캐싱 및 기본 상태 초기화.
    /// </summary>
    private void OnEnable()
    {
        _monCtrl = GetComponent<MonsterController>();
        lineRenderer = GetComponent<LineRenderer>();
        if (!endALRoot) StartCoroutine(Init());

        // 초기 상태
        lineRenderer.enabled = false;
        endStaticPosition = Vector3.zero;
        isEndStaticPositionInitialized = false;
        isLineStatic = false;
    }

    /// <summary>
    /// 라인 활성 시 시작/끝 좌표를 갱신한다.
    /// </summary>
    private void Update()
    {
        if (!lineRenderer.enabled) return;

        // 시작점이 없거나 오브젝트가 비활성화되면 라인 비활성
        if (startALRoot == null || !gameObject.activeInHierarchy)
        {
            lineRenderer.enabled = false;
        }

        // 활성 상태라면 라인 좌표 갱신(끝점은 정지/추적 모드에 따라 분기)
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
    /// GameManager와 Player를 대기/획득 후 끝점 트랜스폼을 캐싱.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst);

        _plyCtrl = GameManager.Inst._plyCtrl;
        endALRoot = _plyCtrl.transform.Find(NAME_ROOT);
    }

    /// <summary>
    /// 라인을 지정 시간만큼 초록색으로 표시한 후,
    /// - 스펠 몬스터: 즉시 종료
    /// - 근접 몬스터: 빨간색으로 잠시 더 정지 표시 후 종료
    /// </summary>
    public IEnumerator DrawForDuration(float duration)
    {
        isEndStaticPositionInitialized = false;
        isLineStatic = false;

        // 1) 활성화(초록색, 동적 끝점 추적)
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.enabled = true;

        // 지정 시간만 표시
        yield return new WaitForSeconds(duration);

        // 2) 끝점 고정 좌표 기록
        endStaticPosition = endALRoot.position;
        isEndStaticPositionInitialized = true;

        // 3) 스펠형: 즉시 종료
        if (_monCtrl.GetMonType() == MonsterController.MonType.Spell)
        {
            lineRenderer.enabled = false;
            isLineStatic = false;
            yield break;
        }

        // 4) 근접형: 빨간색으로 전환 후 정지 상태로 잠시 유지
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        isLineStatic = true;

        yield return new WaitForSeconds(OFFSET_LAST);

        // 5) 종료
        lineRenderer.enabled = false;
    }

    // --- Public getters (외부 참조용) ---
    public Transform StartALRoot => startALRoot;
    public Vector3 EndStaticPosition => endStaticPosition;
    public bool IsEndStaticPositionInitialied => isEndStaticPositionInitialized;
}
