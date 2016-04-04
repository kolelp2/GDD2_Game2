using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapTile : MonoBehaviour {
    int[][] tileables;
    [SerializeField]
    int[] left;
    [SerializeField]
    int[] right;
    [SerializeField]
    int[] up;
    [SerializeField]
    int[] down;
    [SerializeField]
    int rawPosition;
    public MapTileRawPositions RawPosition
    {
        get
        {
            return (MapTileRawPositions)rawPosition;
        }
    }

    void Awake()
    {
        tileables = new int[4][];
        tileables[(int)MapTileDirections.Left] = left;
        tileables[(int)MapTileDirections.Up] = up;
        tileables[(int)MapTileDirections.Right] = right;
        tileables[(int)MapTileDirections.Down] = down;
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public List<int> GetTileablesList(MapTileDirections dir, float rotationCCW)
    {
        rotationCCW = Mathf.Round(rotationCCW);
        //90 degree increments only
        if ((int)rotationCCW % 90 != 0)
            return new List<int>();

        int dirInt = (int)dir;

        int offset = (int)rotationCCW / 90;
        if (offset >= 4)
            offset = offset % 4;

        dirInt = dirInt + offset;
        if (dirInt >= 4)
            dirInt %= 4;

        return new List<int>(tileables[dirInt]);
    }
}

public enum MapTileDirections { Left = 0, Up = 1, Right = 2, Down = 3 }
public enum MapTileRawPositions { TopLeft = 0, TopRight = 1, BottomRight = 2, BottomLeft = 3 }
public enum MapTileOrientations { Zero = 0, Ninety = 1, OneEighty = 2, TwoSeventy = 3 }
