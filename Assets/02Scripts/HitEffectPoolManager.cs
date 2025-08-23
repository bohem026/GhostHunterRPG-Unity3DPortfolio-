using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ElementalManager;

public class HitEffectPoolManager : MonoBehaviour
{
    public const float HIT_EFFECT_DURATION = 0.5f;
    public const string ROOT_NAME = "HitEffectRoot";
    private const string BOX_NAME = "HitEffectBox";

    public static HitEffectPoolManager Inst;

    // 1. Variables carry prefabs.
    private Dictionary<ElementalType, GameObject> EHitEffectPrefabs;
    [SerializeField] private GameObject NHitEffectPrefab;
    // 2. List carry instantiated game objects.
    List<GameObject>[] EHitEffectPools;
    List<GameObject> NHitEffectPool;
    // 3. Transform carries instantiated game objects in heirachy window.
    private Transform box;

    private void Awake()
    {
        Inst = this;
    }

    private void Start()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => ElementalManager.Inst);

        //Initiate prefabs.
        EHitEffectPrefabs = ElementalManager.Inst
            .GetEffectsByType(ElementalManager.EffectType.Hit);

        //Initiate pool for every type.
        EHitEffectPools = new List<GameObject>[EHitEffectPrefabs.Count];
        for (int i = 0; i < EHitEffectPools.Length; i++)
        {
            EHitEffectPools[i] = new List<GameObject>();
        }

        NHitEffectPool = new List<GameObject>();

        //Initiate box.
        box = transform.Find(BOX_NAME);
    }

    public IEnumerator InstHitEffect
        (
        Transform target
        , ElementalManager.ElementalType elType
        , DamageTextController.DamageType dmgType
        )
    {
        //1. Match hit effect with elemental type.
        GameObject obj
            = elType != ElementalManager.ElementalType.Count
            //Elemental type
            ? Get(elType)
            //Normal type
            : Get();

        //2. Set position and scale.
        obj.transform.position
            = target.Find(ROOT_NAME).position;
        obj.transform.localScale = Vector3.one
            * (dmgType == DamageTextController.DamageType.Critical
            ? 1.5f
            : 0.85f);

        yield return new WaitForSecondsRealtime(HIT_EFFECT_DURATION);
        obj.SetActive(false);
    }

    #region GET
    public GameObject Get(ElementalManager.ElementalType type)
    {
        GameObject selected = null;

        foreach (GameObject item in EHitEffectPools[(int)type])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);

                break;
            }
        }

        if (!selected)
        {
            selected = Instantiate(EHitEffectPrefabs[type], box);
            EHitEffectPools[(int)type].Add(selected);
        }

        return selected;
    }

    public GameObject Get()
    {
        Debug.Log("TEST_GET NORMAL");
        GameObject selected = null;

        foreach (GameObject item in NHitEffectPool)
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);

                break;
            }
        }

        if (!selected)
        {
            selected = Instantiate(NHitEffectPrefab, box);
            NHitEffectPool.Add(selected);
        }

        return selected;
    }

    public Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    #endregion
}
