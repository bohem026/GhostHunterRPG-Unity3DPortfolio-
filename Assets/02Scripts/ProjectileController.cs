using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    private enum ProjectileType
    { Arched, Straight, Count/*Length*/ }

    [SerializeField] private ProjectileType Type;
    [SerializeField] private float speed;

    MonsterController shooter;
    Rigidbody _rigid;

    Vector3 staticTargetPosition = Vector3.zero;

    private void Shoot()
    {
        _rigid = GetComponent<Rigidbody>();

        switch (Type)
        {
            case ProjectileType.Arched:
                Vector3 dirVec = staticTargetPosition - transform.position;
                dirVec += Vector3.up * Random.Range(0.8f, 1f);
                _rigid.AddForce(dirVec.normalized * speed, ForceMode.Impulse);
                _rigid.AddTorque(Vector3.back * -10f, ForceMode.Impulse);
                break;
            case ProjectileType.Straight:
                _rigid.velocity = transform.forward * speed;
                break;
            default:
                break;
        }

        Destroy(gameObject, 5f);
    }

    /// <summary>
    /// Arched�� ����ü ���� �浹 �̺�Ʈ �޼����Դϴ�.
    /// </summary>
    /// <param name="collision">�浹 ���</param>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Straight�� ����ü ���� �浹 �̺�Ʈ �޼����Դϴ�.
    /// </summary>
    /// <param name="other">�浹 ���</param>
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            shooter.ApplyDamageInfoToPlayer();
            Destroy(gameObject);
        }

        //if (Type == ProjectileType.Straight &&
        //    other.gameObject.layer == LayerMask.NameToLayer("Shield"))
        //{
        //    Debug.Log("BLOCKED!!");
        //    Destroy(gameObject);
        //}

        if (Type == ProjectileType.Arched &&
            other.gameObject.layer == LayerMask.NameToLayer("Shield_Destroyer"))
        {
            Debug.Log("BLOCKED!!");
            Destroy(gameObject);
        }
    }

    public void Inst
        (
        MonsterController shooter,
        Vector3 staticTargetPosition
        )
    {
        this.shooter = shooter;
        this.staticTargetPosition = staticTargetPosition;
        Debug.Log($"ME{transform.position}TARGET{staticTargetPosition}");
        Shoot();
    }

    public void Inst(MonsterController shooter)
    {
        this.shooter = shooter;
        Shoot();
    }
}
