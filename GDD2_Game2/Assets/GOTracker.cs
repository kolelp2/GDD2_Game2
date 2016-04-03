using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class GOTracker : MonoBehaviour {
    Dictionary<Vector2,List<MonoBehaviour>>[] objDictionary;
    Dictionary<MonoBehaviour, Vector2> unitWorldPositions = new Dictionary<MonoBehaviour, Vector2>();
    int[] unitCountsByType = new int[Enum.GetValues(typeof(ObjectType)).Length];
    Mesh[] densityMapsByObjectType = new Mesh[Enum.GetValues(typeof(ObjectType)).Length];
    GameObject[] densityMapObjects = new GameObject[Enum.GetValues(typeof(ObjectType)).Length];
    Color[] densityMapColors = new Color[Enum.GetValues(typeof(ObjectType)).Length];
    Mesh fowMesh;
    GameObject fowMeshObject;
    public readonly static int resourceNodeTypeCount = 3;

    MapInfo mi;

    [SerializeField]
    int mapPrecision = 1;
    [SerializeField]
    int startingNodesOfEachType = 50;
    [SerializeField]
    int startingHumans = 2000;
    [SerializeField]
    int startingZombies = 10;
    [SerializeField]
    int densityMapPrecision = 20;
    [SerializeField]
    int fowPrecision = 5;
    [SerializeField]
    float fowClearRadius = 60;
    [SerializeField]
    int fowVerticesPerFrame = 50;
    [SerializeField]
    int densityMapVerticesPerFrame = 50;

    bool updatingDensityMaps = false;
    bool updatingFOW = false;

    void Awake()
    {
        objDictionary = new Dictionary<Vector2, List<MonoBehaviour>>[Enum.GetValues(typeof(ObjectType)).Length];
        for (int c = 0; c < objDictionary.Length; c++)
            objDictionary[c] = new Dictionary<Vector2, List<MonoBehaviour>>();
        mi = (MapInfo)gameObject.GetComponent(typeof(MapInfo));
    }

	// Use this for initialization
	void Start () {
        StartCoroutine(GetDensityMaps());
        StartCoroutine(GetFogOfWarMesh());
	}
	
	// Update is called once per frame
	void Update () {
        //win
        if (Time.frameCount>1000 && unitCountsByType[(int)ObjectType.Human] <= 0)
            Application.LoadLevel("EndScreen_Win");
        //lose
        else if (Time.frameCount > 1000 && unitCountsByType[(int)ObjectType.Zombie] <= 0)
            Application.LoadLevel("EndScreen_Lose");

        if (Input.GetKeyDown(KeyCode.E))
            ToggleDensityMapVisibility();

        if (!updatingDensityMaps)
            StartCoroutine(UpdateDensityMaps());
        if (!updatingFOW)
            StartCoroutine(UpdateFOWMesh());
	}
    void ToggleDensityMapVisibility()
    {
        foreach (GameObject mo in densityMapObjects)
            mo.SetActive(!mo.activeInHierarchy);
    }

    IEnumerator GetDensityMaps()
    {
        yield return null;

        for(int c = 0;c<densityMapsByObjectType.Length;c++)
        {
            densityMapObjects[c] = mi.GetBlankMeshObject(densityMapPrecision);
            densityMapsByObjectType[c] = ((MeshFilter)(densityMapObjects[c].GetComponent(typeof(MeshFilter)))).mesh;
        }

        for (int n = 0; n < densityMapColors.Length; n++)
        {
            densityMapColors[n] = Color.HSVToRGB((float)n * (1.0f / (float)densityMapColors.Length), 1, 1);
        }
    }

    IEnumerator GetFogOfWarMesh()
    {
        yield return null;
        fowMeshObject = mi.GetBlankMeshObject(fowPrecision, -2);
        fowMesh = ((MeshFilter)(fowMeshObject.GetComponent(typeof(MeshFilter)))).mesh;
        Color[] colors = new Color[fowMesh.vertexCount];
        for (int c = 0; c < colors.Length; c++)
            colors[c] = new Color(0, 0, 0, 1);
        fowMesh.colors = colors;
    }

    IEnumerator UpdateFOWMesh()
    {
        updatingFOW = true;
        yield return null;
        Vector3[] vertices = fowMesh.vertices;
        Color[] newColors = new Color[vertices.Length];
        int verticesThisFrame = 0;
        for (int c = 0; c < newColors.Length; c++)
        {
            if (GetObjsInRange(mi.GridToWorldSpace(vertices[c],1), 60, ObjectType.Zombie).Count > 0)
                newColors[c] = new Color(0, 0, 0, 0);
            else
                newColors[c] = new Color(0, 0, 0, 1);
            verticesThisFrame++;
            if (verticesThisFrame > fowVerticesPerFrame)
            {
                yield return null;
                verticesThisFrame = 0;
            }
        }
        fowMesh.colors = newColors;
        updatingFOW = false;
    }

    IEnumerator UpdateDensityMaps()
    {
        yield return null;
        updatingDensityMaps = true;
        float vertexProximityRadius = 20;
        float maxAlphaPercentage = .05f;
        int verticesThisFrame = 0;
        for(int type = 0; type < densityMapsByObjectType.Length; type++)
        {
            if (type != (int)ObjectType.Human && type != (int)ObjectType.Zombie)
                continue;

            Vector3[] vertices = densityMapsByObjectType[type].vertices;
            Color[] mapColors = new Color[vertices.Length];
            Color mapColor = densityMapColors[type];

            for(int n = 0;n<vertices.Length;n++)
            {
                int unitProximityCount = GetObjsInRange(mi.GridToWorldSpace(vertices[n], 1), vertexProximityRadius, type).Count;
                float scaledProximityPercentage = (unitCountsByType[type] != 0) ? ((float)unitProximityCount / (float)unitCountsByType[type]) / maxAlphaPercentage : 0;
                mapColors[n] = new Color(mapColor.r, mapColor.g, mapColor.b, (scaledProximityPercentage < 1) ? scaledProximityPercentage/2.0f : .5f);
                verticesThisFrame++;
                if (verticesThisFrame >= densityMapVerticesPerFrame)
                {
                    yield return null;
                    verticesThisFrame = 0;
                }
            }
            densityMapsByObjectType[type].colors = mapColors;
        }
        updatingDensityMaps = false;
    }

    public void GenerateObjs(List<Vector2> waterLocations)
    {
        //generate nodes (not water)
        for (int c = 0; c < startingNodesOfEachType; c++)
        {
            //food
            Instantiate(Resources.Load("Food"), mi.GetRandomSeaLevelMapPosAsWorldPos(), Quaternion.identity);
            //fuel
            Instantiate(Resources.Load("Fuel"), mi.GetRandomSeaLevelMapPosAsWorldPos(), Quaternion.identity);
        }
        //generate water nodes
        foreach(Vector2 loc in waterLocations)
            Instantiate(Resources.Load("Water"), loc, Quaternion.identity);

        //generate humans
        for (int c = 0; c < startingHumans; c++)
        {
            float spawnRadius = 50;
            Vector2 location = waterLocations[(int)Math.Floor(UnityEngine.Random.value * waterLocations.Count)];
            Instantiate(Resources.Load("Human"), location + UnityEngine.Random.insideUnitCircle * spawnRadius, Quaternion.identity);
        }

        //generate zombies
        Vector2 zombieStart = mi.GetRandomSeaLevelMapPosAsWorldPos();
        Camera.main.transform.position = new Vector3(zombieStart.x, zombieStart.y, Camera.main.transform.position.z);
        for (int c = 0; c < startingZombies; c++)
        {
            Instantiate(Resources.Load("Zombie"), zombieStart + UnityEngine.Random.insideUnitCircle * 20, Quaternion.identity);
        }
    }

    public List<MonoBehaviour> GetObjsInRange(Vector2 pos, float radius, ObjectType objType)
    {
        return GetObjsInRange(pos, radius, (int)objType);
    }

    public List<MonoBehaviour> GetObjsInRange(Vector2 position, float radius, int objType)
    {
        if (objType>objDictionary.Length) return new List<MonoBehaviour>();

        var objsOfType = objDictionary[objType];

        Vector2 rangeMin = mi.WorldToGridIndex(new Vector2(position.x - radius, position.y - radius), mapPrecision);
        Vector2 rangeMax = mi.WorldToGridIndex(new Vector2(position.x + radius, position.y + radius), mapPrecision);

        List<MonoBehaviour> activeAndInRange = new List<MonoBehaviour>((int)(rangeMax-rangeMin).sqrMagnitude/2);//initial capacity is totally spitballed

        for (int row = (int)rangeMin.x; row <= rangeMax.x; row++)
        {
            for (int col = (int)rangeMin.y; col <= rangeMax.y; col++)
            {
                List<MonoBehaviour> mapSquareList;
                if (!objsOfType.TryGetValue(new Vector2(row, col), out mapSquareList)) continue;
                for(int c = 0;c<mapSquareList.Count;c++)
                {
                    MonoBehaviour mb = mapSquareList[c];
                    //if object is active, and the squared distance from it to the given position is less than or equal to the given radius squared
                    if (mb != null && ((Vector2)mb.transform.position - position).sqrMagnitude <= radius * radius)
                        activeAndInRange.Add(mb);
                }
            }
        }

        return activeAndInRange;
        //return new List<MonoBehaviour>();
    }

    public void Report(MonoBehaviour mb, ObjectType objType)
    {
        Report(mb, (int)objType);
    }

    public void Report(MonoBehaviour mb, int objType)
    {
        //if the obj dictionary doesn't contain this type yet, add it
        if (objType > objDictionary.Length) return;

        Vector2 gridPos = mi.WorldToGridIndex(mb.gameObject.transform.position, mapPrecision);

        //get the set for this map square for this type
        Dictionary<Vector2, List<MonoBehaviour>> typeList = objDictionary[objType];
        if (!typeList.ContainsKey(gridPos)) typeList.Add(gridPos, new List<MonoBehaviour>());
        List<MonoBehaviour> squareList = typeList[gridPos];
        //add the script to it if it isn't already there
        if (!squareList.Contains(mb)) squareList.Add(mb);

        Vector2 unitWorldPos;
        if (unitWorldPositions.TryGetValue(mb, out unitWorldPos))
        {
            Vector2 prevGridIndex = mi.WorldToGridIndex(unitWorldPos, mapPrecision);

            //...and the current map square is different than the previous map square
            if (prevGridIndex != gridPos)
            {
                //remove the unit from the previous map square
                List<MonoBehaviour> prevSquareList = new List<MonoBehaviour>();
                if (typeList.TryGetValue(prevGridIndex, out prevSquareList))
                    prevSquareList.Remove(mb);

                //if the previous map square is empty, trim empty elements
                if (prevSquareList.Count == 0) prevSquareList.TrimExcess();
            }
        }
        unitWorldPositions[mb] = mb.gameObject.transform.position;
    }
    public int GetUnitCount(ObjectType type)
    {
        return unitCountsByType[(int)type];
    }
    public void ReportCreation(ObjectType newThingType)
    {
        unitCountsByType[(int)newThingType]++;
    }

    public void ReportDeath(MonoBehaviour dt, ObjectType dtType)
    {
        ReportDeath(dt, (int)dtType);
    }

    public void ReportDeath(MonoBehaviour deadThing, int deadThingType)
    {
        //report first to make sure we have the latest info
        Report(deadThing, deadThingType);

        //remove the dead thing from the object dictionary, the position dictionary, and the count list
        if (deadThingType<objDictionary.Length)
        {
            Vector2 deadThingGridPos = mi.WorldToGridIndex(deadThing.gameObject.transform.position, mapPrecision);
            //Vector2 deadThingPrevGridPos = mi.WorldToGridIndex(unitWorldPositions[deadThing], mapPrecision);
            var typeList = objDictionary[deadThingType];
            typeList[deadThingGridPos].Remove(deadThing);
        }
        unitWorldPositions.Remove(deadThing);
        //if (unitCountsByType[deadThingType] > 0)
            unitCountsByType[deadThingType]--;
    }

    //public Vector2 World
}

//it's very important that resource node types be first by index
public enum ObjectType { Food = 0, Water = 1, Fuel = 2, Human = 3, Zombie = 4, Camp = 5 }

//the order is important here - raw node types first, processed node types last
//it's also important that the resource types always start with food
public enum ResourceType { FoodRaw = ObjectType.Food, WaterRaw = ObjectType.Water, FuelRaw = ObjectType.Fuel, Ammo = 3, FoodProcessed = 4, WaterProcessed = 5, FuelProcessed = 6 }
