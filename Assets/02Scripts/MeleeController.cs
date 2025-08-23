using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeController : MonoBehaviour
{
    public const float SLASH_DURATION = 0.5f;
    private const string ROOT_NAME = "SlashEffectRoot";

    [SerializeField] private SwordSO swordAsset;
    //몬스터를 공격할 때 사용_ 속성값 존재할 경우 해당 프리팹으로 교체

    PlayerController _plyCtrl;
    Transform slashEffectRoot;

    // Start is called before the first frame update
    void Start()
    {
        _plyCtrl = GameManager.Inst._plyCtrl;
        slashEffectRoot = _plyCtrl.transform.Find(ROOT_NAME);
    }

    public void InstSlashEffect(AnimController.AnimState state)
    {
        float effectRotX = 0f;

        //Rotate effect by attack type.
        switch (state)
        {
            case AnimController.AnimState.NAttack0:
                effectRotX = swordAsset.EffectRotXNAtk;
                break;
            case AnimController.AnimState.NAttack1:
                effectRotX = -swordAsset.EffectRotXNAtk;
                break;
            case AnimController.AnimState.SAttack:
                effectRotX = swordAsset.EffectRotXSAtk;
                break;
            default:
                break;
        }

        Vector3 dirVec = new Vector3(slashEffectRoot.position.x - transform.position.x,
                                        0.0f,
                                        slashEffectRoot.position.z - transform.position.z);
        GameObject obj = Instantiate(swordAsset.SlashEffect,
                                    slashEffectRoot.position,
                                    Quaternion.LookRotation(
                                        dirVec
                                        , new Vector3(dirVec.z > 0 ? -effectRotX : effectRotX
                                        , 1, 0)),
                                    HitEffectPoolManager.Inst.transform);
        obj.GetComponent<ParticleSystem>().Play();
        GameObject.Destroy(obj, SLASH_DURATION);
    }

    #region GET
    public float GetDamageRate(AnimController.AnimState state)
    {
        float rate = 1f;

        switch (state)
        {
            case AnimController.AnimState.NAttack0:
                rate = swordAsset.MinDamageRate * 0.95f;
                break;
            case AnimController.AnimState.NAttack1:
                rate = swordAsset.MinDamageRate * 1.1f;
                break;
            case AnimController.AnimState.SAttack:
                rate = swordAsset.MinDamageRate * 1.66f;
                break;
            default:
                break;
        }

        return rate;
    }

    public SwordSO GetAsset => swordAsset;
    #endregion
}
