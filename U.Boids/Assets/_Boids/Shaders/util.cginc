const float3x3 IDENTIY3x3 = float3x3(float3(1, 0, 0), float3(0, 1, 0), float3(0, 0, 1));

// Generates the rotation matrix of current boid with the
// forward direction being the positive z_axis
// 'eye' will be current boid pos
// 'at' will be boid to look at pos
// 'up' will be the up direction
float3x3 look_at(float3 eye, float3 target, float3 up)
{
    // forward direction
    float3 z_axis = normalize(target - eye);
    
    // orthogonal to the plane created by z_axis and up
    float3 x_axis = cross(z_axis, up);
    // set default value of x_axis if z_axis = s * up
    if (x_axis.x == 0 && x_axis.y == 0 && x_axis.z == 0)
        x_axis = float3(1, 0, 0);
    else
        x_axis = normalize(x_axis);
    
    // orthogonal to the plane created by x_axis and z_axis
    float3 y_axis = cross(z_axis, x_axis);
    
    // rot matrix with axes as columns [ x y z ]
    return float3x3(
        float3(x_axis.x, y_axis.x, z_axis.x),
        float3(x_axis.y, y_axis.y, z_axis.y),
        float3(x_axis.z, y_axis.z, z_axis.z)
    );
}

// THIS IMPLEMENTATION IS SORTA JANK BUT WORKS FOR NOW
// Slerp linearly interpolates between two quaternion values
// when t=0, the function returns q1
// when t=1, the function returns q2
// where 0 <= t <= 1
// For more information on the computations go to:
// https://www.euclideanspace.com/maths/algebra/realNormedAlgebra/quaternions/slerp/index.htm
float4 slerp(float4 q1, float4 q2, float t)
{
    float4 q = float4(0,0,0,1);
    t = clamp(t, 0, 1);
    
    // calculate the angle between the quaternions
    float cos_half_theta = q1.x * q2.x + q1.y * q2.y + q1.z * q2.z + q1.w * q2.w;
   
    // to force quaternion positive since q = -q
    if (cos_half_theta < 0)
    {
        q2.x *= -1;
        q2.y *= -1;
        q2.z *= -1;
        q2.w *= -1;
        cos_half_theta *= -1;
    }
    
    // if q1=q2 or q1=-q2 then theta = 0 and we can return q1
    if (abs(cos_half_theta) >= 1.0)
    {
        q.x = q1.x;
        q.y = q1.y;
        q.z = q1.z;
        q.w = q1.w;
        return q;
    }
    
    // calculate tempory values
    float half_theta = acos(cos_half_theta);
    float sin_half_theta = sqrt(1.0 - cos_half_theta * cos_half_theta);
    
    // if theta = 180 degrees then result is not fully defined
    // we could rotate around any axis normal to q1 or q2
    if (abs(sin_half_theta) < 0.001)
    {
        q.x = (q1.x * 0.5 + q2.x * 0.5);
        q.y = (q1.y * 0.5 + q2.y * 0.5);
        q.z = (q1.z * 0.5 + q2.z * 0.5);
        q.w = (q1.w * 0.5 + q2.w * 0.5);
        return q;
    }
    
    float ratio_a = sin((1 - t) * half_theta) / sin_half_theta;
    float ratio_b = sin(t * half_theta) / sin_half_theta;
    
    // calculate quaternion
    q.x = (q1.x * ratio_a + q2.x * ratio_b);
    q.y = (q1.y * ratio_a + q2.y * ratio_b);
    q.z = (q1.z * ratio_a + q2.z * ratio_b);
    q.w = (q1.w * ratio_a + q2.w * ratio_b);
    return q;
}

