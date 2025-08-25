using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    /// <summary>�߻�ü ���� ���.</summary>
    private enum ProjectileType { Arched, Straight, Count /*Length*/ }

    [SerializeField] private ProjectileType Type;
    [SerializeField] private float speed;

    private MonsterController shooter;
    private Rigidbody _rigid;
    private Vector3 staticTargetPosition = Vector3.zero;

    // ===== �ܺο��� ȣ���ϴ� ����/�߻� API =====

    /// <summary>
    /// (��������) ������ ��ǥ ������ ������ �߻��մϴ�.
    /// </summary>
    public void Inst(MonsterController shooter, Vector3 staticTargetPosition)
    {
        this.shooter = shooter;
        this.staticTargetPosition = staticTargetPosition;
        Debug.Log($"ME{transform.position}TARGET{staticTargetPosition}");
        Shoot();
    }

    /// <summary>
    /// (������ ��) ���� �������� �߻��մϴ�.
    /// </summary>
    public void Inst(MonsterController shooter)
    {
        this.shooter = shooter;
        Shoot();
    }

    // ===== ���� ���� =====

    /// <summary>
    /// Rigidbody�� �̿��� Ÿ�Կ� �´� �ʱ� �ӵ�/���� �ο��ϰ�,
    /// ���� �ð� �� ������ �����մϴ�.
    /// </summary>
    private void Shoot()
    {
        _rigid = GetComponent<Rigidbody>();

        switch (Type)
        {
            // ������: ��ǥ ���� + �ణ�� ���� ������ ���� ����
            case ProjectileType.Arched:
                Vector3 dirVec = staticTargetPosition - transform.position;
                dirVec += Vector3.up * Random.Range(0.8f, 1f);
                _rigid.AddForce(dirVec.normalized * speed, ForceMode.Impulse);
                _rigid.AddTorque(Vector3.back * -10f, ForceMode.Impulse);
                break;

            // ����: ���� �ӵ� �ο�
            case ProjectileType.Straight:
                _rigid.velocity = transform.forward * speed;
                break;
        }

        // ������ġ: 5�� �� �ڵ� ����
        Destroy(gameObject, 5f);
    }

    // ===== ����Ƽ ���� �̺�Ʈ =====

    /// <summary>
    /// (��������) ��Ʈ���� �ݶ��̴� �浹 ó��.
    /// �÷��̾�/���� ������ ����.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
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
    /// (������) Ʈ���� �浹 ó��.
    /// �÷��̾ ������ ������ ���� �� ����.
    /// ���������� 'Shield_Destroyer' ���̾ ������ ���� ó��.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            shooter.ApplyDamageInfoToPlayer();
            Destroy(gameObject);
        }

        if (Type == ProjectileType.Arched &&
            other.gameObject.layer == LayerMask.NameToLayer("Shield_Destroyer"))
        {
            Debug.Log("BLOCKED!!");
            Destroy(gameObject);
        }
    }
}
