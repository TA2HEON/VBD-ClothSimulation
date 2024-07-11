using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceConstraint
{
    public Node node1, node2;
    public float restLength;

    public DistanceConstraint(Node node1, Node node2, float restLength)
    {
        this.node1 = node1;
        this.node2 = node2;
        this.restLength = restLength;
    }

    public void Solve()
    {
        Vector3 delta = node2.position - node1.position;
        float distance = delta.magnitude;
        float correction = (distance - restLength) / (node1.invMass + node2.invMass);
        
        if (!node1.isFixed)
            node1.position += correction * node1.invMass * delta.normalized;
        if (!node2.isFixed)
            node2.position -= correction * node2.invMass * delta.normalized;
    }
}