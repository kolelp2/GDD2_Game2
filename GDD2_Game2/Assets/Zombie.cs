﻿using UnityEngine;
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
    int targetCheckInterval = 5;
    bool checkingForTarget = false;
	// Use this for initialization
	void Start () {
        myMover = GetComponent<Mover>();
        GameObject map = GameObject.Find("Map");
        myCPM = (CPManager)map.GetComponent(typeof(CPManager));
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
	}
	
	// Update is called once per frame
	void Update () {
        //if we don't have a target, or we've been chasing the current target for longer than the chase time
        if (target == null || lastTargetTime - DateTime.Now > new TimeSpan(0, 0, chaseTime))
        {
            //if we aren't already looking for a target, start doing that
            CheckForTarget();
            //if(!checkingForTarget) StartCoroutine("CheckForTarget");

            //if we don't have a target, the CPM tells us where to go
            myMover.SetVelocity(myCPM.GetVectorAtPosition(transform.position), 1);
        }
        else
        {
            if ((target.transform.position - transform.position).sqrMagnitude <= reach) target.Tag();
            //if we have a target, chase it
            myMover.SetVelocity(target.transform.position - transform.position, 1);
        }

        if (Time.frameCount % 3 == 0)
            myGOT.Report(this, typeof(Zombie));
    }

    void CheckForTarget()
    {
        //mark that we've started checking
        checkingForTarget = true;

        //get nearby game objects from the GO tracker
        List<MonoBehaviour> nearbyMBs = myGOT.GetObjs(transform.position, viewDistance, typeof(Human));

        //determine how many checks we'll make per rame
        int checksPerFrame = (int)Math.Ceiling(nearbyMBs.Count / (float)targetCheckInterval);

        //temp values
        Human closestHuman = null;
        float closestHumanDistanceSqr = float.MaxValue;

        //for each GO...
        int checksThisFrame = 0;
        foreach (MonoBehaviour go in nearbyMBs)
        {
            //if we've done all our checks for this frame...
            if (checksThisFrame == checksPerFrame)
            {
                //stop until the next one
                //yield return null;

                //when we come back, reset the count for this frame
                checksThisFrame = 0;
            }

            //cast to human
            Human goHuman = (Human)go;
            //if it does, get the distance to it
            float goHumanDistanceSqr = (transform.position - goHuman.transform.position).sqrMagnitude;
            //if it's closer than the current closest and within view distance, make it the new closest
            if (goHumanDistanceSqr < closestHumanDistanceSqr/* && goHumanDistanceSqr < viewDistance * viewDistance*/)
            {
                closestHuman = goHuman;
                closestHumanDistanceSqr = goHumanDistanceSqr;
            }

            //for every completed check, increment the count
            checksThisFrame++;

        }
        //closest human is the target. note the time the new target was acquired
        target = closestHuman;
        lastTargetTime = DateTime.Now;
        //yield return null;
        checkingForTarget = false;
        
    } 
}
