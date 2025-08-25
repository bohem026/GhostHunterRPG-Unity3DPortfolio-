using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Stage Asset")]
public class StageSO : ScriptableObject
{
    [Space(20)]
    [Header("STAGE")]
    public int NUM_STAGE;

    [Space(20)]
    [Header("MONSTER")]
    public List<MonsterEntry> entries;

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
    /// <summary>해당 웨이브에서 소환할 몬스터 프리팹.</summary>
    public GameObject prefab;

    /// <summary>웨이브별 소환 수(인덱스 = 웨이브-1).</summary>
    public int[] countPerWave;
}
