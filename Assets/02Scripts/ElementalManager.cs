using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
//using UnityEditor;
using UnityEngine;

public class ElementalManager : MonoBehaviour
{
    private const string ELEMENTAL_PRELINK = "Prefabs/Elemental/";

    public static ElementalManager Inst;

    /// <summary>
    /// 플레이어 기준 원소 공격의 타입입니다.
    /// 관리하는 리소스 타입입니다.
    /// </summary>
    public enum EffectType
    { Dot, Hit, Count/*Length*/ }

    /// <summary>
    /// 원소 종류입니다.
    /// </summary>
    public enum ElementalType
    { Fire, Ice, Light, Poison, Count/*Length*/}
    public ElementalType STAGE_ELEMENT { get; set; }

    private Dictionary<ElementalType, GameObject>[] EffectContainer;

    private void Awake()
    {
        Inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
        LoadRcsToContainer();
    }

    private void Init()
    {
        EffectContainer = new Dictionary<ElementalType, GameObject>[(int)EffectType.Count];
    }

    private void LoadRcsToContainer()
    {
        for (int i = 0; i < (int)EffectType.Count; i++)
        {
            EffectContainer[i] = new Dictionary<ElementalType, GameObject>();

            for (int j = 0; j < (int)ElementalType.Count; j++)
            {
                EffectType efType = (EffectType)i;
                ElementalType elType = (ElementalType)j;

                string path = GetEffectPath(efType, elType);

                GameObject resource = ResourceUtility.GetResourceByType<GameObject>(path);
                EffectContainer[i][elType] = resource;
            }
        }
    }

    public GameObject GetEffect(EffectType efType, ElementalType elType)
    {
        return EffectContainer[(int)efType][elType];
    }

    public Dictionary<ElementalType, GameObject> GetEffectsByType(EffectType type)
    {
        if (type == EffectType.Count) return null;

        return EffectContainer[(int)type];

        //Dictionary<ElementalType, GameObject> result
        //    = new Dictionary<ElementalType, GameObject>();
        //foreach (var item in EffectContainer[(int)type])
        //{
        //    result[item.Key] = item.Value;
        //}

        //return result;
    }

    private string GetEffectPath(EffectType efType, ElementalType elType)
    {
        string path = ELEMENTAL_PRELINK + efType + "_" + elType;

        return path;
    }
}
