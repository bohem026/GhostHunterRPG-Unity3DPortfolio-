using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PaintBallController : MonoBehaviour
{
    private const float CANVAS_DISTANCE = 3.5f;       // ī�޶� ����Ʈ ���� Z �Ÿ�
    private const float SPEED_TO_NORMAL = 10f;        // �ʱ� ������ �� ��ǥ ������ ���� �ӵ�
    private const float REMOVE_EFFECT_LENGTH = 5f;    // ���� ���� �� �ð�(��)
    private const float DECPOWER_REMOVE_SPEED = 100f; // ���� ���� ���� �и�(������ Lerp)
    private const float INCPOWER_EFFECT_SPREAD = 1f;  // ȭ�� �߾Ӻ� ����ġ(���� ����)

    // --- Runtime refs ---
    private PlayerController _plyCtrl;
    private RectTransform _rect;
    private RawImage _rawImg;

    // --- Remove effect targets ---
    private Vector3 rmvEffectDestPosition;
    private Vector3 rmvEffectDestScale;
    private Vector3 elapsedColorAlpha = Vector3.forward; // z ������Ҹ� ����ó�� Ȱ��
    private Vector3 rmvEffectDestAlpha;

    // --- State ---
    private float elapsedTime = 0f;
    private float randomScale = 1.0f; // ���� ��ǥ ������(����)
    private bool isRemoving = false;

    /// <summary>
    /// �ʱ�ȭ �ڷ�ƾ ����(���� ĳ�̡���ġ/������ ���� �� ���� ������� ���� ����).
    /// </summary>
    private void Awake()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// �� ������: ���� �� ������ ����, ���� ���� ���.
    /// </summary>
    private void Update()
    {
        ChangeScaleToNormal();
        PlayRemoveEffect();
    }

    /// <summary>
    /// ���� ���� 0 �� randomScale �� �ε巴�� Ȯ���Ѵ�.
    /// </summary>
    private void ChangeScaleToNormal()
    {
        if (isRemoving) return;
        if (elapsedTime > REMOVE_EFFECT_LENGTH) return;

        if (_rect)
            _rect.localScale = Vector3.Lerp(
                _rect.localScale,
                Vector3.one * randomScale,
                Time.unscaledDeltaTime * SPEED_TO_NORMAL
            );
    }

    /// <summary>
    /// ���� ����: ��¦ �Ʒ��� �̵��ϸ鼭 ������ ���� �� ���� ���� �� �ı�.
    /// </summary>
    private void PlayRemoveEffect()
    {
        if (!isRemoving) return;

        if ((elapsedTime += Time.unscaledDeltaTime) > REMOVE_EFFECT_LENGTH)
        {
            isRemoving = false;
            return;
        }

        float t = elapsedTime / DECPOWER_REMOVE_SPEED;

        // ��ġ/������/���ĸ� ������ ��ǥġ�� ����
        _rect.localPosition = Vector3.Lerp(_rect.localPosition, rmvEffectDestPosition, t);
        _rect.localScale = Vector3.Lerp(_rect.localScale, rmvEffectDestScale, t);
        elapsedColorAlpha = Vector3.Lerp(elapsedColorAlpha, rmvEffectDestAlpha, t);
        _rawImg.color = new Color(1f, 1f, 1f, elapsedColorAlpha.z);
    }

    /// <summary>
    /// ���� ĳ�� �� ȭ�� �� ������(��Ʈ��ƼŬ ����) ��ġ�� ��ġ ��
    /// ��� ��� �� ���� ���� ���� �� ������ ������Ʈ �ı�.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst != null);

        // ���� ĳ��
        _plyCtrl = GameManager.Inst._plyCtrl;
        _rect = GetComponent<RectTransform>();
        _rawImg = GetComponent<RawImage>();

        // 1) �ʱ� ��ǥ ������(1~2��) ����
        randomScale = Random.Range(1f, 2f);

        // 2) ��Ʈ �簢�� ������ �̿��� ȭ�� �߾Ӻο� ����ġ �� ���� ��ġ ����
        RectTransform hrCurrentRect = _plyCtrl.GetHRCurrentRect();
        Vector2 hrRatio = UIUtility.UIRectToViewportRatio(hrCurrentRect);
        float hrWRatioOffset = (0.5f - hrRatio.x / 2f) / INCPOWER_EFFECT_SPREAD;
        float hrHRatioOffset = (0.5f - hrRatio.y / 2f) / INCPOWER_EFFECT_SPREAD;

        Vector2 randomViewport = new Vector2(
            Mathf.Clamp(Random.Range(0f, 1f), hrWRatioOffset, 1f - hrWRatioOffset),
            Mathf.Clamp(Random.Range(0f, 1f), hrHRatioOffset, 1f - hrHRatioOffset)
        );

        Vector3 screenPosition = Camera.main.ViewportToWorldPoint(
            new Vector3(randomViewport.x, randomViewport.y, CANVAS_DISTANCE)
        );

        // 3) �ʱ� ǥ�� ���� ����(��ġ/������/������/������ 0)
        _rect.position = screenPosition;
        _rawImg.color = new Color(1f, 1f, 1f, 1f);
        _rect.sizeDelta = Vector2.one;
        _rect.localScale = Vector3.zero;

        // 4) ��� ���� �� ���� ���� �غ�
        yield return new WaitForSecondsRealtime(0.25f);
        InitDestValue();
        isRemoving = true;

        // 5) ���� ���� ������� ��� �� �ı�
        yield return new WaitUntil(() => !isRemoving);
        Destroy(gameObject);
    }

    /// <summary>
    /// ���� ������ ��ǥ ��ġ/������/���ĸ� �����Ѵ�.
    /// </summary>
    private void InitDestValue()
    {
        rmvEffectDestPosition = _rect.localPosition - Vector3.up * 15f;
        rmvEffectDestScale = _rect.localScale + Vector3.up * 25f;
        rmvEffectDestAlpha = Vector3.forward * 1f; // z=1(���� �״�� ����)
    }
}
