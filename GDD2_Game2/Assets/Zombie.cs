using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour {
    Mover myMover;
    CPManager myCPM;
	// Use this for initialization
	void Start () {
        myMover = GetComponent<Mover>();
        myCPM = GameObject.Find("GameManager").GetComponent<CPManager>();
	}
	
	// Update is called once per frame
	void Update () {
        myMover.Velocity = myCPM.GetVectorAtPosition(transform.position);
	}
}