// Converts the rotation matrix 'm' to a quaternion float4
// where 'm' is a special orthogonal matrix
// For more information on the computations go to:
// https://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/
float4 matrix_to_quaternion(float3x3 m)
{
    // POTENTIAL ERROR: sqrt(*negative value*) OR sqrt() = 0
    float4 q;
    
    // trace is the sum of the diagonal entries
    float trace = m[0][0] + m[1][1] + m[2][2];
    float s;

    if (trace > 0)
    {
        s = sqrt(trace + 1.0) * 2;
        q.x = (m[2][1] - m[1][2]) / s;
        q.y = (m[0][2] - m[2][0]) / s;
        q.z = (m[1][0] - m[0][1]) / s;
        q.w = 0.25 * s;
    }
    else if ((m[0][0] > m[1][1]) && (m[0][0] > m[2][2]))
    {
        s = sqrt(1.0 + m[0][0] - m[1][1] - m[2][2]) * 2;
        q.x = 0.25 * s;
        q.y = (m[0][1] + m[1][0]) / s;
        q.z = (m[0][2] + m[2][0]) / s;
        q.w = (m[2][1] - m[1][2]) / s;
    }
    else if (m[1][1] > m[2][2])
    {
        s = sqrt(1.0 + m[1][1] - m[0][0] - m[2][2]) * 2;
        q.x = (m[0][1] + m[1][0]) / s;
        q.y = 0.25 * s;
        q.z = (m[1][2] + m[2][1]) / s;
        q.w = (m[0][2] - m[2][0]) / s;
    }
    else
    {
        s = sqrt(1.0 + m[2][2] - m[0][0] - m[1][1]) * 2;
        q.x = (m[0][2] + m[2][0]) / s;
        q.y = (m[1][2] + m[2][1]) / s;
        q.z = 0.25 * s;
        q.w = (m[1][0] - m[0][1]) / s;
    }

    return q;
}

// Converts a quaternion to the corresponding rotation matrix
// where 'q' is the quaternion
// For more information on the computations go to:
// https://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToMatrix/index.htm
float3x3 quaternion_to_matrix(float4 q)
{
    float3 x_axis = float3(
                        1 - 2 * q.y * q.y - 2 * q.z * q.z,
                        2 * q.x * q.y + 2 * q.z * q.w,
                        2 * q.x * q.z - 2 * q.y * q.w
                    );
    
    float3 y_axis = float3(
                        2 * q.x * q.y - 2 * q.z * q.w,
                        1 - 2 * q.x * q.x - 2 * q.z * q.z,
                        2 * q.y * q.z + 2 * q.x * q.w
                    );
    
     float3 z_axis = float3(
                        2 * q.x * q.z + 2 * q.y * q.w,
                        2 * q.y * q.z - 2 * q.x * q.w,
                        1 - 2 * q.x * q.x - 2 * q.y * q.y
                    );
    
    // rot matrix with axes as columns [ x y z ]
    return float3x3(
        float3(x_axis.x, y_axis.x, z_axis.x),
        float3(x_axis.y, y_axis.y, z_axis.y),
        float3(x_axis.z, y_axis.z, z_axis.z)
    );
}

// Returns the average quaterion from quaterions[]
// where count is the lenght of the list
// For more information on the computations go to:
// https://math.stackexchange.com/questions/61146/averaging-quaternions/3435296#3435296
float4 quaternion_avg(float4 quaternions[3])
{
    float4 avg = float4(0, 0, 0, 0);
    
    for (int i = 0; i < 3; i++)
    {
        float4 q = quaternions[i];
        float weight = 1.0;
        
        // Correct for double cover
        if (i > 0 && dot(quaternions[i], quaternions[0]) < 0.0)
        {
            weight = -weight;
        }
        
        avg.x += q.x;
        avg.y += q.y;
        avg.z += q.z;
        avg.w += q.w;
    }

    return normalize(avg);
}

// Rotates a quaternion curr towards target by at most
// max_radians. Quaternions curr, target must be normalized.
float4 rotate_towards(float4 curr, float4 target, float max_radians)
{
    // dot product between two quaternions
    float angle = dot(curr, target);
    float t = clamp(max_radians / angle, 0.0, 1.0);
    return slerp(curr, target, t);
}

// for silly little program
bool in_collision(float3 pos)
{
    return pos.x > 45.0 || pos.x < -45.0
        || pos.y > 45.0 || pos.y < -45.0
        || pos.z > 45.0 || pos.z < -45.0;
}
