using UnityEngine;

public class FishController : MonoBehaviour
{
    [SerializeField]
    GameObject[] WayPoints;

    [SerializeField]
    float moveSpeed = 1.0f;
    private int currentIdx = 0;

    // Update is called once per frame
    void Update()
    {
        if (WayPoints.Length == 0) return;

        Transform target = WayPoints[currentIdx].transform;
        Vector3 dir = (target.position - transform.position).normalized;
        float step = moveSpeed * Time.deltaTime;
        if (currentIdx < WayPoints.Length)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, step);
            
            if(dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0, -90, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
            }

            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                currentIdx = (currentIdx + 1) % WayPoints.Length;
            }
        }
    }
}
