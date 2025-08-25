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
    /// <summary>�ش� ���̺꿡�� ��ȯ�� ���� ������.</summary>
    public GameObject prefab;

    /// <summary>���̺꺰 ��ȯ ��(�ε��� = ���̺�-1).</summary>
    public int[] countPerWave;
}
