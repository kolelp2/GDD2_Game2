using UnityEngine;
using System.Collections;

public class ControlPoint : MonoBehaviour {
    CPManager cpm;
    SpriteRenderer sr;
    bool dragging = false; //is this point being mouse-dragged?
    Color color;
    GameObject mmCP;
    [SerializeField]
    float mmCPScale = 15;
    [SerializeField]
    static float drawDepth = -3.0f;

    Transform myTransform;

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
        myTransform = transform;
        GameObject map = GameObject.Find("Map");
        cpm = (CPManager)map.GetComponent(typeof(CPManager));
        sr = (SpriteRenderer)GetComponent(typeof(SpriteRenderer));
        mmCP = (GameObject)Instantiate(Resources.Load("mm control point"));
        mmCP.transform.position = transform.position;
        mmCP.transform.localScale = new Vector3(mmCPScale, mmCPScale, mmCPScale);
        //cpm.AddCP(this);
	}
	
	// Update is called once per frame
	void Update ()
    {
        myTransform.localScale = new Vector3(1,1,1) * (1+(Camera.main.orthographicSize/3));
        //if we're being mouse-dragged...
	    if (dragging)
        {
            //set our position to the mouse's X and Y, and use draw depth for Z
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = drawDepth;
            myTransform.position = mousePos;
            mmCP.transform.position = myTransform.position;
            //request a VF recalculation since we moved a control point
            cpm.QueueVFRecalculation();
        }
    }

    public IEnumerator SetColor(Color color)
    {
        while(sr == null)
            yield return null;
        sr.color = color;
        ((SpriteRenderer)mmCP.GetComponent(typeof(SpriteRenderer))).color = color;
    }

    void OnDestroy()
    {
        Destroy(mmCP);
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
