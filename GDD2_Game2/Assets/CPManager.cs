using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class CPManager : MonoBehaviour {
    //list of all active control points
    List<ControlPoint> CPs = new List<ControlPoint>();

    //field of movement vectors
    Vector2[,] vectorField;
    Vector2 mapSize = new Vector2(100, 100);

    bool recalculateVF = false;
    public int recalcInterval = 5;

    void Awake()
    {
        //change to use precision global, scaled to the map's aspect ratio
        vectorField = new Vector2[(int)mapSize.x, (int)mapSize.y];
        //fill with 0,0s to start
        for (int n = 0; n < vectorField.GetLength(0); n++)
            for (int c = 0; c < vectorField.GetLength(1); c++)
                vectorField[n, c] = new Vector2(0, 0);
    }

	// Use this for initialization
	void Start () {        
        
	}
	
	// Update is called once per frame
	void Update () {
        //debug lines
        for (int n = 0; n < vectorField.GetLength(0); n++)
            for (int c = 0; c < vectorField.GetLength(1); c++)
                Debug.DrawRay(new Vector3(n, c, 0), (Vector3)vectorField[n, c], Color.green, Time.deltaTime);

        //check for queued VF recalculations at every x frames (where x is recalcInterval)
        if (recalculateVF && Time.frameCount % recalcInterval == 0)
        {
            recalculateVF = false;
            RecalculateVectorField();
        }
        //if(Input.GetMouseButtonDown(0))
    }

    public void AddCP(ControlPoint cp)
    {
        CPs.Add(cp);
        RecalculateVectorField();
    }

    public void RemoveCP(ControlPoint cp)
    {
        CPs.Remove(cp);
        RecalculateVectorField();
    }

    void RecalculateVectorField()
    {
        //holds the vectors from current point to each control point
        Vector2[] cpVectors;

        //iterate through the dimensions of the vector field
        for (int row = 0; row < mapSize.x; row++)
        {
            for (int col = 0; col < mapSize.y; col++)
            {
                //reinitialize cp vectors
                cpVectors = new Vector2[CPs.Count];

                //get the current point
                Vector2 p = new Vector2(row, col);

                //tracks the magnitude of the longest cp vector
                float longestMagnitudeSqr = 0.0f;
                //iterate through length of cp list
                for (int c = 0; c < CPs.Count; c++)
                {
                    //get cp and its position
                    ControlPoint cp = CPs[c];
                    Vector2 cpPosition = new Vector2(cp.transform.position.x, cp.transform.position.y);
                    //cp vector is vector from position to cp position
                    cpVectors[c] = cpPosition - p;

                    //check for new longest magnitude
                    if (cpPosition.sqrMagnitude > longestMagnitudeSqr) longestMagnitudeSqr = cpPosition.sqrMagnitude;
                }

                //we'll multiply all vectors by a scale factor so that the closest ones matter more
                //furthest vector is 0, <0,0> is 1
                //float scaleFactorBase = 1 - (1 / longestMagnitude);
                Vector2 calculatedVector = new Vector2(0, 0);

                //don't apply scale factor if there's only 1 cp
                if (cpVectors.Length == 1)
                    calculatedVector = cpVectors[0];
                //otherwise,
                else
                    //add all scaled cp vectors
                    foreach (Vector2 v in cpVectors)
                        calculatedVector += v * ((longestMagnitudeSqr - v.sqrMagnitude) / v.sqrMagnitude);

                //final vector is the sum normalized
                vectorField[row, col] = calculatedVector.normalized;           
            }
        }
    }

    //Gets estimated field vector for a given position
    //Averages nearest four pre-computed field vectors with weightings proportional to their closeness
    public Vector2 GetVectorAtPosition(Vector2 pos)
    {
        //if we're off the map, return normalized vector toward map center
        if (pos.x > mapSize.x - 1 || pos.x < 1 || pos.y > mapSize.y - 1 || pos.y < 1)
            return (new Vector2(mapSize.x / 2, mapSize.y / 2) - pos).normalized;

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
}
