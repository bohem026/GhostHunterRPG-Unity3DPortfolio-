using UnityEngine;

public sealed class SlashEffectController : MonoBehaviour
{
    private const float SPEED = 10f;

    private void Update()
    {
        // ���� Z(����) �������� ������ ����Ʈ�� �̵�
        transform.Translate(Vector3.forward * SPEED * Time.deltaTime, Space.Self);
    }
}
