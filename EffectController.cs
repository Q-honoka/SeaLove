using UnityEngine;

public class EffectController : MonoBehaviour
{
    private Rigidbody rb;
    public GameObject dushEffectOb;
    public ParticleSystem dushEffect;

    private float speedThreshould = 2f;
    private bool isDush = false;
    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        dushEffect = dushEffectOb.GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        isDush = rb.velocity.magnitude > speedThreshould ? true : false;

        if(isDush)
        {
            dushEffect.Play();
        }
        else
        {
            dushEffect.Stop();
        }
    }
}
