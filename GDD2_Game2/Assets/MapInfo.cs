using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapInfo : MonoBehaviour {
    [SerializeField]
    public readonly static int DayLengthInSeconds = 600;
    int dayNumber = 0;
    [SerializeField]
    int tileDimension = 100;
    [SerializeField]
    float tileDepth = 1;
    [SerializeField]
    int numberOfTiles = 11;
    [SerializeField]
    int blankTiles = 5;
    [SerializeField]
    int altitudeFieldPrecision = 2;
    [SerializeField]
    float waterNodeSpreadScale = 50;
    float[,] altitudeField;
    public static float WorldTimeInDays
    {
        get
        {
            return Time.time / DayLengthInSeconds;
        }
    }

    public static event EventHandler<DayEndEventArgs> DayEndEvent;

    Vector2 mapSize;
    public Vector2 MapSize
    {
        get { return mapSize; }
    }
    Vector2 mapPos;
    public Vector2 MapPos
    {
        get { return mapPos; }
    }
    void Awake()
    {
        BoxCollider2D mapBox = (BoxCollider2D)gameObject.GetComponent(typeof(BoxCollider2D));
        mapSize = mapBox.size;

        mapPos = new Vector2(mapBox.transform.position.x - mapBox.size.x / 2, mapBox.transform.position.y - mapBox.size.y / 2);
    }
    // Use this for initialization
    void Start () {
        //place tiles
        //get number of tiles in both directions
        int tileX = (int)Math.Ceiling(mapSize.x / tileDimension);
        int tileY = (int)Math.Ceiling(mapSize.y / tileDimension);
        Sprite[,] mapSprites = new Sprite[tileX, tileY];
        Quaternion[,] spriteRotations = new Quaternion[tileX, tileY];
        //iterate through the 2d tile grid
        for(int row = 0; row < tileX; row++)
        {
            for(int col = 0; col < tileY; col++)
            {
                //get a random rotation in 90deg intervals
                int rotationDegrees = UnityEngine.Random.Range(0, 4) * 90;
                Quaternion rotation = Quaternion.Euler(0, 0, rotationDegrees);
                spriteRotations[row, col] = rotation;
                //instantate the tile at the current position in the grid
                //this means transforming from grid space to world space, accounting for the fact that the grid is bottom-left anchored and the tile objects are center-anchored, and correcting for the map's offset
                int tileType = UnityEngine.Random.Range(1, numberOfTiles + blankTiles);
                if (tileType > numberOfTiles) //the blank tile may be considered more than once
                    tileType = numberOfTiles;
                GameObject newTile = (GameObject)Instantiate(Resources.Load("MapTile" + tileType), new Vector3(mapPos.x + row * tileDimension + tileDimension / 2, mapPos.y + col * tileDimension + tileDimension / 2, tileDepth), rotation);
                SpriteRenderer sr = (SpriteRenderer)newTile.GetComponent(typeof(SpriteRenderer));
                //save the sprites - we'll use them to generate the altitude field
                mapSprites[row, col] = sr.sprite;
                //scale the tiles so they conform to our tile dimension
                float ppu = sr.sprite.rect.width / sr.sprite.bounds.size.x; //this is pixels per unit
                float scale = tileDimension / (sr.sprite.rect.width / ppu); //desired in unity units over current in unity units - with current in unity units being current pixel width over pixels per unit
                newTile.transform.localScale = new Vector3(scale, scale, 1);
            }
        }

        //generate the altitude field
        Vector2 altFieldSize = mapSize / altitudeFieldPrecision;
        altitudeField = new float[(int)altFieldSize.x, (int)altFieldSize.y];
        for(int row = 0; row < altitudeField.GetLength(0); row++)
        {
            for(int col = 0; col < altitudeField.GetLength(1); col++)
            {
                //convert position in altitude field to world position
                Vector2 fieldPointInWorldSpace = GridToWorldSpace(new Vector2(row, col), altitudeFieldPrecision);
                //convert that world position to a position within the tile grid
                Vector2 inTileGridSpace = WorldToGridSpace(fieldPointInWorldSpace, tileDimension);
                //truncate - e.g. (14.2, 1.5) becomes (14, 1) - this will be used as an index
                Vector2 asGridIndex = new Vector2((float)Math.Truncate(inTileGridSpace.x), (float)Math.Truncate(inTileGridSpace.y));
                //get the "remainder" of the truncation - e.g. (14.2, 1.5) becomes (.2, .5) - this will be used to locate the proper pixel within the tile
                Vector2 asTilePercentage = new Vector2(inTileGridSpace.x - asGridIndex.x, inTileGridSpace.y - asGridIndex.y);
                Vector2 translation = new Vector2(.5f, .5f);
                //translate percentage vector to center of tile, rotate it by the tiles rotation, then translate it back
                //this is to account for the random rotation of the tiles when we do the pixel lookup
                //need to use the inverse of the quat, because reasons
                asTilePercentage = (Vector2)(Quaternion.Inverse(spriteRotations[(int)asGridIndex.x, (int)asGridIndex.y]) * (asTilePercentage - translation)) + translation;

                //get the tile
                Sprite currentTile = mapSprites[(int)asGridIndex.x, (int)asGridIndex.y];
                //get the color - this is the color of the pixel directly underneath the current field point
                Color fieldPointColor = currentTile.texture.GetPixel((int)(asTilePercentage.x * currentTile.rect.width), (int)(asTilePercentage.y * currentTile.rect.height));

                altitudeField[row, col] = GetAltitudeForColor(fieldPointColor);
                Debug.DrawRay(fieldPointInWorldSpace, new Vector2(0, altitudeField[row, col]/2), Color.red, float.MaxValue);
            }
        }

        //assemble water borders
        List<Vector2> waterBorders = new List<Vector2>();
        List<Vector2> nodeLocations = new List<Vector2>();
        //iterate through alt field
        for (int row = 0; row < altitudeField.GetLength(0); row++)
        {
            for(int col = 0; col < altitudeField.GetLength(1); col++)
            {
                //if current point is water
                if (altitudeField[row, col] < 0)
                {
                    bool isBorder = false;
                    //h and v are horizontal and vertical offsets
                    //this will iterate through a 3x3 square centered on the current point (row, col)
                    for (int h = -1; h <= 1 && !isBorder; h++)
                        for (int v = -1; v <= 1 && !isBorder; v++)
                            //if the current point plus offsets is a) not the current point, b) within the bounds of the alt field, and c) not water...
                            if (!(h == 0 && v == 0) && h + row >= 0 && h + row < altitudeField.GetLength(0) && v + col >= 0 && v + col < altitudeField.GetLength(1) && altitudeField[h + row, v + col] >= 0)
                                isBorder = true; //...the current point is a border
                    if (isBorder)
                        //make sure to convert to world coords when adding the point
                        waterBorders.Add(GridToWorldSpace(new Vector2(row, col), altitudeFieldPrecision));
                }
            }
        }
        //iterate through the border points
        foreach (Vector2 point in waterBorders)
        {
            //check each border point against all node locations
            bool isFarEnoughFromOtherNodes = true;
            foreach (Vector2 node in nodeLocations)
            {
                //if the distance between them is further than Water's harvest range...
                float distanceSqr = (node - point).sqrMagnitude;
                float targetDistanceSqr = (Water.harvestRange * Water.harvestRange) * waterNodeSpreadScale;
                if (distanceSqr < targetDistanceSqr)
                {
                    isFarEnoughFromOtherNodes = false;
                    break;
                }
            }
            //... add the current point to node locations
            if (isFarEnoughFromOtherNodes)
                nodeLocations.Add(point);
        }

        //pass the list of node locations to the GOT so it can generate stuff
        ((GOTracker)gameObject.GetComponent(typeof(GOTracker))).GenerateObjs(nodeLocations);
	}
	
	// Update is called once per frame
	void Update () {
        //if the current day (running time divided by day length floored) doesn't equal the recorded current day,
        if ((int)Math.Floor(Time.time / DayLengthInSeconds) != dayNumber)
        {
            //increment day number and emit day end event
            dayNumber++;
            DayEndEvent(this, new DayEndEventArgs());
        }
	}

    //takes a vector representing a point in the world and returns a vector representing the corresponding point in a grid of the given precision within the map
    public Vector2 WorldToGridSpace(Vector2 worldPos, int gridPrecision)
    {
        return (worldPos - mapPos) / gridPrecision;
    }

    //same as above, but from grid space to world space
    public Vector2 GridToWorldSpace(Vector2 vfPos, int gridPrecision)
    {
        return (vfPos * gridPrecision) + mapPos;
    }

    //same as WorldToGridSpace, but only affects x and y
    public Vector3 WorldToGridSpace(Vector3 worldPos, int gridPrecision)
    {
        //save Z, transform X and Y
        float z = worldPos.z;
        Vector2 conv = WorldToGridSpace((Vector2)worldPos, gridPrecision);

        //return new X and Y with old Z
        return new Vector3(conv.x, conv.y, z);
    }

    //same as above, but from grid space to world space
    public Vector3 GridToWorldSpace(Vector3 vfPos, int gridPrecision)
    {
        //save Z, transform X and Y
        float z = vfPos.z;
        Vector2 conv = GridToWorldSpace((Vector2)vfPos, gridPrecision);

        //return new X and Y with old Z
        return new Vector3(conv.x, conv.y, z);
    }

    //floors the vector components so they can be used as a 2d array index
    public Vector2 PosToIndex(Vector2 pos)
    {
        return new Vector2((int)Math.Floor(pos.x), (int)Math.Floor(pos.y));
    }

    public Vector2 WorldToGridIndex(Vector2 worldPos, int gridPrecision)
    {
        return PosToIndex(WorldToGridSpace(worldPos, gridPrecision));
    }

    public bool IsWorldPosOnMap(Vector2 worldPos)
    {
        //worldPos = WorldToGridSpace(worldPos, gridPrecision);
        worldPos = worldPos - mapPos;
        return !(worldPos.x > mapSize.x-1 || worldPos.x < 1 || worldPos.y > mapSize.y-1 || worldPos.y < 1);
    }

    public bool IsGridPosOnMap(Vector2 gridPos, int gridPrecision)
    {
        Vector2 mapSizeInGridPos = new Vector2(Mathf.Floor(mapSize.x / gridPrecision), Mathf.Floor(mapSize.y / gridPrecision));
        return !(gridPos.x < 0 || gridPos.x >= mapSizeInGridPos.x-1 || gridPos.y < 0 || gridPos.y >= mapSizeInGridPos.y-1);
    }
    //returns a random world position that's within the bounds of the map
    public Vector2 GetRandomMapPosAsWorldPos()
    {
        float x = UnityEngine.Random.Range(0, mapSize.x) + mapPos.x;
        float y = UnityEngine.Random.Range(0, mapSize.y) + mapPos.y;
        return new Vector2(x, y);
    }

    public Vector2 GetRandomSeaLevelMapPosAsWorldPos()
    {
        Vector2 pos;
        do
        {
            pos = GetRandomMapPosAsWorldPos();
        }
        while (GetAltitudeAtPos(pos) != 0);
        return pos;
    }

    //returns the centroid of an array of points
    public static Vector2 GetCentroid(Vector2[] points)
    {
        Vector2 sum = Vector2.zero;
        for (int c = 0; c < points.Length; c++)
        {
            sum += points[c];
        }
        return sum / points.Length;

    }

    //takes a color value and returns the 
    float GetAltitudeForColor(Color c)
    {
        //water, shallow to deep
        if (AreColorsWithinTolerance(c, new Color(138f / 256f, 188 / 256f, 226 / 256f), .05f))
            return -1;
        else if (AreColorsWithinTolerance(c, new Color(96 / 256f, 156 / 256f, 230 / 256f), .05f))
            return -2;
        else if (AreColorsWithinTolerance(c, new Color(69 / 256f, 124 / 256f, 225 / 256f), .05f))
            return -3;
        else if (AreColorsWithinTolerance(c, new Color(50 / 256f, 108 / 256f, 226 / 256f), .05f))
            return -4;
        else if (AreColorsWithinTolerance(c, new Color(42 / 256f, 99 / 256f, 225 / 256f), .05f))
            return -5;
        //land, low to high
        else if (AreColorsWithinTolerance(c, new Color(194 / 256f, 255 / 256f, 167 / 256f), .05f))
            return 1;
        else if (AreColorsWithinTolerance(c, new Color(162 / 256f, 255 / 256f, 122 / 256f), .05f))
            return 2;
        else if (AreColorsWithinTolerance(c, new Color(155 / 256f, 237 / 256f, 105 / 256f), .05f))
            return 3;
        else if (AreColorsWithinTolerance(c, new Color(145 / 256f, 210 / 256f, 80 / 256f), .05f))
            return 4;
        else if (AreColorsWithinTolerance(c, new Color(145 / 256f, 167 / 256f, 53 / 256f), .05f))
            return 5;
        else return 0;
    }
    static bool AreColorsWithinTolerance(Color c1, Color c2, float tolerance)
    {
        return Math.Abs(c1.r - c2.r) < tolerance && Math.Abs(c1.g - c2.g) < tolerance && Math.Abs(c1.b - c2.b) < tolerance && Math.Abs(c1.a - c2.a) < tolerance;
    }

    //same as the cpm vector method
    //averages the nearest four altitudes to the given position
    public float GetAltitudeAtPos(Vector2 pos)
    {
        //correct for map offset
        pos = WorldToGridSpace(pos, altitudeFieldPrecision);
        //if we're off the map, return normalized vector toward map center
        if (!IsGridPosOnMap(pos, altitudeFieldPrecision))
            return 0;

        float returnValue = 0;

        //get the nearest ints for both dimensions in both directions
        int xUp = (int)Math.Ceiling(pos.x);
        int xDown = (int)Math.Floor(pos.x);
        int yUp = (int)Math.Ceiling(pos.y);
        int yDown = (int)Math.Floor(pos.y);

        //loop in 2d from the bottom ints to the top
        for (int n = xDown; n <= xUp; n++)
        {
            for (int c = yDown; c <= yUp; c++)
            {
                //get the value for the current ints
                float nearVec = altitudeField[n, c];
                //scale factor, closer is stronger
                float scaleFactor = (1 - Math.Abs(n - pos.x)) * (1 - Math.Abs(c - pos.y));
                //add scaled field vector to return vector
                returnValue += nearVec * scaleFactor;
                //Debug.DrawRay(new Vector2(n, c), nearVec * scaleFactor, Color.blue, Time.deltaTime);
            }
        }
//        Debug.DrawRay(new Vector3(pos.x,pos.y,-5), new Vector2(0, returnValue),Color.red,Time.deltaTime*4);
        return returnValue / 4;
    }

    //refine this
    //each unit of altitude removes a sixth of the multiplier
    public float GetSpeedModifierFromAltitudeAtPos(Vector2 pos)
    {
        float alt = GetAltitudeAtPos(pos);
        float altmod = 1 - (Math.Abs(alt) / 6);
        return altmod;
    }

    public Mesh GetBlankMeshFilterPlane(float precision)
    {
        Vector2 centerPoint = mapPos + mapSize / 2;
        GameObject meshPrefab = (GameObject)Instantiate(Resources.Load("BlankMeshPlane"), mapPos, Quaternion.identity);
        Mesh theMesh = ((MeshFilter)meshPrefab.GetComponent(typeof(MeshFilter))).mesh;
        Vector2 meshSize = new Vector2((int)(Math.Ceiling(mapSize.x / precision))+1, (int)(Math.Ceiling(mapSize.y / precision))+1);
        Vector3[] vertices = new Vector3[(int)meshSize.x * (int)meshSize.y];
        int[] triangles = new int[(((int)meshSize.x - 1) * ((int)meshSize.y - 1)) * 6];
        
        //int currentTriCluster = 0;
        for (int row = 0; row < meshSize.x-1; row++)
        {
            for (int col = 0; col < meshSize.y-1; col++)
            {
                //indices
                int current = (int)meshSize.x * row + col;
                int next = current + 1;
                int nextRow = current + (int)meshSize.x;
                int nextRowNext = nextRow + 1;

                if (row == 0)
                {
                    vertices[current] = new Vector3(col, row)*precision;
                    Debug.DrawRay(GridToWorldSpace(vertices[current], 1), new Vector2(0, 1), Color.blue, float.MaxValue);
                    vertices[next] = new Vector3(col + 1, row)*precision;
                    Debug.DrawRay(GridToWorldSpace(vertices[next], 1), new Vector2(0, 1), Color.blue, float.MaxValue);
                }
                vertices[nextRow] = new Vector3(col, row + 1)*precision;
                Debug.DrawRay(GridToWorldSpace(vertices[nextRow], 1), new Vector2(0, 1), Color.blue, float.MaxValue);
                vertices[nextRowNext] = new Vector3(col + 1, row + 1)*precision;
                Debug.DrawRay(GridToWorldSpace(vertices[nextRowNext], 1), new Vector2(0, 1), Color.blue, float.MaxValue);

                //int currentTriCluster = current * 6;
                int currentTriCluster = (((int)meshSize.x - 1) * row + col) *6;
                triangles[currentTriCluster] = current;
                triangles[currentTriCluster + 1] = nextRow;
                triangles[currentTriCluster + 2] = next;
                triangles[currentTriCluster + 3] = next;
                triangles[currentTriCluster + 4] = nextRow;
                triangles[currentTriCluster + 5] = nextRowNext;
                //currentTriCluster += 6;
                
            }
        }
        Color[] color = new Color[vertices.Length];
        for (int c = 0; c < color.Length; c++)
            color[c] = new Color(0, 0, 0, 0);

        theMesh.vertices = vertices;
        theMesh.triangles = triangles;
        theMesh.colors = color;
        return theMesh;
    }
}

public class DayEndEventArgs : EventArgs
{
    public DayEndEventArgs()
    {
    }
}
