## README

Collision Scene.

## PotentialCollisions()


- [x] Borrow KdTree and implemented [Daniel]

  (by yanshi) Note: on my computer I have to copy the `KdTree\KdTreeLib` into `Assert`  to make things works. 

- [ ] Draw debug lines

- [ ] Efficiency report

### CheckCollision()

- [x] GJK algorithm [Yanshi]
  - [x] Ref: http://www.dyn4j.org/2010/04/gjk-gilbert-johnson-keerthi/
  - [x] GJK_collision
    - [x] createSimplex
    - [x] support
    - [x] getFarthestPointInDirection
    - [x] containsOrigin
  - [x] Test
  - [ ] Some further test like visualization and  adding debug line, etc.
  - [ ] Some 3D related stuffs not solved yet.
- [ ] penetration depth vector, EPA algorithm
  - [ ] Ref: http://www.dyn4j.org/2010/05/epa-expanding-polytope-algorithm/





### Extra credit

a. Scratch implement a data structures



b. Borrow KdTree



c. 3D collision detection implementation



d. 3D GJK, EPA implementation

