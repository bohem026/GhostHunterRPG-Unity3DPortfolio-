using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PaintBallController : MonoBehaviour
{
    private const float CANVAS_DISTANCE = 3.5f;       // 카메라 뷰포트 기준 Z 거리
    private const float SPEED_TO_NORMAL = 10f;        // 초기 스케일 → 목표 스케일 보간 속도
    private const float REMOVE_EFFECT_LENGTH = 5f;    // 제거 연출 총 시간(초)
    private const float DECPOWER_REMOVE_SPEED = 100f; // 제거 연출 보간 분모(느슨한 Lerp)
    private const float INCPOWER_EFFECT_SPREAD = 1f;  // 화면 중앙부 가중치(퍼짐 정도)

    // --- Runtime refs ---
    private PlayerController _plyCtrl;
    private RectTransform _rect;
    private RawImage _rawImg;

    // --- Remove effect targets ---
    private Vector3 rmvEffectDestPosition;
    private Vector3 rmvEffectDestScale;
    private Vector3 elapsedColorAlpha = Vector3.forward; // z 구성요소를 알파처럼 활용
    private Vector3 rmvEffectDestAlpha;

    // --- State ---
    private float elapsedTime = 0f;
    private float randomScale = 1.0f; // 최초 목표 스케일(랜덤)
    private bool isRemoving = false;

    /// <summary>
    /// 초기화 코루틴 시작(참조 캐싱·위치/스케일 세팅 후 제거 연출까지 수명 관리).
    /// </summary>
    private void Awake()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// 매 프레임: 등장 시 스케일 보정, 제거 연출 재생.
    /// </summary>
    private void Update()
    {
        ChangeScaleToNormal();
        PlayRemoveEffect();
    }

    /// <summary>
    /// 등장 직후 0 → randomScale 로 부드럽게 확대한다.
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
    /// 제거 연출: 살짝 아래로 이동하면서 스케일 변경 및 알파 보간 후 파기.
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

        // 위치/스케일/알파를 서서히 목표치로 보간
        _rect.localPosition = Vector3.Lerp(_rect.localPosition, rmvEffectDestPosition, t);
        _rect.localScale = Vector3.Lerp(_rect.localScale, rmvEffectDestScale, t);
        elapsedColorAlpha = Vector3.Lerp(elapsedColorAlpha, rmvEffectDestAlpha, t);
        _rawImg.color = new Color(1f, 1f, 1f, elapsedColorAlpha.z);
    }

    /// <summary>
    /// 참조 캐싱 → 화면 내 무작위(히트레티클 가중) 위치에 배치 →
    /// 잠시 대기 후 제거 연출 시작 → 끝나면 오브젝트 파괴.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst != null);

        // 참조 캐싱
        _plyCtrl = GameManager.Inst._plyCtrl;
        _rect = GetComponent<RectTransform>();
        _rawImg = GetComponent<RawImage>();

        // 1) 초기 목표 스케일(1~2배) 결정
        randomScale = Random.Range(1f, 2f);

        // 2) 히트 사각형 비율을 이용해 화면 중앙부에 가중치 둔 랜덤 위치 선택
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

        // 3) 초기 표시 상태 세팅(위치/불투명/사이즈/스케일 0)
        _rect.position = screenPosition;
        _rawImg.color = new Color(1f, 1f, 1f, 1f);
        _rect.sizeDelta = Vector2.one;
        _rect.localScale = Vector3.zero;

        // 4) 잠시 노출 후 제거 연출 준비
        yield return new WaitForSecondsRealtime(0.25f);
        InitDestValue();
        isRemoving = true;

        // 5) 제거 연출 종료까지 대기 후 파괴
        yield return new WaitUntil(() => !isRemoving);
        Destroy(gameObject);
    }

    /// <summary>
    /// 제거 연출의 목표 위치/스케일/알파를 설정한다.
    /// </summary>
    private void InitDestValue()
    {
        rmvEffectDestPosition = _rect.localPosition - Vector3.up * 15f;
        rmvEffectDestScale = _rect.localScale + Vector3.up * 25f;
        rmvEffectDestAlpha = Vector3.forward * 1f; // z=1(알파 그대로 유지)
    }
}
