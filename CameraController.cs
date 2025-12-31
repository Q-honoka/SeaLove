using UnityEngine;

public class CameraController : MonoBehaviour
{
    private GameObject cameraOb;

    // í«â¡ïœêî
    private Camera cam;
    private Rigidbody rb;

    private float defaultFOV;
    public float maxFOV = 70f;
    private float speedThreshould = 2f;
    private float transitionDuration = 0.2f;
    private float fovVelocity = 0;

    // Start is called before the first frame update
    void Start()
    {
        cameraOb = this.transform.Find("Camera").gameObject;

        // í«â¡
        if(cameraOb != null )
        {
            cam = cameraOb.GetComponent<Camera>();
        }
        rb = GetComponent<Rigidbody>();
        defaultFOV = cam.fieldOfView;
    }

    // Update is called once per frame
    void Update()
    {
        // â¡ë¨éûÇ…ÉJÉÅÉâÇÃéãñÏÇçLÇ≠Ç∑ÇÈ
        if( cam != null )
        {
            if (rb.velocity.magnitude > speedThreshould)
            {
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, maxFOV, ref fovVelocity, transitionDuration);
            }
            else
            {
                cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, defaultFOV, ref fovVelocity, transitionDuration);
            }
        }
    }
}
