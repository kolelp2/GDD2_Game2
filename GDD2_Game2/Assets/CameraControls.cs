using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public float zoomSpeed = 1;
    public float targetOrtho;
    public float smoothSpeed = 2.0f;
    public float minOrtho = 1.0f;
    public float maxOrtho = 20.0f;
    public Vector3 targetPosition;
    float multiplier = 0.0f;
    public float scrollSpeed = 1.0f;
    [SerializeField]
    float panSpeed = 2;

    void Start()
    {
        targetOrtho = Camera.main.orthographicSize;
        targetPosition = gameObject.transform.position;
    }

    void Update()
    {
        //get scroll amount
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            //increment target zoom
            targetOrtho -= scroll * zoomSpeed;
            //clamp
            targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);
        }

        //move toward target zoom
        Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, targetOrtho, smoothSpeed * Time.deltaTime);

        //camera movement for this frame
        Vector2 cameraMovement = new Vector2(0, 0);
        //add directional vectors for any movement keys
        if (Input.GetKey(KeyCode.W)) cameraMovement += new Vector2(0, panSpeed);
        if (Input.GetKey(KeyCode.A)) cameraMovement += new Vector2(-panSpeed, 0);
        if (Input.GetKey(KeyCode.S)) cameraMovement += new Vector2(0, -panSpeed);
        if (Input.GetKey(KeyCode.D)) cameraMovement += new Vector2(panSpeed, 0);

        //normalize the final vector and multiply by scroll speed
        cameraMovement = cameraMovement.normalized * scrollSpeed * panSpeed * (1 + .05f * targetOrtho);

        transform.position += new Vector3(cameraMovement.x, cameraMovement.y, 0);
    }
}