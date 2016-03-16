using UnityEngine;
using System.Collections;

public class ControlPoint : MonoBehaviour {
    CPManager cpm;
    SpriteRenderer sr;
    bool dragging = false; //is this point being mouse-dragged?
    Color color;
    [SerializeField]
    static float drawDepth = -1.0f;

    float strength = 1;
    public float Strength
    {
        get { return strength; }
        set { strength = value; }
    }

    public static float DrawDepth
    {
        get { return drawDepth; }
    }

	// Use this for initialization
	void Start () {
        GameObject map = GameObject.Find("Map");
        cpm = (CPManager)map.GetComponent(typeof(CPManager));
        sr = (SpriteRenderer)GetComponent(typeof(SpriteRenderer));
        //cpm.AddCP(this);
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.localScale = new Vector3(1,1,1) * (1+(Camera.main.orthographicSize/3));
        //if we're being mouse-dragged...
	    if (dragging)
        {
            //set our position to the mouse's X and Y, and use draw depth for Z
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = drawDepth;
            transform.position = mousePos;
            //request a VF recalculation since we moved a control point
            cpm.QueueVFRecalculation();
        }
    }

    public void SetColor(Color color)
    {
        sr.color = color;
    }

    void OnDestroy()
    {
        cpm.RemoveCP(this);
    }

    void OnMouseOver()
    {
        //right click destroys
        if (Input.GetMouseButtonDown(1) && !dragging)
            Destroy(gameObject);
        //left down enables dragging, left up disables it
        else if (Input.GetMouseButtonDown(0)) dragging = true;
        else if (Input.GetMouseButtonUp(0)) dragging = false;
    }
}
