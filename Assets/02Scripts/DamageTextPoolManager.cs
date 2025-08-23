using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DamageTextPoolManager : MonoBehaviour
{
    private const string ROOT_NAME = "DamageTextRoot";
    public static DamageTextPoolManager Inst;

    [SerializeField] private Transform box;

    [Space(20)]
    // 1. Variables carry prefabs.
    [SerializeField] private GameObject[] dmgTxtPrefabs;
    // 2. List carry instantiated game objects.
    List<GameObject>[] dmgTxtPools;

    void Awake()
    {
        Inst = this;
    }

    void Start()
    {
        Init();
    }

    private void Init()
    {
        //Sort by DamageController.DamageType
        dmgTxtPrefabs = ResourceUtility.SortByEnum
            <DamageTextController, DamageTextController.DamageType>
            (dmgTxtPrefabs, e => e.GetDamageType);

        //Initiate pool for every type.
        dmgTxtPools = new List<GameObject>[dmgTxtPrefabs.Length];
        for (int i = 0; i < dmgTxtPools.Length; i++)
        {
            dmgTxtPools[i] = new List<GameObject>();
        }
    }

    #region GET
    public GameObject Get(Calculator.DamageInfo info)
    {
        DamageTextController.DamageType type = info.Type;
        float value = info.Value;

        GameObject selected = null;

        foreach (GameObject item in dmgTxtPools[(int)type])
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
            selected = Instantiate(dmgTxtPrefabs[(int)type], box);
            dmgTxtPools[(int)type].Add(selected);
        }

        //Text update.
        if (type != DamageTextController.DamageType.Evaded)
            selected.GetComponent<Text>().text
                = Mathf.CeilToInt(value).ToString();

        return selected;
    }

    public Transform GetRoot(Transform target) => target.Find(ROOT_NAME);
    #endregion
}
