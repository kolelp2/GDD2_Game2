using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
    [SerializeField]
    int updateInterval = 10;
    int updateSeed;
    
	// Use this for initialization
	void Start () {
        myMover = GetComponent<Mover>();
        GameObject map = GameObject.Find("Map");
        myCPM = (CPManager)map.GetComponent(typeof(CPManager));
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
        updateSeed = (int)Math.Round(UnityEngine.Random.value * (updateInterval-1));
	}
	
	// Update is called once per frame
	void Update () {
        //if we don't have a target, or we've been chasing the current target for longer than the chase time
        if (target == null || lastTargetTime - DateTime.Now > new TimeSpan(0, 0, chaseTime))
        {
            //if we aren't already looking for a target, start doing that
            if (Time.frameCount % updateInterval == updateSeed) CheckForTarget();
            //if(!checkingForTarget) StartCoroutine("CheckForTarget");

            //if we don't have a target, the CPM tells us where to go
            Vector2 targetVel = myCPM.GetVectorAtPosition(transform.position);
            if (targetVel != Vector2.zero) myMover.SetVelocity(targetVel, 1);
        }
        else
        {
            if ((target.transform.position - transform.position).sqrMagnitude <= reach) target.Tag();
            //if we have a target, chase it
            myMover.SetVelocity(target.transform.position - transform.position, 1);
        }

        if (Time.frameCount % updateInterval == updateSeed+1)
            myGOT.Report(this, (int)ObjectType.Zombie);
    }

    void CheckForTarget()
    {

        //get nearby game objects from the GO tracker
        List<MonoBehaviour> nearbyMBs = myGOT.GetObjsInRange(transform.position, viewDistance, (int)ObjectType.Human);

        //determine how many checks we'll make per rame
        //int checksPerFrame = (int)Math.Ceiling(nearbyMBs.Count / (float)targetCheckInterval);

        //temp values
        Human closestHuman = null;
        float closestHumanDistanceSqr = float.MaxValue;

        //for each GO...
        for(int c=0;c<nearbyMBs.Count;c++)
        {
            //cast to human
            Human goHuman = (Human)nearbyMBs[c];
            //if it does, get the distance to it
            float goHumanDistanceSqr = (transform.position - goHuman.transform.position).sqrMagnitude;
            //if it's closer than the current closest and within view distance, make it the new closest
            if (goHumanDistanceSqr < closestHumanDistanceSqr/* && goHumanDistanceSqr < viewDistance * viewDistance*/)
            {
                closestHuman = goHuman;
                closestHumanDistanceSqr = goHumanDistanceSqr;
            }
        }
        //closest human is the target. note the time the new target was acquired
        target = closestHuman;
        lastTargetTime = DateTime.Now;
        
    } 
}
