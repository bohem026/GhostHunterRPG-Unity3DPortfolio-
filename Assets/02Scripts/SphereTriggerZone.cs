using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereTriggerZone : MonoBehaviour
{
    [Space(20)]
    [Header("TRAP ZONE")]
    [SerializeField] private GameObject sphereTrapZone;
    [Space(20)]
    [Header("SPAWN POINT")]
    [SerializeField] private List<Transform> spawnPoints;

    private bool isTrapZoneTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isTrapZoneTriggered) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isTrapZoneTriggered = true;
            sphereTrapZone.SetActive(true);

            //Register this zone as current.
            StageManager.Inst.UpdateCurrentSphereZone(this);
            //Start wave(FIRST).
            StartCoroutine(StageManager.Inst.StartWave());
        }
    }

    public SphereTrapZone SphereTrapZone
        => sphereTrapZone.GetComponent<SphereTrapZone>();

    public List<Transform> SpawnPoints => spawnPoints;
}
