using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;

public class ModifySplinePoint : MonoBehaviour
{
    [SerializeField] private SplineComputer spline;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [SerializeField] private List<Vector3> Positions;
    void Start()
    {

    }

    void Update()
    {
        if (startPoint != null)
        {
            startPoint.position = Positions[0] + new Vector3(0, (Time.deltaTime * 0.05f), 0);
        }
        if (endPoint != null)
        {
            endPoint.position = Positions[1] + new Vector3(0, (Time.deltaTime * 0.05f), 0);
        }

        //spline.SetPoints(spline.GetPoints());
        spline.Rebuild(false);
    }
}
