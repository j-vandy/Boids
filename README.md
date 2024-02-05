# Boids
Bird-oid objects, also known as boids, were originally created by Craig Reynolds in 1986. Boids follow a set of rules to simulate complex and realistic-looking flocking behaviors seen in animals such as birds and fish.  The three primary rules that boids follow are:

Separation: maintaining a minimum distance from their neighboring boids
Alignment: matching the average direction of their neighboring boids
Cohesion: moving towards the average position of their neighbors

The positional and rotational data of each boid are computed via a compute shader to improve performance. This implementation was accomplished through the utilization of the Unity Game Engine.This interactive grass-rendering demo uses a shell-based fur rendering technique called shell texturing.