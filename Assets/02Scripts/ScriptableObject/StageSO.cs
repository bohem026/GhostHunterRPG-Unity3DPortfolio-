using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Stage Asset")]
public class StageSO : ScriptableObject
{
    [Space(20)]
    public int NUM_STAGE;
    [Header("MONSTER")]
    public List<MonsterEntry> entries;
    //public List<GameObject> monPrefabs;
    [Space(20)]
    [Header("REWARD")]
    public int stGemCount;
    public int skGemCount;
    public List<GearSO> rewardGears;
    [Space(20)]
    [Header("BGM")]
    public AudioClip bgm_Wave1;
    public AudioClip bgm_Wave2;
    public AudioClip bgm_Wave3;

}

[Serializable]
public class MonsterEntry
{
    public GameObject prefab;
    public int[] countPerWave;
}
