using UnityEngine;
using System.Collections.Generic;
using System;

public class Zombie : MonoBehaviour {
    Mover myMover;
    CPManager myCPM;
    GOTracker myGOT;
    Human target = null;
    DateTime lastTargetTime = new DateTime();
    [SerializeField]
    float viewDistance = 5.0f;
    [SerializeField]
    int chaseTime = 7;
    [SerializeField]
    float reach = .1f;
	// Use this for initialization
	void Start () {
        myMover = GetComponent<Mover>();
        GameObject map = GameObject.Find("Map");
        myCPM = map.GetComponent<CPManager>();
        myGOT = map.GetComponent<GOTracker>();
	}
	
	// Update is called once per frame
	void Update () {
        //if we don't have a target, or we've been chasing the current target for longer than the chase time
        if (target == null || lastTargetTime - DateTime.Now > new TimeSpan(0, 0, chaseTime))
        {
            //get nearby game objects from the GO tracker
            List<GameObject> nearbyGOs = myGOT.GetObjsWithinRange(transform.position, viewDistance);
            Human closestHuman = null;
            float closestHumanDistanceSqr = float.MaxValue;
            //for each GO...
            foreach (GameObject go in nearbyGOs)
            {
                //check if it has a human component
                Human goHuman = go.GetComponent<Human>();
                if (goHuman != null)
                {
                    //if it does, get the distance to it
                    float goHumanDistanceSqr = (transform.position - goHuman.transform.position).sqrMagnitude;
                    //if it's closer than the current closest, make it the new closest
                    if (goHumanDistanceSqr < closestHumanDistanceSqr)
                    {
                        closestHuman = goHuman;
                        closestHumanDistanceSqr = goHumanDistanceSqr;
                    }
                }
            }
            //closest human is the target. note the time the new target was acquired
            target = closestHuman;
            lastTargetTime = DateTime.Now;

            //if we don't have a target, the CPM tells us where to go
            myMover.SetVelocity(myCPM.GetVectorAtPosition(transform.position), 1);
        }
        else
        {
            if ((target.transform.position - transform.position).sqrMagnitude <= reach) target.Tag();
            //if we have a target, chase it
            myMover.SetVelocity(target.transform.position - transform.position, 1);
        }

        myGOT.Report(gameObject);
    }
}
