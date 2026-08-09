using UnityEngine;

public class MainMenuCameraFloat : MonoBehaviour
{
    public Vector3 targetCenter = Vector3.zero;
    public float orbitSpeed = 1.2f; // slow elegant sweep
    public float floatSpeed = 0.6f;
    public float floatAmplitude = 0.12f;
    public float distance = 3.2f;

    private float angle = 45f;
    private float startY;

    void Start()
    {
        startY = transform.position.y;
    }

    void Update()
    {
        angle += orbitSpeed * Time.deltaTime;
        float radian = angle * Mathf.Deg2Rad;
        
        float x = targetCenter.x + Mathf.Cos(radian) * distance;
        float z = targetCenter.z + Mathf.Sin(radian) * distance;
        float y = startY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        
        transform.position = new Vector3(x, y, z);
        
        // Soft target tracking slightly above the table surface
        transform.LookAt(targetCenter + new Vector3(0f, 0.25f, 0f));
    }
}
