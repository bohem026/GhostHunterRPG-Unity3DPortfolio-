using System;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using UnityEngine;

// Manages spell list contains all kinds of spells.
public class SpellManager : MonoBehaviour
{
    public static SpellManager Inst;
    private static int MAX_SPELL_LV = 5;

    public enum SpellLvMode
    { UP, DOWN, RESET }

    [SerializeField] private GameObject[] SpellPrototypes;  // Original form of spell effect;
    [SerializeField] private List<GameObject>[] SpellList;
    //[SerializeField] private int[] CurSpellLvArray;
    /*Test*/
    public int[] CurSpellLvArray;
    /*Test*/

    GameObject spell;
    bool isInitialized;

    // !!! 사용자 스킬트리 레벨 저장할 배열 !!!

    void Awake()
    {
        if (isInitialized) return;

        Inst = this;
        InitSpellList();

        isInitialized = true;
    }

    private void InitSpellList()
    {
        //1. Init collections.
        SpellList = new List<GameObject>[SpellPrototypes.Length];
        CurSpellLvArray = new int[SpellList.Length];
        for (int i = 0; i < SpellList.Length; i++)
        {
            SpellList[i] = new List<GameObject>();
            /*Note*/
            /*-1로 초기화: 스킬 미해금 상태*/
            /*0 이상으로 초기화: 스킬 해금 상태*/
            CurSpellLvArray[i] = 0;
            /*Note*/
        }

        //2. Sort spell prototypes by ID.
        Array.Sort(SpellPrototypes, (a, b) =>
        a.GetComponent<SpellPrototypeSorter>().ID.CompareTo(
            b.GetComponent<SpellPrototypeSorter>().ID));

        //3. Init all skill list.
        for (int outer = 0; outer < SpellPrototypes.Length; outer++)
        {
            for (int inner = 0; inner < MAX_SPELL_LV; inner++)
            {
                //if (!(spell = (GameObject)AssetDatabase.LoadAssetAtPath(
                //                $"Assets/01Prefabs/Spell/{SpellPrototypes[i].name}_Lv{j}.prefab",
                //                typeof(GameObject))))
                string path = $"Prefabs/Spell/{SpellPrototypes[outer].name}_Lv{inner}";
                Debug.Log(path);
                if (!(spell = ResourceUtility.GetResourceByType<GameObject>(path)))
                    continue;

                //SpellList[i].Add(spell);
                SpellList[outer].Insert(inner, spell);
            }
        }
    }

    #region --- [Get func]
    public GameObject GetSpell(int ID)
    {
        if (ID >= GetSpellTypeCount())
            return null;

        SpellEffectController temp
            = SpellList[ID][0].GetComponent<SpellEffectController>();
        SkillWindowController.SKType Outer = temp.OUTER;
        SkillWindowController.ELType Inner = temp.INNER;
        int level = GlobalValue.Instance.GetSkillLevelByEnum(Outer, Inner);
        /*Test*/
        //level = Mathf.Clamp(level, 1, 5);
        //Debug.Log($"ID {ID} LEVEL {level}");
        /*Test*/

        Debug.Log($"ID{ID}LEVEL{level}");
        if (level == 0) return null;
        return SpellList[ID][level - 1];
    }

    public int GetSpellTypeCount()
    {
        return SpellPrototypes.Length;
    }
    #endregion
}
