using System.Collections;
using UnityEngine;

public class DamageTextController : MonoBehaviour
{
    private const float HORIZONTAL_SPEED = 5f;  // Lerp �ӵ� ���
    private const float VERTICAL_OFFSET = 75f;  // ȭ�� ��� ������(px)

    public enum DamageType
    {
        Normal, Critical, Fire, Poison, /*Heal,*/ Evaded, Count /*Length*/
    }

    [SerializeField] private DamageType type;
    [SerializeField] private float duration;      // ���� �ð�(��)

    // --- Runtime refs/state ---
    private PlayerController _plyCtrl;
    private RectTransform _rect;
    private Transform root;                       // ���� ���� Ÿ��(��: ������ �ؽ�Ʈ ��Ʈ)
    private Vector3 curScreenPos;
    private bool isMovable;

    /// <summary>
    /// �ʼ� ������Ʈ ĳ�� �� ���� �ʱ�ȭ.
    /// </summary>
    private void OnEnable()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _rect = GetComponent<RectTransform>();
        isMovable = false;
    }

    /// <summary>
    /// ������ �ؽ�Ʈ �ʱ�ȭ �� ���� Ÿ�̸�.
    /// </summary>
    /// <param name="root">���� ��ġ�� �� Ÿ�� Ʈ������</param>
    /// <param name="delay">ǥ�� �� ��� �ð�(��)</param>
    public IEnumerator Init(Transform root, float delay)
    {
        // 1) Ÿ��/�ʱ� ��ġ/�Ÿ� ��� ������
        this.root = root;
        _rect.position = Camera.main.WorldToScreenPoint(this.root.position);
        transform.localScale = GetScaleByDistance(this.root);

        // 2) ���� �� �̵� ����
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        isMovable = true;

        // 3) ǥ�� ���� �� ��Ȱ��ȭ
        yield return new WaitForSecondsRealtime(duration);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ī�޶���� �Ÿ��� ���� ������ ������(0.5~1.0).
    /// </summary>
    private Vector3 GetScaleByDistance(Transform root)
    {
        float standardDist = (_plyCtrl.transform.position - Camera.main.transform.position).magnitude;
        float measuredDist = (root.position - Camera.main.transform.position).magnitude;
        return Vector3.one * Mathf.Clamp(standardDist / measuredDist, 0.5f, 1f);
    }

    /// <summary>
    /// Ÿ���� ȭ�� ��ǥ�� �����ϸ� ���� �ε巴�� �̵�.
    /// </summary>
    private void Update()
    {
        if (!isMovable) return;

        // Ÿ���� ������� ��� ����
        if (root == null || !root.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        // Ÿ���� ȭ�� ��ġ�� ���󰡸� ���� Lerp �̵�
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
