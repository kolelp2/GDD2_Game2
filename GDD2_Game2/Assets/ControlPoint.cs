using UnityEngine;
using System.Collections;

public class ControlPoint : MonoBehaviour {
    CPManager cpm;

	// Use this for initialization
	void Start () {
        cpm = GameObject.Find("GameManager").GetComponent<CPManager>();
        cpm.AddCP(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnDestroy()
    {
        cpm.RemoveCP(this);
    }
}
