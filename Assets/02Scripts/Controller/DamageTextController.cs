using System.Collections;
using UnityEngine;

public class DamageTextController : MonoBehaviour
{
    private const float HORIZONTAL_SPEED = 5f;  // Lerp 속도 계수
    private const float VERTICAL_OFFSET = 75f;  // 화면 상단 오프셋(px)

    public enum DamageType
    {
        Normal, Critical, Fire, Poison, /*Heal,*/ Evaded, Count /*Length*/
    }

    [SerializeField] private DamageType type;
    [SerializeField] private float duration;      // 유지 시간(초)

    // --- Runtime refs/state ---
    private PlayerController _plyCtrl;
    private RectTransform _rect;
    private Transform root;                       // 따라갈 월드 타겟(예: 몬스터의 텍스트 루트)
    private Vector3 curScreenPos;
    private bool isMovable;

    /// <summary>
    /// 필수 컴포넌트 캐싱 및 상태 초기화.
    /// </summary>
    private void OnEnable()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _rect = GetComponent<RectTransform>();
        isMovable = false;
    }

    /// <summary>
    /// 데미지 텍스트 초기화 및 수명 타이머.
    /// </summary>
    /// <param name="root">시작 위치가 될 타겟 트랜스폼</param>
    /// <param name="delay">표시 전 대기 시간(초)</param>
    public IEnumerator Init(Transform root, float delay)
    {
        // 1) 타겟/초기 위치/거리 기반 스케일
        this.root = root;
        _rect.position = Camera.main.WorldToScreenPoint(this.root.position);
        transform.localScale = GetScaleByDistance(this.root);

        // 2) 지연 후 이동 시작
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        isMovable = true;

        // 3) 표시 지속 후 비활성화
        yield return new WaitForSecondsRealtime(duration);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 카메라와의 거리비에 따른 가독성 스케일(0.5~1.0).
    /// </summary>
    private Vector3 GetScaleByDistance(Transform root)
    {
        float standardDist = (_plyCtrl.transform.position - Camera.main.transform.position).magnitude;
        float measuredDist = (root.position - Camera.main.transform.position).magnitude;
        return Vector3.one * Mathf.Clamp(standardDist / measuredDist, 0.5f, 1f);
    }

    /// <summary>
    /// 타겟을 화면 좌표로 추적하며 위로 부드럽게 이동.
    /// </summary>
    private void Update()
    {
        if (!isMovable) return;

        // 타겟이 사라지면 즉시 종료
        if (root == null || !root.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        // 타겟의 화면 위치를 따라가며 위로 Lerp 이동
        curScreenPos = Camera.main.WorldToScreenPoint(root.position);
        _rect.position = Vector3.Lerp(
            _rect.position,
            curScreenPos + Vector3.up * VERTICAL_OFFSET,
            Time.unscaledDeltaTime * HORIZONTAL_SPEED
        );
    }

    // --- Getter ---
    public DamageType GetDamageType => type;
}
