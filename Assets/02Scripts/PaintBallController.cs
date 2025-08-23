using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AnimController;

public class PaintBallController : MonoBehaviour
{
    private const string PAINTBALL_FOLDER_LINK = "Images/Paintballs";
    private const string PAINTBALL_PRELINK = "Images/Paintballs/paint_effect_0";

    private const float CANVAS_DISTANCE = 3.5f;
    private const float SPEED_TO_NORMAL = 10f;
    private const float REMOVE_EFFECT_LENGTH = 5f;
    private const float DECPOWER_REMOVE_SPEED = 100f;
    private const float INCPOWER_EFFECT_SPREAD = 1f;

    private PlayerController _plyCtrl;
    private RectTransform _rect;
    private RawImage _rawImg;

    Vector3 rmvEffectDestPosition;
    Vector3 rmvEffectDestScale;
    Vector3 elapsedColorAlpha = Vector3.forward;
    Vector3 rmvEffectDestAlpha;
    private float elapsedTime = 0f;
    private float randomScale = 1.0f;
    private bool isRemoving = false;

    private void Awake()
    {
        StartCoroutine(Init());
    }

    private void Update()
    {
        ChangeScaleToNormal();
        PlayRemoveEffect();
    }

    private void ChangeScaleToNormal()
    {
        if (isRemoving) return;
        if (elapsedTime > REMOVE_EFFECT_LENGTH) return;

        /*!!Note!!*/
        /*스케일 값 랜덤*/
        if (_rect)
            _rect.localScale = Vector3.Lerp(_rect.localScale, Vector3.one * randomScale, Time.unscaledDeltaTime * SPEED_TO_NORMAL);
    }

    private void PlayRemoveEffect()
    {
        if (!isRemoving) return;
        if ((elapsedTime += Time.unscaledDeltaTime) > REMOVE_EFFECT_LENGTH)
        {
            isRemoving = false;
            return;
        }

        float elapsedTimeRate = elapsedTime / DECPOWER_REMOVE_SPEED;

        //Manipulate position y;
        _rect.localPosition = Vector3.Lerp(_rect.localPosition, rmvEffectDestPosition, elapsedTimeRate);
        //Manipulate scale y;
        _rect.localScale = Vector3.Lerp(_rect.localScale, rmvEffectDestScale, elapsedTimeRate);
        //Manipulate color a;
        elapsedColorAlpha = Vector3.Lerp(elapsedColorAlpha, rmvEffectDestAlpha, elapsedTimeRate);
        _rawImg.color = new Color(1f, 1f, 1f, elapsedColorAlpha.z);
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst != null);

        _plyCtrl = GameManager.Inst._plyCtrl;
        _rect = this.GetComponent<RectTransform>();
        _rawImg = this.GetComponent<RawImage>();

        randomScale = Random.Range(1f, 2f);

        RectTransform hrCurrentRect = _plyCtrl.GetHRCurrentRect();
        Vector2 hrRatio = UIUtility.UIRectToViewportRatio(hrCurrentRect);
        float hrWRatioOffset = (0.5f - hrRatio.x / 2f) / INCPOWER_EFFECT_SPREAD;
        float hrHRatioOffset = (0.5f - hrRatio.y / 2f) / INCPOWER_EFFECT_SPREAD;

        Vector2 randomViewport = new Vector2(Mathf.Clamp(UnityEngine.Random.Range(0f, 1f)
                                                        , hrWRatioOffset
                                                        , 1f - hrWRatioOffset)
                                            , Mathf.Clamp(UnityEngine.Random.Range(0f, 1f)
                                                        , hrHRatioOffset
                                                        , 1f - hrHRatioOffset));
        Vector3 screenPosition = Camera.main.ViewportToWorldPoint(
                                    new Vector3(randomViewport.x
                                                , randomViewport.y
                                                , CANVAS_DISTANCE));

        //int totalPaintBallType = ResourceUtility.CountResourcesOfType<Texture2D>(PAINTBALL_FOLDER_LINK);
        //int randomPaintBallIdx = Random.Range(0, totalPaintBallType) + 1;
        //_rawImg.texture = Resources.Load<Texture2D>(PAINTBALL_PRELINK + randomPaintBallIdx);

        _rect.position = screenPosition;
        //Restore color value after positioning.
        _rawImg.color = new Color(1f, 1f, 1f, 1f);
        _rect.sizeDelta = Vector2.one;
        _rect.localScale = Vector3.zero;

        yield return new WaitForSecondsRealtime(0.25f);
        InitDestValue();
        isRemoving = true;

        yield return new WaitUntil(() => !isRemoving);
        Destroy(gameObject);
    }

    /*OK*/
    private void InitDestValue()
    {
        rmvEffectDestPosition = _rect.localPosition - Vector3.up * 15f;
        rmvEffectDestScale = _rect.localScale + Vector3.up * 25f;
        rmvEffectDestAlpha = Vector3.forward * 1f;
    }
}
