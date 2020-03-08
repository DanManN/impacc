using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KdTree;
using KdTree.Math;

public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;

    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism, bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    private KdTree.KdTree<float, int> ktree = null;


    public double TOLERANCE = 0.00001;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }

    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();
        DrawBoundingBoxes();
        DrawTree();

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions

    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        ktree = new KdTree<float, int>(2, new FloatMath());

        for (int i = 0; i < prisms.Count; i++)
        {
            var prism = prisms[i];
            foreach (var point in prism.points)
                ktree.Add(new[] { point.x, point.z }, i);
        }

        for (int i = 0; i < prisms.Count; i++)
        {
            if (prisms[i].points.Length == 0)
                continue;
            var prismI = prisms[i];
            var prismTransformI = prismObjects[i].transform;
            var bboxI = BoundingBox(prismI.points);
            float radius = Vector3.Distance(bboxI[0], bboxI[1]) / 2;
            print(radius);
            float[] position = new float[] { prismTransformI.position.x, prismTransformI.position.z };
            foreach (KdTree.KdTreeNode<float, int> node in ktree.RadialSearch(position, radius))
            {
                int j = node.Value;
                if (j == i) continue;
                var prismJ = prisms[j];
                var prismTransformJ = prismObjects[j].transform;
                var bboxJ = BoundingBox(prismJ.points);
                if (AreBoxesColliding(bboxI, bboxJ))
                {
                    var checkPrisms = new PrismCollision();
                    checkPrisms.a = prisms[i];
                    checkPrisms.b = prisms[j];

                    yield return checkPrisms;
                }
            }
        }
        yield break;
    }

    // Support functions for building Simplex

    // Ref: https://stackoverflow.com/questions/17386299/get-farthest-vertice-on-a-specified-direction-of-a-3d-model-unity/17407420
    private Vector3 getFarthestPointInDirection(Prism shape, Vector3 direction)
    {
        Vector3 farthestPoint = shape.points[0];
        float farDistance = 0f;

        foreach(Vector3 vert in shape.points)
        {
            float tmp = Vector3.Dot(direction,vert);
            if(tmp > farDistance)
            {
                farDistance = tmp;
                farthestPoint = vert;
            }
        }
        // Debug.Log("farthestPoint: " + farthestPoint);

        return farthestPoint;
    }

    // Ref: http://www.dyn4j.org/2010/04/gjk-gilbert-johnson-keerthi/
    private Vector3 support(Prism shape1, Prism shape2, Vector3 d) 
    {
        // d is a vector direction (doesn't have to be normalized)
        // get points on the edge of the shapes in opposite directions
        Vector3 p1 = getFarthestPointInDirection(shape1, d);
        Vector3 p2 = getFarthestPointInDirection(shape2, -d);
        // perform the Minkowski Difference
        Vector3 p3 = p1 - p2;
        // p3 is now a point in Minkowski space on the edge of the Minkowski Difference
        return p3;
    }


    private Simplex GJK_collision(Prism A, Prism B, Simplex simplex)
    {
        Vector3 ORIGIN = Vector3.zero;
        // choose a search direction
        Vector3 d = new Vector3((Random.value - 0.5f) * 2, 0, (Random.value - 0.5f) * 2);
        // get the first Minkowski Difference point
        simplex.add(support(A, B, d));
        // negate d for the next point
        d = -d;
        // start looping
        while (true) {
            Debug.Log("Test...");
            // add a new point to the simplex because we haven't terminated yet
            simplex.add(support(A, B, d));
            // make sure that the last point we added actually passed the origin
            var lastVert = simplex.getLast();
            if (Vector3.Dot(lastVert, d) <= 0) 
            {
                // if the point added last was not past the origin in the direction of d
                // then the Minkowski Sum cannot possibly contain the origin since
                // the last point added is on the edge of the Minkowski Difference
                return null;
            } else 
            {
                // otherwise we need to determine if the origin is in
                // the current simplex
                if (simplex.containsOrigin(d)) 
                {
                    // if it does then we know there is a collision
                    return simplex;
                } else {
                    // otherwise we cannot be certain so find the edge who is
                    // closest to the origin and use its normal (in the direction
                    // of the origin) as the new d and continue the loop
                    d = simplex.getDirection();
                }
            }
        }
    }

    // Ref: http://www.dyn4j.org/2010/05/epa-expanding-polytope-algorithm/
    private (double, Vector3, int) findClosestEdge(Simplex simplex)
    {

        double closest_dist = double.MaxValue;
        Vector3 closest_norm = Vector3.zero;
        int closest_index = -1;

        for (int i = 0; i < simplex.vertices.Count; i++)
        {
            int j = i + 1 == simplex.vertices.Count ? 0 : i + 1;

            Vector3 a = simplex.get(i);
            Vector3 b = simplex.get(j);

            Vector3 edge = b - a;

            // vector from edge toward origin
            Vector3 n = Vector3.Cross(Vector3.Cross(edge, a), edge);

            n.Normalize();

            double dist_origin_edge = Vector3.Dot(n, a);

            if (dist_origin_edge < closest_dist)
            {
                closest_dist = dist_origin_edge;
                closest_norm = n;
                closest_index = j;
            }

        }

        return (closest_dist, closest_norm, closest_index);
    }


    private Vector3 EPA_pd(Prism A, Prism B, Simplex simplex)
    {
        while (true)
        {
            (double dist, Vector3 norm, int index) = findClosestEdge(simplex);

            Vector3 p = support(A, B, norm);

            float d = Vector3.Dot(p, norm);

            if (d - dist < TOLERANCE)
            {
                return (norm * d);
            }

            else
            {
                simplex.insert(index, p);
            }
        }

    }

    private bool CheckCollision(PrismCollision collision)
    {
        // Task 1. Determine whether there is an actual collision using the GJK
        var prismA = collision.a;
        var prismB = collision.b;

        // GJK Collision
        // bool isCollide = true;

        // For each collision, create a simplex to calculate Minkowski sum
        Simplex simplex = new Simplex();

        // Oho Yeah it's working now!
        Simplex gjk_simp = GJK_collision(prismA, prismB, simplex);

        // Task 2. If there is, compute the penetration depth vector using EPA algorithm
        // EPA calculate penetration depth:
        var pd = Vector3.zero;
        if (gjk_simp != null)
        {
            pd = EPA_pd(prismA, prismB, gjk_simp);
        }
        collision.penetrationDepthVectorAB = pd;

        bool isCollision = pd == Vector3.zero ? false : true;

        return isCollision;
    }
    
    #endregion

    #region Private Functions

    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        prismObjA.transform.position += pushA;
        prismObjB.transform.position += pushB;

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }
    
    #endregion

    #region Visualization Functions

    private void DrawTree(float r = 0, float g = 0, float b = 1, float a = 1)
    {
        if (ktree == null) return;
        var wireFrameColor = new Color(r, g, b, a);
        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        foreach (KdTree.KdTreeNode<float, int> node in ktree.AsEnumerable())
        {
            var point = new Vector3(node.Point[0], 0, node.Point[1]);
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            if (node.LeftChild != null)
            {
                var pointC = new Vector3(node.LeftChild.Point[0], 0, node.LeftChild.Point[1]);
                Debug.DrawLine(point + Vector3.up * yMin, pointC + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(point + Vector3.up * yMax, pointC + Vector3.up * yMax, wireFrameColor);
            }
            if (node.RightChild != null)
            {
                var pointC = new Vector3(node.RightChild.Point[0], 0, node.RightChild.Point[1]);
                Debug.DrawLine(point + Vector3.up * yMin, pointC + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(point + Vector3.up * yMax, pointC + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    private void DrawShape(Vector3[] points, float r = 1, float g = 0, float b = 1, float a = 1)
    {
        var wireFrameColor = new Color(r, g, b, a);
        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();

        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    #endregion


    #region Utility Functions

    private Vector3[] BoundingBox(Vector3[] points)
    {
        Vector3[] corners = new Vector3[] { points[0], points[0] };
        for (int i = 1; i < points.Length; i++)
        {
            for (int b = 0; b < 3; b += 2)
            {
                if (corners[0][b] > points[i][b])
                    corners[0][b] = points[i][b];
                else if (corners[1][b] < points[i][b])
                    corners[1][b] = points[i][b];
            }
        }
        return corners;
    }

    private Vector3[] Corners2Points(Vector3[] points)
    {
        return new Vector3[] {
            points[0],
            points[0].x * Vector3.right + points[1].z * Vector3.forward,
            points[1],
            points[1].x * Vector3.right + points[0].z * Vector3.forward,
        };
    }

    private bool AreBoxesColliding(Vector3[] a, Vector3[] b)
    {
        // print(a[0]);
        // print(a[1]);
        // print(b[0]);
        // print(b[1]);
        return a[0].x < b[1].x && a[1].x > b[0].x && a[0].z < b[1].z && a[1].z > b[0].z;
    }

    private void DrawBoundingBoxes()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var bbox = BoundingBox(prism.points);
            DrawShape(Corners2Points(bbox));
        }
    }

    #endregion

    #region Utility Classes
    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
