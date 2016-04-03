using UnityEngine;
using System.Collections;

public class Minimap : MonoBehaviour {
    MapInfo mi;
    [SerializeField]
    float minimapSize = .2f;
    // Use this for initialization
    void Start()
    {
        mi = (MapInfo)GameObject.Find("Map").GetComponent(typeof(MapInfo));
        Camera minimap = (Camera)GameObject.Find("Minimap").GetComponent(typeof(Camera));
        minimap.transform.position = mi.MapPos + mi.MapSize / 2;
        float mapWhRatio = mi.MapSize.x / mi.MapSize.y;
        minimap.orthographicSize = mi.MapSize.x / (mapWhRatio * 2);
        Vector2 viewPort = new Vector2(minimapSize, ((minimapSize * Screen.width) * (1 / mapWhRatio)) / Screen.height);
        minimap.rect = new Rect(1 - viewPort.x, 1 - viewPort.y, viewPort.x, viewPort.y);
        minimap.transform.position += new Vector3(0, 0, -20);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
