using UnityEngine;
using System.Collections;

public class ControlPoint : MonoBehaviour {
    CPManager cpm;
    bool dragging = false; //is this point being mouse-dragged?
    public static float drawDepth = -1.0f;

	// Use this for initialization
	void Start () {
        cpm = GameObject.Find("GameManager").GetComponent<CPManager>();
        cpm.AddCP(this);
	}
	
	// Update is called once per frame
	void Update ()
    {
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
