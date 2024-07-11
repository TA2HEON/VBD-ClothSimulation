using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloth : MonoBehaviour
{
    public float nodeSpacing = 0.1f;
    public int width = 20;
    public int height = 20;
    public float damping = 0.1f;
    public float timeStep = 0.02f;
    public float gravity = -9.81f;
    public int solverIterations = 5;
    public Material clothMaterial;
    public float pullForce = 1f; // 상단 노드를 당기는 힘
    public float maxPullDistance = 1f; // 최대 당기려고 하는 거리

    private Node[,] nodes;
    private List<DistanceConstraint> constraints = new List<DistanceConstraint>();
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Start()
    {
        CreateNodes();
        ConnectNodes();
        CreateMesh();
        StartCoroutine(SimulationLoop());
    }

    void CreateNodes()
    {
        nodes = new Node[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                Vector3 position = new Vector3(j * nodeSpacing, i * nodeSpacing, 0);
                nodes[i, j] = new Node(position, 1f);
                // 하단 노드 고정
                if (i == 0)
                {
                    nodes[i, j].isFixed = true;
                }
            }
        }
    }

    void ConnectNodes()
    {
        // Structural constraints
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width - 1; j++)
            {
                constraints.Add(new DistanceConstraint(nodes[i, j], nodes[i, j + 1], nodeSpacing));
            }
        }

        for (int i = 0; i < height - 1; i++)
        {
            for (int j = 0; j < width; j++)
            {
                constraints.Add(new DistanceConstraint(nodes[i, j], nodes[i + 1, j], nodeSpacing));
            }
        }

        // Shear constraints
        for (int i = 0; i < height - 1; i++)
        {
            for (int j = 0; j < width - 1; j++)
            {
                constraints.Add(new DistanceConstraint(nodes[i, j], nodes[i + 1, j + 1], nodeSpacing * Mathf.Sqrt(2)));
                constraints.Add(new DistanceConstraint(nodes[i + 1, j], nodes[i, j + 1], nodeSpacing * Mathf.Sqrt(2)));
            }
        }

        // Bending constraints
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width - 2; j++)
            {
                constraints.Add(new DistanceConstraint(nodes[i, j], nodes[i, j + 2], nodeSpacing * 2));
            }
        }

        for (int i = 0; i < height - 2; i++)
        {
            for (int j = 0; j < width; j++)
            {
                constraints.Add(new DistanceConstraint(nodes[i, j], nodes[i + 2, j], nodeSpacing * 2));
            }
        }
    }

    void CreateMesh()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = clothMaterial;

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        UpdateMesh();
    }

    void UpdateMesh()
    {
        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uv = new Vector2[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int index = i * width + j;
                vertices[index] = nodes[i, j].position;
                uv[index] = new Vector2((float)j / (width - 1), (float)i / (height - 1));
            }
        }

        int triangleIndex = 0;
        for (int i = 0; i < height - 1; i++)
        {
            for (int j = 0; j < width - 1; j++)
            {
                int index = i * width + j;

                triangles[triangleIndex++] = index;
                triangles[triangleIndex++] = index + width;
                triangles[triangleIndex++] = index + 1;

                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + width;
                triangles[triangleIndex++] = index + width + 1;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    IEnumerator SimulationLoop()
    {
        while (true)
        {
            UpdateSimulation();
            UpdateMesh();
            yield return new WaitForSeconds(timeStep);
        }
    }

    void UpdateSimulation()
    {
    ApplyExternalForces();
    UpdateVelocities();
    UpdatePositions();
    SolveConstraints();
    UpdateVelocitiesFromPositions();

    // 디버그 로그
    Debug.Log($"Top node position: {nodes[height-1, width/2].position.y}");
    }

    void ApplyExternalForces()
{
    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            if (!nodes[i, j].isFixed)
            {
                if (i == height - 1) // 상단 노드에만 힘 적용
                {
                    float currentStretch = nodes[i, j].position.y - nodes[i, j].initialPosition.y;
                    float remainingStretch = maxPullDistance - currentStretch;
                    if (remainingStretch > 0)
                    {
                        //Vector3 force = Vector3.up * pullForce * remainingStretch;
                        //nodes[i, j].velocity += force * timeStep;

                        nodes[i,j].velocity += Vector3.up * pullForce * timeStep;
                    }
                }
            }
        }
    }
}

    void UpdateVelocities()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (!nodes[i, j].isFixed)
                {
                    nodes[i, j].velocity *= (1 - damping);
                }
            }
        }
    }

    void UpdatePositions()
{
    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            if (!nodes[i, j].isFixed)
            {
                nodes[i, j].prevPosition = nodes[i, j].position;
                nodes[i, j].position += nodes[i, j].velocity * timeStep;

                if (i == height - 1) // 상단 노드의 최대 이동 거리 제한
                {
                    float maxY = nodes[i, j].initialPosition.y + maxPullDistance;
                    nodes[i, j].position.y = Mathf.Min(nodes[i, j].position.y, maxY);
                }
            }
        }
    }
}

    void SolveConstraints()
    {
        for (int iteration = 0; iteration < solverIterations; iteration++)
        {
            foreach (var constraint in constraints)
            {
                constraint.Solve();
            }

            // 고정된 노드(하단)의 위치를 원래 위치로 복원
            for (int j = 0; j < width; j++)
            {
                nodes[0, j].position = nodes[0, j].initialPosition;
            }
        }
    }

    void UpdateVelocitiesFromPositions()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (!nodes[i, j].isFixed)
                {
                    nodes[i, j].velocity = (nodes[i, j].position - nodes[i, j].prevPosition) / timeStep;
                }
            }
        }
    }

    void OnDrawGizmos()
{
    if (nodes == null) return;
    for (int i = 0; i < height; i++)
    {
        for (int j = 0; j < width; j++)
        {
            if (i == 0)
                Gizmos.color = Color.red; // 하단 노드
            else if (i == height - 1)
                Gizmos.color = Color.green; // 상단 노드
            else
                Gizmos.color = Color.blue; // 중간 노드
            Gizmos.DrawSphere(nodes[i, j].position, 0.05f);
        }
    }
}
}
