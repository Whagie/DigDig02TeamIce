using Dreamteck.Splines;
using Dreamteck.Splines.Examples;
using System.Collections.Generic;
using UnityEngine;

public class ExampleJunctionHandler : MonoBehaviour
{
    SplineTracer follower;

    private void Awake()
    {
        follower = GetComponent<SplineFollower>();
    }

    private void OnEnable()
    {
        follower.onNode += OnNode; //onNode is called every time the follower passes by a Node
    }

    private void OnDisable()
    {
        follower.onNode -= OnNode;
    }

    private void OnNode(List<SplineTracer.NodeConnection> passed)
    {
        Node node = passed[0].node;
        Node.Connection[] connections = node.GetConnections();

        Debug.Log("Reached node " + node.name + " connected at point " + passed[0].point);

        // If this node has only one spline, there's no junction
        if (connections.Length <= 1) return;

        // Try to find a JunctionSwitch component on this node
        JunctionSwitch junctionSwitch = node.GetComponent<JunctionSwitch>();
        if (junctionSwitch == null || junctionSwitch.bridges.Length == 0)
        {
            Debug.Log("No JunctionSwitch found on node " + node.name);
            return;
        }

        // Find which connection the follower is currently on
        int currentConnection = -1;
        for (int i = 0; i < connections.Length; i++)
        {
            if (connections[i].spline == follower.spline && connections[i].pointIndex == passed[0].point)
            {
                currentConnection = i;
                break;
            }
        }

        if (currentConnection == -1) return; // Safety check

        // Iterate all bridges defined in the JunctionSwitch
        foreach (JunctionSwitch.Bridge bridge in junctionSwitch.bridges)
        {
            if (!bridge.active) continue;
            if (bridge.a == bridge.b) continue;

            // Skip bridges not involving our current connection
            if (currentConnection != bridge.a && currentConnection != bridge.b) continue;

            // If we’re on one side of the bridge, check direction and switch
            if (currentConnection == bridge.a)
            {
                if ((int)follower.direction != (int)bridge.bDirection) continue;
                SwitchSpline(connections[bridge.a], connections[bridge.b]);
                return;
            }
            else
            {
                if ((int)follower.direction != (int)bridge.aDirection) continue;
                SwitchSpline(connections[bridge.b], connections[bridge.a]);
                return;
            }
        }
    }

    void SwitchSpline(Node.Connection from, Node.Connection to)
    {
        //See how much units we have travelled past that Node in the last frame
        float excessDistance = follower.spline.CalculateLength(follower.spline.GetPointPercent(from.pointIndex), follower.UnclipPercent(follower.result.percent));

        //Set the spline to the follower
        follower.spline = to.spline;
        follower.RebuildImmediate();

        //Get the location of the junction point in percent along the new spline
        double startpercent = follower.ClipPercent(to.spline.GetPointPercent(to.pointIndex));

        if (Vector3.Dot(from.spline.Evaluate(from.pointIndex).forward, to.spline.Evaluate(to.pointIndex).forward) < 0f)
        {
            if (follower.direction == Spline.Direction.Forward) follower.direction = Spline.Direction.Backward;
            else follower.direction = Spline.Direction.Forward;
        }

        //Position the follower at the new location and travel excessDistance along the new spline

        follower.SetPercent(follower.Travel(startpercent, excessDistance, follower.direction));
    }
}