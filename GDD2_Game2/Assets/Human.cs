using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Human : MonoBehaviour {
    #region serialized fields
    //parameters
    //mostly just polling intervals and seeds for various things
    [SerializeField]
    static int humanCheckInterval = 180;
    int humanCheckSeed;
    [SerializeField]
    static float humanCheckDistance = 7.0f;
    [SerializeField]
    static int greetInterval = 120;
    int greetSeed;
    [SerializeField]
    static int zombieCheckInterval = 120;
    int zombieCheckSeed;
    [SerializeField]
    static int targetPickInterval = 20;
    int targetPickSeed;
    [SerializeField]
    static float zombieCheckDistance = 10.0f;
    [SerializeField]
    static int resourceNodeCheckInterval = 480;
    int[] resourceNodeCheckSeed = new int[GOTracker.resourceNodeTypeCount];
    [SerializeField]
    static float resourceNodeCheckDistance = 50.0f;
    [SerializeField]
    static int campCheckInterval = 480;
    int campCheckSeed;
    [SerializeField]
    static float campCheckDistance = 100.0f;
    [SerializeField]
    static int decisionMakingInterval = 83;
    int decisionMakingSeed;
    [SerializeField]
    static float resourceThreshhold = 2.5f;
    [SerializeField]
    static float baseCarryingCapacity = 3;
    float carryingCapacity;
    [SerializeField]
    static int actionInterval = 37;
    int actionSeed;
    static int locationPruneInterval = 60;
    int locationPruneSeed;
    [SerializeField]
    static int campDelay = 200;
    [SerializeField]
    static float baseResourceDecayPerDay = 10;
    static int drainInterval = 600;
    int drainSeed;
    static float defaultTargetTolerance = 1;
    static float maximumCampDensity = 50;
    static int reportInterval = 60;
    int reportSeed;
    static int moveInterval = 5;
    int moveSeed;
    static int greetsPerTurn = 5;
    static int attackCD = 2;
    int lastAttack = 0;
    float attackRange = 1f;
    //the amount of skill required to evade tags 50% of the time
    static float skillFor50Percent = 50f;
    static float skillPerEvade = .5f;
    static float skillPerKill = 2f;
    static float statTransferPercentage = .05f;
    static float staminaPerSecond = .5f;
    static int campingFrames = 3;
    #endregion

    #region members
    //other components
    Mover myMover; //on the human
    GOTracker myGOT; //not on the human
    MapInfo mi;
    Stats myStats;

    //nearby actors
    List<MonoBehaviour> nearbyZombies = new List<MonoBehaviour>();
    Zombie targetZombie = null; //also current combat target
    List<MonoBehaviour> nearbyHumans = new List<MonoBehaviour>();
    List<MonoBehaviour> nearbyCamps = new List<MonoBehaviour>();
    List<MonoBehaviour>[] nearbyResourceNodesByType = new List<MonoBehaviour>[GOTracker.resourceNodeTypeCount];
    Vector2?[] nearestNodesByType = new Vector2?[GOTracker.resourceNodeTypeCount];
    Vector2? nearestCamp = null;

    //resource node locations and harvest ranges, by type
    HashSet<Vector2>[] resourceLocationsByType = new HashSet<Vector2>[GOTracker.resourceNodeTypeCount];
    static float[] harvestRangesByResourceType = new float[GOTracker.resourceNodeTypeCount]; //lol@static

    //a dead location is one where there should be a resource/camp, but isn't
    HashSet<Vector2> deadLocations = new HashSet<Vector2>(); //used to remove entries from the resource/camp location hash sets, values are moved to archive after use
    HashSet<Vector2> deadLocationsArchive = new HashSet<Vector2>(); //gets passed around with greetings

    //known locations of camps
    HashSet<Vector2> campLocations = new HashSet<Vector2>();

    //inventory for resources. one slot for every resource type
    float[] inventory = new float[Enum.GetValues(typeof(ResourceType)).Length];

    //the position to move towards. probably doesn't need to be nullable anymore
    Vector2? targetPos = null;
    float targetPosTolerance = defaultTargetTolerance; //if we're closer than this to the target, we're at the target

    //resource decay per second
    float resourceDecay;

    //are we panicking?
    bool panic = false;

    //are we trying to place a camp?
    bool camping = false;

    //wander direction
    Vector2? wanderDirection = null;

    float[] drainAmountsByType;
    #endregion

    #region properties
    //indicates the maximum distance this human can travel before dying
    float WalkableDistance
    {
        get
        {
            float foodLimit = lowestProcessedResource;
            float secondsFoodLasts = foodLimit / resourceDecay;
            float optimalDistance = secondsFoodLasts * myMover.MaxMoveSpeed;
            return optimalDistance * .8f;
        }
    }

    //returns an array parallel to the node location set array indicating whether the human is low on each type of processed resource
    bool[] NeededResources
    {
        get
        {
            bool[] needed = new bool[GOTracker.resourceNodeTypeCount];
            for (int c = 0; c < needed.Length; c++)
                if (inventory[c + (int)ResourceType.FoodProcessed] < resourceThreshhold)
                    needed[c] = true;
            return needed;
        }
    }

    //returns an array parallel to the node location set array indicating whether the human can carry more of each type of raw resource
    bool[] CanCarryMoreResources
    {
        get
        {
            bool[] canCarry = new bool[GOTracker.resourceNodeTypeCount];
            for (int c = 0; c < canCarry.Length; c++)
                if (inventory[c] < carryingCapacity)
                    canCarry[c] = true;
            return canCarry;
        }
    }

    //returns the amount of the processed resource the human ahs the least of
    float lowestProcessedResource
    {
        get
        {
            float lowest = float.MaxValue;
            for (int c = (int)ResourceType.FoodProcessed; c < inventory.Length; c++)
                if (inventory[c] < lowest)
                    lowest = inventory[c];
            return lowest;
        }
    }

    Vector2? ClosestCampLong
    {
        get
        {
            //null means we know no camp locations
            Vector2? closest = null;
            foreach (Vector2 campPos in campLocations)
            {
                //don't check distances if this is the first actual vector we've checked
                if (closest == null)
                    closest = campPos;
                //otherwise, set closest to the new vector if the new vector is closer than closest
                else if ((campPos - (Vector2)transform.position).sqrMagnitude < (closest.Value - (Vector2)transform.position).sqrMagnitude)
                    closest = campPos;
            }
            return closest;
        }
    }

    float InverseEvadeChance
    {
        get
        {
            float currentSkill = myStats.GetStat(StatTypes.Skill);
            return skillFor50Percent / (currentSkill + skillFor50Percent);
        }
    }
    #endregion

    //making public fields to place the zombie sprite and animator into
    public Sprite ZombieSprite;
    public RuntimeAnimatorController ZombieAnimator;

    // Use this for initialization
    void Start () {
        //grab scripts
        myMover = (Mover)GetComponent(typeof(Mover));
        GameObject map = GameObject.Find("Map");
        myGOT = (GOTracker)map.GetComponent(typeof(GOTracker));
        myStats = (Stats)gameObject.GetComponent(typeof(Stats));
        myGOT.ReportCreation(ObjectType.Human);
        mi = (MapInfo)map.GetComponent(typeof(MapInfo));

        //subscribe to day events
        MapInfo.DayEndEvent += OnDayEnd;

        //stat-dependent fields
        carryingCapacity = baseCarryingCapacity;
        resourceDecay = baseResourceDecayPerDay / MapInfo.DayLengthInSeconds;

        //initialize inventory and node location array
        for (int c = 0; c < inventory.Length; c++)
            inventory[c] = carryingCapacity;
        for (int c = 0; c < resourceLocationsByType.Length; c++)
        {
            resourceLocationsByType[c] = new HashSet<Vector2>();
            nearbyResourceNodesByType[c] = new List<MonoBehaviour>();
        }

        drainAmountsByType = new float[resourceLocationsByType.Length];

        //generate polling seeds
        humanCheckSeed = (int)Math.Round(UnityEngine.Random.value * (humanCheckInterval-1));
        greetSeed = (int)Math.Round(UnityEngine.Random.value * (greetInterval-1));
        zombieCheckSeed = (int)Math.Round(UnityEngine.Random.value * (zombieCheckInterval - 1));
        targetPickSeed = (int)Math.Round(UnityEngine.Random.value * (targetPickInterval-1));
        for (int c = 0; c < resourceNodeCheckSeed.Length; c++) //this array holds a seed for each raw resource type
            resourceNodeCheckSeed[c] = (int)Math.Round(UnityEngine.Random.value * (resourceNodeCheckInterval-1));
        campCheckSeed = (int)Math.Round(UnityEngine.Random.value * (campCheckInterval-1));
        decisionMakingSeed = (int)Math.Round(UnityEngine.Random.value * (decisionMakingInterval-1));
        actionSeed = (int)Math.Round(UnityEngine.Random.value * (actionInterval-1));
        reportSeed = (int)Math.Round(UnityEngine.Random.value * (reportInterval-1));
        moveSeed = (int)Math.Round(UnityEngine.Random.value * (moveInterval-1));
        locationPruneSeed = (int)Math.Round(UnityEngine.Random.value * (locationPruneInterval - 1));
        drainSeed = (int)Math.Round(UnityEngine.Random.value * (drainInterval - 1));
    }
	
	// Update is called once per frame
	void Update () {
        //starvation check and upkeep - unit is killed if it has less than 0 of any processed resource, loses decay per second times delta time per frame
        #region starvation/upkeep
        int startResource = (int)ResourceType.FoodProcessed;
        for (int c = startResource; c < startResource + GOTracker.resourceNodeTypeCount; c++)
        {
            if (inventory[c] < 0)
            {
                Destroy(gameObject);
            }
            drainAmountsByType[c-startResource] += resourceDecay * Time.deltaTime;
        }
        if(Time.frameCount % drainInterval == drainSeed)
        {
            for (int c = 0; c < GOTracker.resourceNodeTypeCount; c++)
            {
                inventory[c + startResource] -= drainAmountsByType[c];
                drainAmountsByType[c] = 0;
            }
        }
        #endregion

        //stat increases
        #region stats
        //stamina - only while moving, twice as fast in combat
        if (!(myMover.Velocity == Vector2.zero))
            myStats.ChangeStat(StatTypes.Stamina, (panic) ? 2 * staminaPerSecond * Time.deltaTime : staminaPerSecond * Time.deltaTime);
        #endregion


        //attack
        #region attack
        if (targetZombie != null)
        {
            panic = true;
            //set targetpos to a combination of away from zombie and towards nearest/strongest camp
            targetPos = 2 * (Vector2)transform.position - (Vector2)targetZombie.transform.position;

            //tag the zombie
            if (Time.frameCount - lastAttack > attackCD && ((Vector2)targetZombie.transform.position - (Vector2)transform.position).sqrMagnitude < attackRange * attackRange)
            {
                lastAttack = Time.frameCount;
                bool success = false;
                targetZombie.Tag(InverseEvadeChance, out success);
                if (success)
                    myStats.ChangeStat(StatTypes.Skill, skillPerKill);
            }
        }
        #endregion

        //zombie check
        #region zombieCheck/targetPick
        if (Time.frameCount % zombieCheckInterval == zombieCheckSeed)
            nearbyZombies = myGOT.GetObjsInRange(transform.position, zombieCheckDistance, ObjectType.Zombie);

        //target pick
        if (Time.frameCount % targetPickInterval == targetPickSeed)
            PickTarget();
        #endregion

        //resource node checks
        #region node check
        int resourceNodeCheckMod = Time.frameCount % resourceNodeCheckInterval; //save the mod
        for (int c = 0; c < resourceNodeCheckSeed.Length; c++)//if the mod matches one of the seeds
            if (!panic && (resourceNodeCheckMod == resourceNodeCheckSeed[c]))
                NodeCheck(c);
        #endregion

        //camp check
        #region camp check
        if (!panic && Time.frameCount%campCheckInterval==campCheckSeed)
        {
            CampCheck();
        }
        #endregion

        //human check
        #region human check / greeting
        if (!panic && Time.frameCount % humanCheckInterval == humanCheckSeed)
            nearbyHumans = myGOT.GetObjsInRange(transform.position, humanCheckDistance, ObjectType.Human);

        //greeting
        if (Time.frameCount % greetInterval == greetSeed)
            StartGreeting();
        #endregion

        //actions
        #region actions
        if (!panic && Time.frameCount % actionInterval == actionSeed)
            Actions();
        #endregion

        //location pruning
        

        //decision making
        #region decision making
        if (!panic && (targetPos == null || Time.frameCount % decisionMakingInterval == decisionMakingSeed))
        {
            //remove dead locations from resource node and camp sets
            foreach (Vector2 deadLocation in deadLocations)
            {
                for (int c = 0; c < resourceLocationsByType.Length; c++)
                {
                    resourceLocationsByType[c].Remove(deadLocation);
                    if (nearestNodesByType[c].Value == deadLocation)
                        nearestNodesByType[c] = null;
                }
                campLocations.Remove(deadLocation);
                if (nearestCamp.Value == deadLocation)
                    nearestCamp = null;
                deadLocationsArchive.Add(deadLocation); //make sure to add each dead location to the archive
            }

            //what resources do I need?
            bool[] needed = NeededResources;
            bool[] canCarryMore = CanCarryMoreResources;

            //get the closest camp
            Vector2? closestCamp = (nearestCamp.HasValue) ? nearestCamp : ClosestCampLong;

            //get the closest of each resource node
            Vector2?[] closestNodes = nearestNodesByType; //null means we don't have any nodes of that type
            bool allNodeTypesKnown = true;
            for (int c = 0; c < needed.Length; c++)
                if (closestNodes[c] == null) //double check with a long search if null
                {
                    closestNodes[c] = ScanForNearestNodeOfType(c);
                    if (closestNodes[c] == null)
                        allNodeTypesKnown = false;
                }
            //if we don't have one of each node type
            if (!allNodeTypesKnown)
            {
                //wander until we do
                Wander();
            }
            //if we have all the node types but no camp
            else if (nearestCamp == null)
            {
                //convert to non-nullable type
                Vector2[] closestNodesNotNullable = new Vector2[closestNodes.Length];
                for (int c = 0; c < closestNodes.Length; c++)
                    closestNodesNotNullable[c] = closestNodes[c].Value;

                //get circumcenter of closest known nodes
                Vector2 campPos = MapInfo.GetCentroid(closestNodesNotNullable);
                //if we're there
                if (!camping && (campPos - (Vector2)transform.position).sqrMagnitude < targetPosTolerance * targetPosTolerance)
                {
                    StartCoroutine(PlaceCamp(campingFrames));
                }
                //if we're not, go there
                else
                    targetPos = campPos;
            }
            //if I need any...
            else if (Array.IndexOf(needed, true) != -1)
            {
                //closest node of a type we need
                Vector2? closestNode = null;
                ResourceType? closestNodeType = null;

                //search closestNodes for the closest of a type we need
                for (int c = 0; c < needed.Length; c++)
                {
                    //if we don't need this type or can't carry any more of it, skip it
                    if (needed[c] == false || canCarryMore[c] == false) continue;
                    //get the nearest node of the current type - null means we don't know any node locations
                    Vector2? closestOfType = closestNodes[c];

                    //don't check distances if this is the first actual vector we've checked
                    if (closestNode == null)
                    {
                        closestNode = closestOfType;
                        closestNodeType = (ResourceType)c;
                    }
                    //if we already have a vector, replace it with the new one if the new one is closer
                    else if (closestOfType != null && (closestOfType.Value - (Vector2)transform.position).sqrMagnitude < (closestNode.Value - (Vector2)transform.position).sqrMagnitude)
                    {
                        closestNode = closestOfType;
                        closestNodeType = (ResourceType)c;
                    }
                }

                //get walkable distance
                float walkableDistance = WalkableDistance;

                //closest camp/node is walkable if it exists and is within walkable distance
                bool closestCampIsWalkable = (closestCamp != null && (closestCamp.Value - (Vector2)transform.position).sqrMagnitude < walkableDistance * walkableDistance);
                bool closestNodeIsWalkable = (closestNode != null && (closestNode.Value - (Vector2)transform.position).sqrMagnitude < walkableDistance * walkableDistance);

                //if only one is walkable (that's an XOR)
                if (closestCampIsWalkable ^ closestNodeIsWalkable)
                {
                    //walk to that one
                    targetPos = (closestCampIsWalkable) ? closestCamp : closestNode;
                    targetPosTolerance = (closestCampIsWalkable) ? Camp.minInteractRadius * .9f : harvestRangesByResourceType[(int)closestNodeType] * .9f;
                    Debug.DrawRay(transform.position, targetPos.Value - (Vector2)transform.position, Color.red, .1f);
                }
                //if neither are walkable
                else if (!(closestCampIsWalkable || closestNodeIsWalkable))
                {
                    //if nothing is walkable, wander
                    Wander();
                }
                //if both are walkable...
                else
                {
                    //whether the distance from here to the node to the camp is walkable
                    bool canDoBoth = (closestNode.Value - (Vector2)transform.position).sqrMagnitude + (closestCamp - closestNode).Value.sqrMagnitude < walkableDistance * walkableDistance;

                    //go to the node if you can do both
                    if (canDoBoth)
                    {
                        targetPos = closestNode;
                        targetPosTolerance = harvestRangesByResourceType[(int)closestNodeType.Value]*.9f;
                        Debug.DrawRay(transform.position, targetPos.Value - (Vector2)transform.position, Color.magenta, .1f);
                    }
                    //otherwise go to the camp
                    else
                    {
                        targetPos = closestCamp;
                        targetPosTolerance = Camp.minInteractRadius*.9f;
                        Debug.DrawRay(transform.position, targetPos.Value - (Vector2)transform.position, Color.green, .1f);
                    }
                }
            }
            //if I don't need any, but can carry more...
            else if (Array.IndexOf(canCarryMore, true) != -1)
            {
                //closest node of a type we need
                Vector2? closestNode = null;
                ResourceType? closestNodeType = null;

                //search closestNodes for the closest of a type we can carry more of
                for (int c = 0; c < canCarryMore.Length; c++)
                {
                    //if can't carry any more of it, skip it
                    if (canCarryMore[c] == false) continue;
                    //get the nearest node of the current type - null means we don't know any node locations
                    Vector2? closestOfType = closestNodes[c];

                    //don't check distances if this is the first actual vector we've checked
                    if (closestNode == null)
                    {
                        closestNode = closestOfType;
                        closestNodeType = (ResourceType)c;
                    }
                    //if we already have a vector, replace it with the new one if the new one is closer
                    else if (closestOfType != null && (closestOfType.Value - (Vector2)transform.position).sqrMagnitude < (closestNode.Value - (Vector2)transform.position).sqrMagnitude)
                    {
                        closestNode = closestOfType;
                        closestNodeType = (ResourceType)c;
                    }
                }
                if (closestNode != null)
                {
                    targetPos = closestNode;
                    targetPosTolerance = harvestRangesByResourceType[(int)closestNodeType.Value]*.9f;
                    Debug.DrawRay(transform.position, targetPos.Value - (Vector2)transform.position, Color.green, .1f);
                }
            }
            //if I don't need any and can't carry more...
            else
            {
                //go to nearest camp
                targetPos = closestCamp;
                targetPosTolerance = Camp.minInteractRadius*.9f;
                Debug.DrawRay(transform.position, targetPos.Value-(Vector2)transform.position, Color.blue, .1f);
            }

        }
        #endregion

        //movement
        #region movement        
        if (Time.frameCount % moveInterval == moveSeed && targetPos != null)
        {
            myMover.SetVelocity(new Vector2(0, 0), 0);
            if ((targetPos - (Vector2)transform.position).Value.sqrMagnitude < targetPosTolerance * targetPosTolerance)
            {
                targetPos = null;
                targetPosTolerance = defaultTargetTolerance;

                //target reached, do stuff
                for (int c = 0; c < resourceNodeCheckSeed.Length; c++)
                    NodeCheck(c);
                CampCheck();
                Actions();
            }
            else
                myMover.SetVelocity((Vector2)(targetPos - (Vector2)gameObject.transform.position), 1.0f);
        }
        #endregion

        panic = false;

        //reporting
        if (Time.frameCount % reportInterval == reportSeed)
            myGOT.Report(this, ObjectType.Human);
	}

    void CampCheck()
    {
        nearbyCamps = myGOT.GetObjsInRange(transform.position, campCheckDistance, ObjectType.Camp);
        foreach (MonoBehaviour mb in nearbyCamps)
        {
            campLocations.Add(mb.gameObject.transform.position);
            float distanceToCampSqr = ((Vector2)mb.gameObject.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (nearestCamp == null || distanceToCampSqr < (nearestCamp - (Vector2)transform.position).Value.sqrMagnitude)
                nearestCamp = mb.transform.position;
        }
    }

    void Wander()
    {
        if (wanderDirection == null || wanderDirection == Vector2.zero)
            wanderDirection = UnityEngine.Random.insideUnitCircle.normalized;
        targetPos = (Vector2)transform.position + wanderDirection * 50;

        if(!mi.IsWorldPosOnMap(targetPos.Value))
        {
            wanderDirection = null;
            Wander();
        }
    }

    void NodeCheck(int c)
    {
        //add any in-range nodes of that resource to our hash set
        nearbyResourceNodesByType[c] = myGOT.GetObjsInRange(transform.position, resourceNodeCheckDistance, c);
        foreach (ResourceNode mb in nearbyResourceNodesByType[c])
        {
            resourceLocationsByType[c].Add(mb.gameObject.transform.position);
            harvestRangesByResourceType[c] = mb.GetHarvestRange(transform.position);
            if (nearestNodesByType[c] == null || ((Vector2)mb.transform.position - (Vector2)transform.position).sqrMagnitude < (nearestNodesByType[c] - (Vector2)transform.position).Value.sqrMagnitude)
                nearestNodesByType[c] = mb.transform.position;
        }
        //check nearest node against nearby nodes to prune dead nodes
        //if we have a node of this type, and it's within half our check distance
        if (nearestNodesByType[c] != null && (nearestNodesByType[c].Value - (Vector2)transform.position).sqrMagnitude < (resourceNodeCheckDistance / 2) * (resourceNodeCheckDistance / 2))
        {
            //try to find it in our nearest node collection
            bool isPresent = false;
            foreach (MonoBehaviour node in nearbyResourceNodesByType[c])
                if (node != null && (Vector2)node.transform.position == nearestNodesByType[c].Value)
                    isPresent = true;
            //if we can't, mark it as dead
            if (!isPresent)
                deadLocations.Add(nearestNodesByType[c].Value);
        }
    }

    IEnumerator PlaceCamp(int waitFrames)
    {
        //mark that we're placing a camp
        camping = true;
        //on each wait frame
        for (int c = 0; c < waitFrames; c++)
        {
            //check for camps
            CampCheck();
            //if one is found, stop camping and break
            if (nearestCamp != null)
            {
                camping = false;
                yield break;
            }
            //otherwise wait another rame
            else
                yield return null;
        }
        //after all wait frames, check again
        CampCheck();
        //if a camp still isn't found
        if (nearestCamp == null)
        {
            //place one, add it to camp locations, and mark it as our new closest camp
            Instantiate(Resources.Load("Camp"), transform.position, Quaternion.identity);
            campLocations.Add(transform.position);
            nearestCamp = transform.position;
        }
        camping = false;
    }

    void Actions()
    {
        //deposit into and request from any camps in range
        foreach (Camp cmp in nearbyCamps)
        {
            //since we're iterating anyway, let's check for the closest camp
            float distanceToCampSqr = ((Vector2)cmp.gameObject.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (nearestCamp == null || distanceToCampSqr < (nearestCamp - (Vector2)transform.position).Value.sqrMagnitude)
                nearestCamp = cmp.transform.position;

            if (distanceToCampSqr < cmp.InteractionRadius * cmp.InteractionRadius)
            {
                for (int c = 0; c <= (int)ResourceType.FuelRaw; c++)
                    cmp.DepositRawResource(inventory, (ResourceType)c);
                //starting at food, since I don't know how ammo works yet
                for (int c = (int)ResourceType.FoodProcessed; c < inventory.Length; c++)
                    inventory[c] += cmp.RequestToUseRawResource((ResourceType)(c - (int)ResourceType.FoodProcessed), carryingCapacity - inventory[c]);
            }
        }
        //gather from all resources that are within their harvest range
        for (int c = 0; c < resourceNodeCheckSeed.Length; c++)
            foreach (ResourceNode rn in nearbyResourceNodesByType[c])
            {
                //humans gather more often than they check for resources, so they'll try to harvest for a second after the resource expires
                //we don't want exceptions, so check for null first
                if (rn == null) continue;

                //since we're iterating anyway, check if this node is closer than our closest for its type
                float distanceToNodeSqr = ((Vector2)rn.gameObject.transform.position - (Vector2)transform.position).sqrMagnitude;
                ResourceType nodeType = rn.ResourceType;
                if (nearestNodesByType[(int)nodeType] == null || distanceToNodeSqr < (nearestNodesByType[(int)nodeType] - (Vector2)transform.position).Value.sqrMagnitude)
                    nearestNodesByType[(int)nodeType] = rn.transform.position; //make it the new closest if it is

                //range check
                float rnHarvestRange = rn.GetHarvestRange(transform.position); //this will be somewhat expensive for water, so we don't want to call it twice
                if (distanceToNodeSqr < rnHarvestRange * rnHarvestRange)
                {
                    float requestAmt = carryingCapacity - inventory[(int)rn.ResourceType]; //request enough to get you to your carrying capacity
                    float gatherAmt = rn.Harvest(requestAmt); //save the actual amount harvested
                    inventory[(int)rn.ResourceType] += gatherAmt; //add amount harvested to inventory
                                                                  //less harvested thhan requested means the node ran dry
                    if (gatherAmt < requestAmt) //add it to dead locations
                        deadLocations.Add(rn.gameObject.transform.position);
                }
            }
    }

    public void Tag(float atkValue)
    {
        if(atkValue<0 || atkValue>InverseEvadeChance)
        {
            myStats.ChangeStat(StatTypes.Skill, skillPerEvade);
            return;
        }

        //change sprite and animator
        SpriteRenderer sr = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        sr.sprite = ZombieSprite;
        Animator animator = (Animator)gameObject.GetComponent(typeof(Animator));
        animator.runtimeAnimatorController = ZombieAnimator;


        //report death to GO tracker
        //myGOT.ReportDeath(this, (int)ObjectType.Human);
        //add zombie script
        gameObject.AddComponent(typeof(Zombie));
        //destroy this script
        Destroy(this);
    }

    void PickTarget()
    {
        MonoBehaviour current = targetZombie;
        targetZombie = null;
        foreach (MonoBehaviour z in nearbyZombies)
            if (z != null && (current == null || ((Vector2)z.transform.position - (Vector2)transform.position).sqrMagnitude < ((Vector2)current.transform.position - (Vector2)transform.position).sqrMagnitude))
                targetZombie = (Zombie)z;
    }

    void StartGreeting()
    {
        for (int c = 0; c < nearbyHumans.Count && c<greetsPerTurn; c++)
        {
            MonoBehaviour mb = nearbyHumans[c];
            if (mb != null)
                StartCoroutine(Greet((Human)mb));
        }
    }
    IEnumerator Greet(Human other, bool first = true)
    {
        //get new camp locations
        foreach (Vector2 cLoc in other.campLocations)
        {
            campLocations.Add(cLoc);
            if (nearestCamp.HasValue && (nearestCamp.Value - (Vector2)transform.position).sqrMagnitude > (cLoc - (Vector2)transform.position).sqrMagnitude)
                nearestCamp = cLoc;
        }
        yield return null;

        //get new node locations for each resource
        for (int c = 0; c < resourceLocationsByType.Length; c++)
        {
            foreach (Vector2 rLoc in other.resourceLocationsByType[c])
            {
                resourceLocationsByType[c].Add(rLoc);
                if (nearestNodesByType[c].HasValue && (nearestNodesByType[c].Value - (Vector2)transform.position).sqrMagnitude > (rLoc - (Vector2)transform.position).sqrMagnitude)
                    nearestNodesByType[c] = rLoc;
            }
        }
        yield return null;

        //any dead locations the other has that this one doesn't get added to the dead locations set
        foreach (Vector2 dLoc in other.deadLocationsArchive)
            deadLocations.Add(dLoc);
        yield return null;

        //stat transfers
        StatTransfer(StatTypes.Skill, other);
        StatTransfer(StatTypes.Knowledge, other);
        StatTransfer(StatTypes.Coordination, other);

        if (first)
            other.Greet(this, false);
    }

    void StatTransfer(StatTypes stat, Human other)
    {
        //if other's skill is greater than ours, add the difference multiplied by the transfer percentage to ours
        float myStat = myStats.GetStat(stat);
        float otherStat = other.myStats.GetStat(stat);
        if (myStat < otherStat)
            myStats.ChangeStat(stat, (otherStat - myStat) * statTransferPercentage);
    }

    void OnDayEnd(object sender, DayEndEventArgs dea)
    {
        //we're starting at the processed resource section of the inventory
        int startResource = (int)ResourceType.FoodProcessed;
        //loop through and subtract 1 from every processed resource
        for (int c = 0; c <= GOTracker.resourceNodeTypeCount; c++)
            inventory[c + startResource]--;
    }

    Vector2? ScanForNearestNodeOfType(int typeInt)
    {
       // int typeInt = (int)typeInt;
        HashSet<Vector2> locationsOfThisType = resourceLocationsByType[typeInt];
        Vector2? closest = null;
        foreach(Vector2 nodePos in locationsOfThisType)
        {
            if (closest == null)
                closest = nodePos;
            else
                if (((Vector2)transform.position - nodePos).sqrMagnitude < (closest.Value - (Vector2)transform.position).sqrMagnitude)
                closest = nodePos;
        }
        return closest;
    }

    void OnDestroy()
    {
        myGOT.ReportDeath(this, ObjectType.Human);
    }

}

