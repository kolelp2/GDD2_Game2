using UnityEngine;
using System.Collections;
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
    // Use this for initialization
    void Start () {
        BoxCollider2D mapBox = (BoxCollider2D)gameObject.GetComponent(typeof(BoxCollider2D));
        mapSize = mapBox.size;

        mapPos = new Vector2(mapBox.transform.position.x - mapBox.size.x / 2, mapBox.transform.position.y - mapBox.size.y / 2);

        //place tiles
        //get number of tiles in both directions
        int tileX = (int)Math.Ceiling(mapSize.x / tileDimension);
        int tileY = (int)Math.Ceiling(mapSize.y / tileDimension);
        Sprite[,] mapSprites = new Sprite[tileX, tileY];
        int[,] spriteRotations = new int[tileX, tileY];
        //iterate through the 2d tile grid
        for(int row = 0; row < tileX; row++)
        {
            for(int col = 0; col < tileY; col++)
            {
                //get a random rotation in 90deg intervals
                int rotationDegrees = UnityEngine.Random.Range(0, 4) * 90;
                Quaternion rotation = Quaternion.Euler(0, 0, rotationDegrees);
                spriteRotations[row, col] = rotationDegrees;
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

                //get the tile
                Sprite currentTile = mapSprites[(int)asGridIndex.x, (int)asGridIndex.y];
                //get the color - this is the color of the pixel directly underneath the current field point
                Color fieldPointColor = currentTile.texture.GetPixel((int)(asTilePercentage.x * currentTile.rect.width), (int)(asTilePercentage.y * currentTile.rect.height));

                altitudeField[row, col] = GetAltitudeForColor(fieldPointColor);
            }
        }

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
        return 0;
    }
}

public class DayEndEventArgs : EventArgs
{
    public DayEndEventArgs()
    {
    }
}
