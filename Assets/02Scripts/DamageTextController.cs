using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTextController : MonoBehaviour
{
    private const float HORIZONTAL_SPEED = 5f;
    private const float VERTICAL_OFFSET = 75f;

    public enum DamageType
    {
        Normal,
        Critical,
        Fire,
        Poison,
        /*Heal,*/
        Evaded,
        Count/*Length*/
    }
    [SerializeField] private DamageType type;

    //���� �ð�
    [SerializeField] private float duration;

    PlayerController _plyCtrl;
    RectTransform _rect;

    Transform root;
    Vector3 preScreenPos;
    Vector3 curScreenPos;

    bool isMovable;

    private void OnEnable()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        _rect = this.GetComponent<RectTransform>();

        isMovable = false;
    }

    public IEnumerator Init(Transform root/*���� ��ġ*/, float delay/*�ִϸ��̼� ��� �� ����*//*�������*/)
    {
        //�ʱ� ����
        //1. Ÿ��(root) ����
        this.root = root;
        //2. ��ġ �ʱ�ȭ
        _rect.position = Camera.main.WorldToScreenPoint(this.root.position);
        //3. ������ ����
        transform.localScale = GetScaleByDistance(this.root);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        isMovable = true;

        yield return new WaitForSecondsRealtime(duration);
        gameObject.SetActive(false);
    }

    private Vector3 GetScaleByDistance(Transform root)
    {
        float standardDist = (_plyCtrl.transform.position - Camera.main.transform.position).magnitude;
        float measuredDist = (root.position - Camera.main.transform.position).magnitude;

        return Vector3.one * Mathf.Clamp(standardDist / measuredDist, 0.5f, 1f);
    }

    private void Update()
    {
        if (!isMovable) return;

        //Remove self if target is dead.
        if (root == null || !root.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        //Play damage text animation if is movable.
        curScreenPos = Camera.main.WorldToScreenPoint(root.position);
        _rect.position = Vector3.Lerp(_rect.position, curScreenPos + Vector3.up * VERTICAL_OFFSET, Time.unscaledDeltaTime * HORIZONTAL_SPEED);
    }

    #region GET
    public DamageType GetDamageType => type;
    #endregion
}
