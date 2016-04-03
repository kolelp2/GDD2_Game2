using UnityEngine;
using System.Collections;

public class MinimapCollider : MonoBehaviour {
    [SerializeField]
    //float depth = -15;
    MapInfo mi;
    Camera mm;
	// Use this for initialization
	void Start () {
        mi = (MapInfo)GameObject.Find("Map").GetComponent(typeof(MapInfo));
        mm = (Camera)GameObject.Find("Minimap").GetComponent(typeof(Camera));
        BoxCollider2D bc = (BoxCollider2D) GetComponent(typeof(BoxCollider2D));
        bc.size = mi.MapSize;
        transform.position += new Vector3(0, 0, 1f);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnMouseOver()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 worldPoint = mm.ScreenToWorldPoint(Input.mousePosition);
            if (mi.IsWorldPosOnMap(worldPoint))
            {
                Transform cTransform = Camera.main.transform;
                cTransform.position = new Vector3(worldPoint.x, worldPoint.y, cTransform.position.z);
            }
        }
    }
}
