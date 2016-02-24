using UnityEngine;
using System.Collections;

public class CPSurface : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnMouseDown()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 newPos = new Vector3(mousePos.x, mousePos.y, ControlPoint.drawDepth);
        Instantiate(Resources.Load("control point"), newPos, Quaternion.identity);
    }
}
