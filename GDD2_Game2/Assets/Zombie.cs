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
    int chaseTime = 1;
    [SerializeField]
    float reach = .1f;
    [SerializeField]
    int updateInterval = 10;
    int updateSeed;
    static int attackCD = 2;
    int lastAttack = 0;
    float drawDepth = -.02f;
    //ratio between distance to nearest CP and distance to target below which the cp will be preferred
    //so a ratio of 3 means that the nearest control point will be preferred if it is less than 3 times as far away as the target
    static float cpBoundingRatio = 1;
    
	// Use this for initialization
	void Start () {
        myMover = GetComponent<Mover>();
        GameObject map = GameObject.Find("Map");
        myCPM = (CPManager)map.GetComponent(typeof(CPManager));
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
        myGOT.ReportCreation(ObjectType.Zombie);
        updateSeed = (int)Math.Round(UnityEngine.Random.value * (updateInterval-1));
        transform.position += new Vector3(0, 0, drawDepth);
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
            //tag
            if (Time.frameCount-lastAttack>attackCD &&(target.transform.position - transform.position).sqrMagnitude <= reach)
            {
                lastAttack = Time.frameCount;
                target.Tag(UnityEngine.Random.value);
            }
            float cpDistanceSqr = myCPM.GetDistanceSqrFromCPAtPos(transform.position);
            float targetDistanceSqr = ((Vector2)target.transform.position - (Vector2)transform.position).sqrMagnitude;

            myMover.SetVelocity(target.transform.position - transform.position, 1);
            //if the ratio between the distance to the cp and the distance to the target is greater than the bounding ratio, lose the target
            if (cpDistanceSqr / targetDistanceSqr < cpBoundingRatio)
                target = null;
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

    public void Tag(float failRate, out bool success)
    {
        success = false;
        //zombie rolls. If it's below the provided fail rate, the zombie lives
        if (!(UnityEngine.Random.value < failRate))
        {
            success = true;
            Destroy(gameObject);
            //Destroy(this);
        }
    }

    void OnDestroy()
    {
        myGOT.ReportDeath(this, ObjectType.Zombie);
    }
}
