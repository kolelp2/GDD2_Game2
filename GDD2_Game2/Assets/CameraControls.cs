using UnityEngine;

public class CameraControls : MonoBehaviour
{
    public float zoomSpeed = 1000;
    public float targetOrtho;
    public float smoothSpeed = 50000000f;
    public float minOrtho = 1.0f;
    public float maxOrtho = float.MaxValue;
    public Vector3 targetPosition;
    float multiplier = 0.0f;
    public float scrollSpeed = 50f;
    public float zoomModifier = 2;
    [SerializeField]
    float panSpeed = 2;

    void Start()
    {
        targetOrtho = Camera.main.orthographicSize;
        targetPosition = gameObject.transform.position;
    }

    void Update()
    {
        //transform.position.Set(transform.position.x, transform.position.y, -10 - zoomModifier* Camera.main.orthographicSize);
        //get scroll amount
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            //increment target zoom
            targetOrtho -= scroll * zoomSpeed;
        }

        //clamp
        targetOrtho = Mathf.Clamp(targetOrtho, minOrtho, maxOrtho);

        //move toward target zoom
        Camera.main.orthographicSize = Mathf.MoveTowards(Camera.main.orthographicSize, targetOrtho, smoothSpeed * Time.deltaTime);

        //camera movement for this frame
        Vector2 cameraMovement = new Vector2(0, 0);
        //add directional vectors for any movement keys
        if (Input.GetKey(KeyCode.W)) cameraMovement += new Vector2(0, panSpeed);
        if (Input.GetKey(KeyCode.A)) cameraMovement += new Vector2(-panSpeed, 0);
        if (Input.GetKey(KeyCode.S)) cameraMovement += new Vector2(0, -panSpeed);
        if (Input.GetKey(KeyCode.D)) cameraMovement += new Vector2(panSpeed, 0);
        if (Input.GetKey(KeyCode.Z)) targetOrtho -= .1f * zoomSpeed;
        else if (Input.GetKey(KeyCode.X)) targetOrtho += .1f * zoomSpeed;

        //exit game
        if (Input.GetKey(KeyCode.Escape)) Application.Quit();

        //normalize the final vector and multiply by scroll speed
        cameraMovement = cameraMovement.normalized * scrollSpeed * panSpeed * (1 + .05f * targetOrtho);

        transform.position += new Vector3(cameraMovement.x, cameraMovement.y, 0);
    }
}