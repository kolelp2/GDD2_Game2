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
    Vector2[] cameraCorners = new Vector2[4];
    MapInfo mi;
    Transform myTransform;
    LineRenderer lr;
    void Start()
    {
        myTransform = transform;
        targetOrtho = Camera.main.orthographicSize;
        targetPosition = myTransform.position;
        mi = (MapInfo)GameObject.Find("Map").GetComponent(typeof(MapInfo));
        lr = (LineRenderer)GetComponent(typeof(LineRenderer));

        //calculate max zoom
        float cameraAR = Camera.main.aspect;
        float mapWhRatio = mi.MapSize.x / mi.MapSize.y;
        float cameraToMapRatio = cameraAR / mapWhRatio;
        float maxOrthoByMapSize = (cameraToMapRatio >= 1) ? (mi.MapSize.x / cameraAR) / 2 : (mi.MapSize.y / 2);
        if (maxOrthoByMapSize < maxOrtho)
            maxOrtho = maxOrthoByMapSize-10;
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
        if (Input.GetKey(KeyCode.W))
            cameraMovement += new Vector2(0, panSpeed);
        if (Input.GetKey(KeyCode.A))
            cameraMovement += new Vector2(-panSpeed, 0);
        if (Input.GetKey(KeyCode.S))
            cameraMovement += new Vector2(0, -panSpeed);
        if (Input.GetKey(KeyCode.D))
            cameraMovement += new Vector2(panSpeed, 0);
        if (Input.GetKey(KeyCode.Z)) targetOrtho -= .1f * zoomSpeed;
        else if (Input.GetKey(KeyCode.X)) targetOrtho += .1f * zoomSpeed;

        //exit game
        if (Input.GetKey(KeyCode.Escape)) Application.Quit();

        //normalize the final vector and multiply by scroll speed
        cameraMovement = cameraMovement.normalized * scrollSpeed * panSpeed * (1 + .05f * targetOrtho);
        
        myTransform.position += new Vector3(cameraMovement.x, cameraMovement.y, 0);

        //bounds checking
        //get corner coordinates
        cameraCorners[0] = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        cameraCorners[1] = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0));
        cameraCorners[2] = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0));
        cameraCorners[3] = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

        //for each corner
        foreach(Vector2 corner in cameraCorners)
        {
            //get its distance from the map
            Vector2 point = mi.GetNearestPointOnMap(corner);
            //if the distance isn't zero, move the camera back by half the distance
            if (point.x - corner.x != 0 || point.y - corner.y != 0)
            {
                //we have to do half the distance because the corners will register as being out of bounds in pairs
                //so if we're off the right side of the map, the top-right and bottom-right will both trigger a counter-movement
                myTransform.position += (Vector3)(point - corner)/2;
            }
        }

        Vector3 p1 = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 p2 = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0));
        Vector3 p3 = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3 p4 = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 0));

        lr.SetPositions(new Vector3[] { p1, p2, p3, p4, p1, p4, p3, p2, p1 });
    }
}