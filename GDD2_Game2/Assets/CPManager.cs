using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CPManager : MonoBehaviour {
    //list of all active control points
    HashSet<ControlPoint> CPs = new HashSet<ControlPoint>();

    //field of movement vectors
    Vector2[,] vectorField;
    Vector2 vfSize;
    Vector2 mapPos;

    bool recalculateVF = false;
    [SerializeField]
    int recalcInterval = 5;
    [SerializeField]
    int vfPrecision = 1;

    void Awake()
    {
        
    }

	// Use this for initialization
	void Start () {
        //get the map's collider
        BoxCollider2D mapBox = (BoxCollider2D)GameObject.Find("Map").GetComponent(typeof(BoxCollider2D));
        vfSize = mapBox.size / vfPrecision; //vf size is the collider's size divided by the precision
        //mapPos is the position of the vf's bottom-left corner in world space
        //the vf is bottom-left anchored, but the collider is center anchored. we have to get the collider's bottom-left corner
        mapPos = new Vector2(mapBox.transform.position.x - mapBox.size.x / 2, mapBox.transform.position.y - mapBox.size.y / 2);

        //change to use precision global, scaled to the map's aspect ratio
        vectorField = new Vector2[(int)vfSize.x, (int)vfSize.y];
        //fill with 0,0s to start
        for (int n = 0; n < vectorField.GetLength(0); n++)
            for (int c = 0; c < vectorField.GetLength(1); c++)
                vectorField[n, c] = new Vector2(0, 0);
    }
	
	// Update is called once per frame
	void Update () {
        //debug lines
        /*for (int n = 0; n < vectorField.GetLength(0); n++)
            for (int c = 0; c < vectorField.GetLength(1); c++)
                Debug.DrawRay(VFToWorldSpace(new Vector3(n, c, 0)), (Vector3)vectorField[n, c].normalized, Color.green, Time.deltaTime);*/

        //check for queued VF recalculations at every x frames (where x is recalcInterval)
        if (recalculateVF && Time.frameCount % recalcInterval == 0)
        {
            recalculateVF = false;
            StartCoroutine("RecalculateVectorField");
        }
        //if(Input.GetMouseButtonDown(0))
    }

    public void AddCP(ControlPoint cp)
    {
        CPs.Add(cp);
        QueueVFRecalculation();
    }

    public void RemoveCP(ControlPoint cp)
    {
        CPs.Remove(cp);
        //skip a frame to allow the CP to kill itself before we start
        Destroy(cp.gameObject);
        StopCoroutine("RecalculateVectorField");
        QueueVFRecalculation();
    }

    IEnumerator RecalculateVectorField()
    {
        //only process as many frames as necessary to get the vf recalculated within the recalc window
        int calculationsPerFrame = (int)Math.Ceiling((vfSize.x * vfSize.y) / recalcInterval);

        //holds the vectors from current point to each control point
        Vector2[] cpVectors = new Vector2[CPs.Count];
        ControlPoint[] cps = new ControlPoint[CPs.Count];
        CPs.CopyTo(cps);

        //iterate through the dimensions of the vector field
        for (int row = 0; row < vectorField.GetLength(0); row++)
        {
            for (int col = 0; col < vectorField.GetLength(1); col++)
            {
                //reinitialize cp vectors
                Array.Clear(cpVectors, 0, cpVectors.Length);

                //get the current point in VF space
                Vector2 p = new Vector2(row, col);

                //tracks the magnitude of the longest cp vector
                float longestMagnitudeSqr = 0.0f;
                Vector2 firstCPVector = new Vector2(0,0);
                //iterate through length of cp list
                int c = 0;
                foreach(ControlPoint cp in cps)
                {
                    //ControlPoint cp = [c];
                    Vector2 cpPosition = WorldToVFSpace(new Vector2(cp.transform.position.x, cp.transform.position.y));
                    //cp vector is vector from position to cp position
                    cpVectors[c] = cpPosition - p;
                    c++;

                    //check for new longest magnitude
                    if (cpPosition.sqrMagnitude > longestMagnitudeSqr) longestMagnitudeSqr = cpPosition.sqrMagnitude;
                }

                //we'll multiply all vectors by a scale factor so that the closest ones matter more
                //furthest vector is 0, <0,0> is 1
                //float scaleFactorBase = 1 - (1 / longestMagnitude);
                Vector2 calculatedVector = new Vector2(0, 0);

                //if there's only one CP, don't apply scale factor
                if (cpVectors.Length == 1)
                    calculatedVector = cpVectors[0];
                //otherwise...
                else
                    //add all scaled cp vectors
                    foreach (Vector2 v in cpVectors)
                        calculatedVector += v * (longestMagnitudeSqr - v.sqrMagnitude) / v.sqrMagnitude;

                //final vector is the sum normalized
                vectorField[row, col] = calculatedVector;

                //if we've done the last in this batch, stop until the next frame
                if ((row * col) % calculationsPerFrame == calculationsPerFrame - 1) yield return null;
            }
        }
    }

    //Gets estimated field vector for a given position
    //Averages nearest four pre-computed field vectors with weightings proportional to their closeness
    public Vector2 GetVectorAtPosition(Vector2 pos)
    {
        //correct for map offset
        pos = WorldToVFSpace(pos);
        //if we're off the map, return normalized vector toward map center
        if (!IsVFPosWithinVF(pos))
            return (VFToWorldSpace(new Vector2(vfSize.x / 2, vfSize.y / 2) - pos)).normalized;

        Vector2 returnVector = new Vector2(0, 0);

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
                //get the field vector for the current ints
                Vector2 nearVec = vectorField[n, c];
                //scale factor, closer is stronger
                float scaleFactor = (1 - Math.Abs(n - pos.x)) * (1 - Math.Abs(c - pos.y));
                //add scaled field vector to return vector
                returnVector += nearVec * scaleFactor;
                //Debug.DrawRay(new Vector2(n, c), nearVec * scaleFactor, Color.blue, Time.deltaTime);
            }
        }

        //return normalized return vector
        return returnVector.normalized;
    }

    public void QueueVFRecalculation()
    {
        recalculateVF = true;
    }

    void OnMouseDown()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 newPos = new Vector3(mousePos.x, mousePos.y, ControlPoint.DrawDepth);
        if (IsWorldPosWithinVF(newPos))
            Instantiate(Resources.Load("control point"), newPos, Quaternion.identity);
    }

    //takes a vector representing a point in the world and returns a vector representing the corresponding point in the vector field
    Vector2 WorldToVFSpace(Vector2 worldPos)
    {
        return (worldPos - mapPos) / vfPrecision;
    }

    //same as above, but from vector field space to world space
    Vector2 VFToWorldSpace(Vector2 vfPos)
    {
        return (vfPos * vfPrecision) + mapPos;
    }

    //same as WorldToVFSpace, but only affects x and y
    Vector3 WorldToVFSpace(Vector3 worldPos)
    {
        //save Z, transform X and Y
        float z = worldPos.z;
        Vector2 conv = WorldToVFSpace((Vector2)worldPos);

        //return new X and Y with old Z
        return new Vector3(conv.x, conv.y, z);
    }

    //same as above, but from vf space to world space
    Vector3 VFToWorldSpace(Vector3 vfPos)
    {
        //save Z, transform X and Y
        float z = vfPos.z;
        Vector2 conv = VFToWorldSpace((Vector2)vfPos);

        //return new X and Y with old Z
        return new Vector3(conv.x, conv.y, z);
    }

    bool IsWorldPosWithinVF(Vector2 worldPos)
    {
        worldPos = WorldToVFSpace(worldPos);
        return !(worldPos.x > vfSize.x - 1 || worldPos.x < 1 || worldPos.y > vfSize.y - 1 || worldPos.y < 1);
    }

    bool IsVFPosWithinVF(Vector2 vfPos)
    {
        return !(vfPos.x > vfSize.x - 1 || vfPos.x < 1 || vfPos.y > vfSize.y - 1 || vfPos.y < 1);
    }
}
