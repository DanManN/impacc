## README

Using original version of Collision Scene code

## 2. Git Log

See log.txt

## 4. PotentialCollisions()

- k-d tree implementation taken from https://github.com:codeandcats/KdTree.git

- Since insertion into k-d tree takes average O(logN) per point and we insert N
  points, the time to build the k-d tree for all points is average O(NlogN).
  Then we do queries for nearest neighbors stopping once we leave a circle of
  radius the diagonal of the bounding box of the polygon. Each such nearest
  neighbor query takes average O(slogN) time for s found neighbors. The
  expectation is that s is small compared to N and can be treated as a
  constant. So when we do N such queries the query time is expected O(NlogN).
  Thus building and querying for potential collisions takes expected O(NlogN)
  time in total.

### 5. CheckCollision()

- [x] GJK algorithm [Yanshi]
  - [x] Ref: http://www.dyn4j.org/2010/04/gjk-gilbert-johnson-keerthi/
  - [x] GJK_collision
    - [x] createSimplex
      - [x] support class Simplex
    - [x] support (Get a direction to get vertex from Minkowski Sum)
    - [x] getFarthestPointInDirection
    - [x] containsOrigin
- [x] EPA algorithm [Ashley]
  - [x] http://www.dyn4j.org/2010/05/epa-expanding-polytope-algorithm/
  - [x] penetration depth vector
  - [x] find closest edge


### Extra credit

b. Borrow KdTree
