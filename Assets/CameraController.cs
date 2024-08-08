using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;       // Speed of the camera panning
    public float zoomSpeed = 5f;       // Speed of zooming (changing the Z position)
    public float minZoom = -10f;       // Minimum Z position (zoom limit)
    public float maxZoom = -200f;      // Maximum Z position (zoom limit)

    private Vector3 lastPosition;

    void Update()
    {
        PanCamera();
        ZoomCamera();
    }

    void PanCamera()
    {
        if (Input.GetMouseButtonDown(0))  // Left mouse button clicked
        {
            lastPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 delta = lastPosition - Input.mousePosition;
            transform.Translate(delta.x * -transform.position.z / 1000, delta.y * -transform.position.z / 1000, 0);
            lastPosition = Input.mousePosition;
        }


    }

    void ZoomCamera()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            Vector3 position = transform.position;
            position.z += scroll * zoomSpeed * 100f * Time.deltaTime; // Adjust zoom speed by scaling with Time.deltaTime
            position.z = Mathf.Clamp(position.z, maxZoom, minZoom);  // Clamp the Z position within the limits
            transform.position = position;
        }
    }
}