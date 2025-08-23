using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Animations.SpringBones.GameObjectExtensions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

/// <summary>
/// 이펙트가 갖는 속성을 정의하고 관련된 작업을 수행합니다.
/// </summary>
public class ElementalController : MonoBehaviour
{
    private const string ROOT_NAME = "ElementalEffectRoot";

    [SerializeField] private ElementalStatSO elementalStatAsset;

    private GameObject elementalEffect;     //Effect prefab of dot attack.
    private GameObject displayingEffect;
    private float damageDelta = 0f;
    private bool attackFlag = false;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    private void Update()
    {
        if (elementalStatAsset != null) return;
        //if (elementalStatAsset.GetELTYPE() != ElementalManager.ElementalType.Fire
        //    && elementalStatAsset.GetELTYPE() != ElementalManager.ElementalType.Poison)
        //    return;
        if (!attackFlag) return;

        damageDelta += Time.deltaTime;
        if (damageDelta >= elementalStatAsset.GetITV(0/*Level*/))
        {
            damageDelta = 0f;
            //HitEffectPoolManager에서 instantiate
        }
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => ElementalManager.Inst);

        if (elementalStatAsset != null)
        {
            ElementalManager.EffectType efType = elementalStatAsset.GetEFTYPE();
            ElementalManager.ElementalType elType = elementalStatAsset.GetELTYPE();
            elementalEffect = ElementalManager.Inst.GetEffect(efType, elType);

            /*Test*/
            Debug.Log(elementalEffect.name);
            /*Test*/
        }
    }

    public void InstElementalEffect(Transform target)
    {
        ClearRoot(target);
        StartCoroutine(PlayElementalEffect(target));
    }

    private void ClearRoot(Transform target)
    {
        Transform root = GetRoot(target);

        foreach (Transform child in root)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    /*Test*/
    IEnumerator PlayElementalEffect(Transform target)
    {
        displayingEffect = Instantiate(elementalEffect, GetRoot(target));

        /*!!Note!!*/
        //델타 이용해 도트 데미지 구현

        yield return new WaitForSeconds(elementalStatAsset.GetDUR(0));
        Destroy(displayingEffect);
    }
    /*Test*/

    #region GET
    private Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    public ElementalStatSO GetAsset => elementalStatAsset;
    #endregion
}
