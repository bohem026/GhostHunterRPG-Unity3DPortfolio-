using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class AttackLineController : MonoBehaviour
{
    private const string NAME_ROOT = "AttackLineRoot";
    private const float OFFSET_LAST = 1.33f;

    [SerializeField] private Transform startALRoot;

    PlayerController _plyCtrl;
    MonsterController _monCtrl;
    Transform endALRoot;
    LineRenderer lineRenderer;

    Vector3 endStaticPosition;
    bool isEndStaticPositionInitialized;
    bool isLineStatic;

    void OnEnable()
    {
        _monCtrl = GetComponent<MonsterController>();
        lineRenderer = GetComponent<LineRenderer>();
        if (!endALRoot) StartCoroutine(Init());

        //Component
        lineRenderer.enabled = false;
        //Variable
        endStaticPosition = Vector3.zero;
        isEndStaticPositionInitialized = false;
        isLineStatic = false;
    }

    void Update()
    {
        if (!lineRenderer.enabled) return;

        if (startALRoot == null ||
            !gameObject.activeInHierarchy)
        {
            lineRenderer.enabled = false;
        }

        if (endALRoot != null)
        {
            lineRenderer.SetPosition(0, startALRoot.position);
            lineRenderer.SetPosition
                (1,
                isLineStatic ? endStaticPosition : endALRoot.position);
        }
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => GameManager.Inst);

        _plyCtrl = GameManager.Inst._plyCtrl;
        endALRoot = _plyCtrl.transform.Find(NAME_ROOT);
    }

    public IEnumerator DrawForDuration(float duration)
    {
        isEndStaticPositionInitialized = false;
        isLineStatic = false;
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.enabled = true;

        yield return new WaitForSeconds(duration);
        endStaticPosition = endALRoot.position;
        isEndStaticPositionInitialized = true;

        //스펠-> 즉시 종료
        if (_monCtrl.GetMonType() == MonsterController.MonType.Spell)
        {
            lineRenderer.enabled = false;
            isLineStatic = false;
            yield break;
        }

        //밀리-> 종료 유예
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        isLineStatic = true;

        yield return new WaitForSeconds(OFFSET_LAST);
        lineRenderer.enabled = false;
    }

    public Transform StartALRoot => startALRoot;
    public Vector3 EndStaticPosition => endStaticPosition;
    public bool IsEndStaticPositionInitialied => isEndStaticPositionInitialized;
}
