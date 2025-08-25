using UnityEngine;

public sealed class SlashEffectController : MonoBehaviour
{
    private const float SPEED = 10f;

    private void Update()
    {
        // 로컬 Z(전방) 방향으로 슬래시 이펙트를 이동
        transform.Translate(Vector3.forward * SPEED * Time.deltaTime, Space.Self);
    }
}
