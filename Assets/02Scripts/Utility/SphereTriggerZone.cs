using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���� ������ �÷��̾ �����ϸ� ���� Ʈ���� Ȱ��ȭ�ϰ�
/// �ش� ������ ���� ���̺��� ���� �������� ����մϴ�.
/// </summary>
public class SphereTriggerZone : MonoBehaviour
{
    [Header("TRAP ZONE")]
    [SerializeField] private GameObject sphereTrapZone;

    [Header("SPAWN POINT")]
    [SerializeField] private List<Transform> spawnPoints;

    private bool isTrapZoneTriggered = false;

    /// <summary>
    /// �÷��̾ Ʈ���ſ� �����ϸ� �� ���� Ʈ���� Ȱ��ȭ�ϰ�
    /// �������� �Ŵ����� ���� ������ ����� �� ���̺긦 �����մϴ�.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (isTrapZoneTriggered) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isTrapZoneTriggered = true;
            sphereTrapZone.SetActive(true);

            StageManager.Inst.UpdateCurrentSphereZone(this);
            StartCoroutine(StageManager.Inst.StartWave());
        }
    }

    // GET
    public SphereTrapZone SphereTrapZone => sphereTrapZone.GetComponent<SphereTrapZone>();
    public List<Transform> SpawnPoints => spawnPoints;
}
