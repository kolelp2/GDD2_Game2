using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Human : MonoBehaviour {
    enum HumanState { Combat, Seeking, Idle }

    HumanState state = HumanState.Idle;
    Mover myMover;
    GOTracker myGOT;

    List<MonoBehaviour> nearbyZombies = new List<MonoBehaviour>();
    Zombie targetZombie = null;

    List<MonoBehaviour> nearbyHumans = new List<MonoBehaviour>();

    //3 for the three resource node types
    HashSet<Vector2>[] resourceLocationsByType = new HashSet<Vector2>[GOTracker.resourceNodeTypeCount];

    //a dead location is one where there should be a resource/camp, but isn't
    HashSet<Vector2> deadLocations = new HashSet<Vector2>(); //used to remove entries from the resource/camp location hash sets, values are moved to archive after use
    HashSet<Vector2> deadLocationsArchive = new HashSet<Vector2>(); //gets passed around with greetings

    HashSet<Vector2> campLocations = new HashSet<Vector2>();

    int[] inventory = new int[Enum.GetNames(typeof(ResourceTypeAll)).Length];

    Vector2? targetPos = null;
    float targetPosTolerance = 0;

    [SerializeField]
    int humanCheckInterval = 60;
    int humanCheckSeed;
    [SerializeField]
    float humanCheckDistance = 5.0f;
    [SerializeField]
    int greetInterval = 120;
    int greetSeed;
    [SerializeField]
    int zombieCheckInterval = 120;
    int zombieCheckSeed;
    [SerializeField]
    int targetPickInterval = 60;
    int targetPickSeed;
    [SerializeField]
    float zombieCheckDistance = 10.0f;
    [SerializeField]
    int resourceNodeCheckInterval = 480;
    int[] resourceNodeCheckSeed = new int[GOTracker.resourceNodeTypeCount];
    [SerializeField]
    float resourceNodeCheckDistance = 50.0f;
    [SerializeField]
    int campCheckInterval = 480;
    int campCheckSeed;
    [SerializeField]
    float campCheckDistance = 50.0f;
    [SerializeField]
    int decisionMakingInterval = 240;
    int decisionMakingSeed;

    // Use this for initialization
    void Start () {
        //grab scripts
        myMover = (Mover)GetComponent(typeof(Mover));
        GameObject map = GameObject.Find("Map");
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));

        //subscribe to day events
        MapInfo.DayEndEvent += OnDayEnd;

        //initialize inventory and node location array
        for (int c = 0; c < inventory.Length; c++)
            inventory[c] = 0;
        for (int c = 0; c < resourceLocationsByType.Length; c++)
            resourceLocationsByType[c] = new HashSet<Vector2>();

        //generate polling seeds
        humanCheckSeed = (int)Math.Round(UnityEngine.Random.value * (humanCheckInterval));
        greetSeed = (int)Math.Round(UnityEngine.Random.value * (greetInterval));
        zombieCheckSeed = (int)Math.Round(UnityEngine.Random.value * (zombieCheckInterval - 1));
        targetPickSeed = (int)Math.Round(UnityEngine.Random.value * (targetPickInterval));
        for (int c = 0; c < resourceNodeCheckSeed.Length; c++) //this array holds a seed for each raw resource type
            resourceNodeCheckSeed[c] = (int)Math.Round(UnityEngine.Random.value * (resourceNodeCheckInterval));
        campCheckSeed = (int)Math.Round(UnityEngine.Random.value * (campCheckInterval));
        decisionMakingSeed = (int)Math.Round(UnityEngine.Random.value * (decisionMakingInterval));
    }
	
	// Update is called once per frame
	void Update () {
        int startResource = (int)ResourceTypeAll.FoodProcessed;
        for(int c = startResource;c<=startResource+GOTracker.resourceNodeTypeCount;c++)
            if (inventory[c] < 0)
            {
                myGOT.ReportDeath(this, ObjectType.Human);
                Destroy(this);
            }
        //attack
        if (targetZombie != null)
        {
            //set targetpos to a combination of away from zombie and towards nearest/strongest camp

            //tag the zombie
        }

        //zombie check
        if (Time.frameCount % zombieCheckInterval == zombieCheckSeed)
            nearbyZombies = myGOT.GetObjsInRange(transform.position, zombieCheckDistance, ObjectType.Zombie);

        //target pick
        if (Time.frameCount % targetPickInterval == targetPickSeed)
            targetZombie = PickTarget();

        //resource node checks
        int resourceNodeCheckMod = Time.frameCount % resourceNodeCheckInterval; //save the mod
        for(int c = 0;c<resourceNodeCheckSeed.Length;c++)//if the mod matches one of the seeds
            if(resourceNodeCheckMod == resourceNodeCheckSeed[c])
            {
                //add any in-range nodes of that resource to our hash set
                List<MonoBehaviour> nearbyResourceNodes = myGOT.GetObjsInRange(transform.position, resourceNodeCheckDistance, c);
                foreach (MonoBehaviour mb in nearbyResourceNodes)
                    resourceLocationsByType[c].Add(mb.gameObject.transform.position);
            }

        //camp check
        if(Time.frameCount%campCheckInterval==campCheckSeed)
        {
            List<MonoBehaviour> nearbyCamps = myGOT.GetObjsInRange(transform.position, campCheckDistance, ObjectType.Camp);
            foreach (MonoBehaviour mb in nearbyCamps)
                campLocations.Add(mb.gameObject.transform.position);
        }

        //human check
        if (Time.frameCount % humanCheckInterval == humanCheckSeed)
            nearbyHumans = myGOT.GetObjsInRange(transform.position, humanCheckDistance, ObjectType.Human);

        //greeting
        if (Time.frameCount % greetInterval == greetSeed)
            foreach (MonoBehaviour mb in nearbyHumans)
                if (mb != null)
                    Greet((Human)mb);

        //movement
        myMover.SetVelocity(new Vector2(0, 0), 0);
        if (state == HumanState.Seeking)
        {
            if ((targetPos - (Vector2)transform.position).Value.sqrMagnitude < targetPosTolerance * targetPosTolerance)
            {
                state = HumanState.Idle;
                targetPosTolerance = 0;
            }
            else
                myMover.SetVelocity((Vector2)(targetPos - (Vector2)gameObject.transform.position), 1.0f);
        }

        //decision making
        //maybe take out this conditional, humans should probably reevaluate decisions constantly, especially with the possibility of new greet information
        else if(state == HumanState.Idle || deadLocations.Count>0)
        {
            //remove dead locations from resource node and camp sets
            foreach(Vector2 deadLocation in deadLocations)
            {
                for (int c = 0; c < resourceLocationsByType.Length; c++)
                    resourceLocationsByType[c].Remove(deadLocation);
                campLocations.Remove(deadLocation);
                deadLocationsArchive.Add(deadLocation); //make sure to add each dead location to the archive
            }
            

            //keep at the end of the decision making block
            if (state == HumanState.Idle && targetPos != null)
                deadLocations.Add(targetPos.Value);
        }

        //reporting
        if (Time.frameCount % 3 == 0)
            myGOT.Report(this, ObjectType.Human);
	}

    public void Tag()
    {
        //change color
        SpriteRenderer sr = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        sr.color = new Color(189.0f / 255.0f, 189.0f / 255.0f, 189.0f / 255.0f);
        //report death to GO tracker
        myGOT.ReportDeath(this, (int)ObjectType.Human);
        //add zombie script
        gameObject.AddComponent(typeof(Zombie));
        //destroy this script
        Destroy(this);
    }

    Zombie PickTarget()
    {
        return null;
    }

    void Greet(Human other, bool first = true)
    {
        //get new camp locations
        campLocations.UnionWith(other.campLocations);
        //get new node locations for each resource
        for (int c = 0; c < resourceLocationsByType.Length; c++)
            resourceLocationsByType[c].UnionWith(other.resourceLocationsByType[c]);
        //any dead locations the other has that this one doesn't get added to the dead locations set
        foreach (Vector2 v in other.deadLocationsArchive)
            if (!deadLocationsArchive.Contains(v))
                deadLocations.Add(v);
        //if we're the first, they greet us as the second
        if (first) other.Greet(this, false);
    }

    void OnDayEnd(object sender, DayEndEventArgs dea)
    {
        //we're starting at the processed resource section of the inventory
        int startResource = (int)ResourceTypeAll.FoodProcessed;
        //loop through and subtract 1 from every processed resource
        for (int c = 0; c <= GOTracker.resourceNodeTypeCount; c++)
            inventory[c + startResource]--;
    }
}

public enum ResourceTypeAll { FoodRaw = ObjectType.Food, WaterRaw = ObjectType.Water, Fuel = ObjectType.Fuel, Ammo = 3, FoodProcessed = 4, WaterProcessed = 5, FuelProcessed = 6 }
