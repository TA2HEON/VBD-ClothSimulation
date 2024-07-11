using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3 position;
    public Vector3 prevPosition;
    public Vector3 velocity;
    public float invMass;
    public bool isFixed;
    public Vector3 initialPosition;

    public Node(Vector3 initialPosition, float mass)
    {
        this.position = initialPosition;
        this.prevPosition = initialPosition;
        this.initialPosition = initialPosition;
        this.velocity = Vector3.zero;
        this.invMass = 1f / mass;
        this.isFixed = false;
    }
}