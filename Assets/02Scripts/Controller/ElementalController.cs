using System.Collections;
using UnityEngine;

/// <summary>
/// ����(�Ӽ�) ����Ʈ�� ���ҽ��� ��ȸ/����ϰ�
/// DOT(���� ����) Ÿ�̹��� �����Ѵ�.
/// </summary>
public class ElementalController : MonoBehaviour
{
    private const string ROOT_NAME = "ElementalEffectRoot";

    [SerializeField] private ElementalStatSO elementalStatAsset;

    // ����Ʈ ������ �� ��� �� �ν��Ͻ�
    private GameObject elementalEffect;
    private GameObject displayingEffect;

    // DOT Ÿ�̹�
    private float damageDelta = 0f;
    private bool attackFlag = false;

    /// <summary>
    /// ���� �� �Ŵ��� �غ� ��ٷȴٰ� ����Ʈ ���ҽ��� ĳ���Ѵ�.
    /// </summary>
    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// DOT(���� ����) ������ �����ϰ� Ʈ���� ������ �Ǵ��Ѵ�.
    /// (�� ���� �ڻ� ���� �� ���� ��ȯ�ϴ� ������ �־� �ǵ� Ȯ�� �ʿ�)
    /// </summary>
    private void Update()
    {
        if (elementalStatAsset != null) return;
        if (!attackFlag) return;

        damageDelta += Time.deltaTime;
        if (damageDelta >= elementalStatAsset.GetITV(0 /* Level */))
        {
            damageDelta = 0f;
            // TODO: HitEffectPoolManager ��� DOT Ÿ��/����Ʈ ó��
        }
    }

    /// <summary>
    /// ElementalManager �غ� ��� �� ȿ�� Ÿ��/���ҿ� �´� ����Ʈ ������ ĳ��.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => ElementalManager.Inst);

        if (elementalStatAsset != null)
        {
            var efType = elementalStatAsset.GetEFTYPE();
            var elType = elementalStatAsset.GetELTYPE();
            elementalEffect = ElementalManager.Inst.GetEffect(efType, elType);
        }
    }

    /// <summary>
    /// ����� ��Ʈ�� ���� ����Ʈ�� ����Ѵ�(���� ����Ʈ ���� �� ����).
    /// </summary>
    public void InstElementalEffect(Transform target)
    {
        ClearRoot(target);
        StartCoroutine(PlayElementalEffect(target));
    }

    /// <summary>
    /// ����� ���� ����Ʈ ��Ʈ ���� ������Ʈ�� ��� �����Ѵ�.
    /// </summary>
    private void ClearRoot(Transform target)
    {
        Transform root = GetRoot(target);
        foreach (Transform child in root)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// ����Ʈ�� �ν��Ͻ�ȭ�Ͽ� ���� �ð� ���� ǥ�� �� �����Ѵ�.
    /// (DOT ������ Update�� delta �������� ���� ó��)
    /// </summary>
    private IEnumerator PlayElementalEffect(Transform target)
    {
        displayingEffect = Instantiate(elementalEffect, GetRoot(target));
        yield return new WaitForSeconds(elementalStatAsset.GetDUR(0));
        Destroy(displayingEffect);
    }

    // --- Helpers / Properties ---
    private Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    public ElementalStatSO GetAsset => elementalStatAsset;
}
