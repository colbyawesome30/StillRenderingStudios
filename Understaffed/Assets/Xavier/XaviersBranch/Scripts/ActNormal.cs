using UnityEngine;

public class ActNormal : MonoBehaviour
{
    public Vector3 minScale = new Vector3(0.9f, 0.9f, 0.9f);
    public Vector3 maxScale = new Vector3(1.1f, 1.1f, 1.1f);

    // How fast the object bounces
    public float speed = 1f;

    // Shifts the timing of the bounce. Useful if you have many objects
    public float Offset = .5f;

    //BaseSize
    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        //Goes up and down between 0 and 1 over time.
        float t = (Mathf.Sin((Time.time + Offset) * speed * Mathf.PI * 2f) + 1f) * 0.5f;

        // Start with a blank scale to fill in below
        Vector3 target = baseScale;

        // Blend each axis between minScale and maxScale
        target.x = Mathf.Lerp(minScale.x, maxScale.x, t);
        target.y = Mathf.Lerp(minScale.y, maxScale.y, t);
        target.z = Mathf.Lerp(minScale.z, maxScale.z, t);

        // Apply the new size to the object every frame
        transform.localScale = target;
    }
}