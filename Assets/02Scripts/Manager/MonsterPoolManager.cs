using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterPoolManager : MonoBehaviour
{
    public static MonsterPoolManager Inst;

    [Space(20)]
    [SerializeField] private Transform box;

    // ������ ����(�������� �����ͷκ���)
    private List<MonsterEntry> monEntries;
    // Ÿ�Ժ� �ν��Ͻ� Ǯ
    private List<GameObject>[] monPools;
    private StageSO asset;

    private bool isInitialized = false;

    /// <summary>
    /// �̱��� ����.
    /// </summary>
    private void Awake()
    {
        if (!Inst) Inst = this;
    }

    /// <summary>
    /// �ʱ�ȭ �ڷ�ƾ ����.
    /// </summary>
    private void Start()
    {
        if (!isInitialized) StartCoroutine(Init());
    }

    /// <summary>
    /// StageManager �غ� ��� �� �������� ����/��Ʈ��/Ǯ �ʱ�ȭ ��
    /// Ÿ�Ժ� ���� �ڽ�(�θ� Transform) ����.
    /// </summary>
    private IEnumerator Init()
    {
        yield return new WaitUntil(() => StageManager.Inst);

        // 1) �������� ����/��Ʈ�� �ε�
        asset = StageManager.Inst.Asset;
        monEntries = asset.entries;

        // 2) Ÿ�Ժ� Ǯ ���� �� ���� �ڽ� �غ�
        monPools = new List<GameObject>[monEntries.Count];
        for (int i = 0; i < monPools.Length; i++)
        {
            monPools[i] = new List<GameObject>();

            GameObject childBox = new GameObject($"Box {i}");
            childBox.transform.SetParent(box);
            childBox.transform.localPosition = Vector3.zero;
        }

        isInitialized = true;
    }

    /// <summary>
    /// ���� �ε����� ���� �ν��Ͻ��� �����´�.
    /// ��Ȱ�� Ǯ���� �����ϰ�, ������ ���� �����Ѵ�.
    /// </summary>
    public GameObject Get(int index)
    {
        // �ε��� ���� üũ
        if (index < 0 || index >= monEntries.Count)
            return null;

        GameObject selected = null;

        // 1) ��Ȱ�� ������Ʈ ����
        foreach (GameObject item in monPools[index])
        {
            if (!item.activeSelf)
            {
                selected = item;
                item.SetActive(true);
                break;
            }
        }

        // 2) ������ ���� ���� �� Ǯ�� �߰�
        if (!selected)
        {
            selected = Instantiate(monEntries[index].prefab, box.GetChild(index));
            monPools[index].Add(selected);
        }

        return selected;
    }

    /// <summary>
    /// ���������� �� ���̺긦 �����ϴ� "���� ����" ��ȯ�� �����Ѵ�.
    /// (���̺긦 ���� ���� ���� ��ȯ�ϴ� �ܰ� �� �� ��)
    /// </summary>
    public void SummonWaveAtom()
    {
        Debug.Log("[DEBUG] SUMMON WAVE ATOM");

        int currentWave = StageManager.Inst.CurrentWave;                 // 1-based
        int currentWaveAtom = (StageManager.Inst.CurrentWaveAtom += 1);  // ���� ���� ���� �ܰ� ����
        SphereTriggerZone currentSphereZone = StageManager.Inst.CurrentSphereZone;

        int currentWaveIndex = Mathf.Clamp(currentWave - 1, 0, StageManager.MAX_WAVE - 1);

        // Ÿ�Ժ� ��ȯ ��
        int[] summonCounts = new int[monEntries.Count];

        // --- ���̺� ���� �ܰ躰 ��ȯ �� ��� ---
        // 1) �ִ� ���� �ܰ� �ʰ� ��: ���� ���̺�� ����
        if (currentWaveAtom > StageManager.MAX_WAVE_ATOM)
        {
            StageManager.Inst.CurrentWaveAtom = 0;

            if (!StageManager.Inst.IsWaveInProgress)
                StartCoroutine(StageManager.Inst.StartWave());

            return;
        }
        // 2) ������ ���� �ܰ�: ���� ���� ��� ��ȯ
        else if (currentWaveAtom == StageManager.MAX_WAVE_ATOM)
        {
            for (int i = 0; i < monEntries.Count; i++)
            {
                summonCounts[i] =
                    (monEntries[i].countPerWave[currentWaveIndex] / StageManager.MAX_WAVE_ATOM) +
                    (monEntries[i].countPerWave[currentWaveIndex] % StageManager.MAX_WAVE_ATOM);
            }

            StageManager.Inst.IsWaveInProgress = false;
        }
        // 3) 1~(������-1) �ܰ�: ���̺� ���� 1/MAX_WAVE_ATOM��ŭ ��ȯ
        else
        {
            for (int i = 0; i < monEntries.Count; i++)
            {
                summonCounts[i] =
                    monEntries[i].countPerWave[currentWaveIndex] / StageManager.MAX_WAVE_ATOM;
            }
        }
        // ---

        // ��ȯ ��ġ ����
        int spCount = currentSphereZone.SpawnPoints.Count;
        Transform spawnPoint = currentSphereZone.SpawnPoints[Random.Range(0, spCount)];

        // --- ���� ��ȯ ---
        for (int outer = 0; outer < monEntries.Count; outer++)
        {
            for (int inner = 0; inner < summonCounts[outer]; inner++)
            {
                int capturedOuter = outer;
                GameObject go = Instantiate(monEntries[capturedOuter].prefab, box);
                go.transform.position = spawnPoint.position;
            }
        }
        // ---
    }
}
