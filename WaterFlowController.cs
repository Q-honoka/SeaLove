using UnityEngine;

public class WaterFlowController : MonoBehaviour
{
    [SerializeField, Header("範囲内に入ったものに加える力の強さ")]
    float flowForce;

    [SerializeField, Header("押し出す方向(どれか1つの軸に1を入れてください)")]
    Vector3 pushDirection;

    private Rigidbody rb;

    /// <summary>
    /// コライダーの範囲内にいる間、力を加える
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerStay(Collider other)
    {
        Debug.Log("範囲内");
        if(other.transform.parent.gameObject.CompareTag("Player"))
        {
            rb = other.GetComponentInParent<Rigidbody>();

            // 衝突した相手に力を加える
            if (rb != null)
            {
                Debug.Log("流す");
                rb.AddForce(pushDirection * flowForce, ForceMode.Acceleration);
            }
        }
    }
}
