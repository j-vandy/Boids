using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using NaughtyAttributes;

public struct Boid
{
    public Vector3 position;
    public Vector4 rotation;

    public Quaternion getRotation() => new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
}

public class BoidController : MonoBehaviour
{
    // exposed
    [Header("References")]
    [SerializeField] private GameObject boidPrefab;
    [SerializeField] private ComputeShader computeShader;
    [Header("Settings")]
    [SerializeField] private int count = 25;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float rot_speed = 180f;
    [SerializeField] private float viewing_dist = 10f;
    [Range(0,1)]
    [SerializeField] private float avoid_dist = 0.33f;
    [Range(0,1)]
    [SerializeField] private float avoidance_factor = 1f;
    [Range(0,1)]
    [SerializeField] private float alignment_factor = 1f;
    [Range(0,1)]
    [SerializeField] private float cohesion_factor = 1f;
    // instantiate boid
    private List<GameObject> objects = new List<GameObject>();
    private Boid[] data = new Boid[0];
    private int index = 0;
    private float delta_time = 0f;
    // dispatch compute shader
    private int stride_size = sizeof(float) * 7;
    private bool instantiated = false;
    private ComputeBuffer boidsBuffer;
    private int kernel_index = 0;
    private int threadGroupsX = 1;

    private void Start()
    {
        GenerateBoids();
    }

    private void Update()
    {
        delta_time = Time.deltaTime;
        DispatchComputeShader();
    }

    private void OnDestroy()
    {
        ClearBuffer();
    }

    private Boid[] CopyArray(Boid[] b, int size)
    {
        Boid[] o = new Boid[size];
        for (int i = 0; i < Mathf.Min(b.Length, size); i++)
        {
            o[i] = b[i];
        }
        return o;
    }

    // size must be l.Count
    private List<GameObject> ClampList(List<GameObject> l, int size)
    {
        List<GameObject> o = new List<GameObject>();
        for (int i = 0; i < size; i++)
        {
            o.Add(l[i]);
        }
        return o;
    }

    private void ClearBuffer()
    {
        // destroy buffer
        if (boidsBuffer != null)
            boidsBuffer.Dispose();
        instantiated = false;
    }

    private void InstantiateBoid(float pos_x = 0, float pos_y = 0, float pos_z = 0, float q_x = 0, float q_y = 0, float q_z = 0, float q_w = 0)
    {
        Vector3 pos = new Vector3(pos_x, pos_y, pos_z);
        Quaternion rot = new Quaternion(q_x, q_y, q_z, q_w);
        GameObject obj = Instantiate(boidPrefab, pos, rot);
        objects.Add(obj);

        // generate boid struct
        Boid boid = new Boid();
        boid.position = pos;
        boid.rotation = new Vector4(rot.x, rot.y, rot.z, rot.w);
        data[index] = boid;

        // increment index
        index++;
    }

    private void GenerateBoids()
    {
        // destroy boids buffer
        ClearBuffer();

        // copy and resize data
        data = CopyArray(data, count);

        if (objects.Count > count)
        {
            // destroy gameobjects
            for (int i = objects.Count - 1; i > count - 1; i--)
            {
                GameObject go = objects[i];
                Destroy(go);
            }
            
            // copy and resize objects
            objects = ClampList(objects, count);
        }
        else
        {
            // instantiate boids
            for (int i = objects.Count; i < count; i++)
            {
                Quaternion q = UnityEngine.Random.rotation;
                InstantiateBoid(UnityEngine.Random.Range(-45, 45),
                        UnityEngine.Random.Range(-45, 45),
                        UnityEngine.Random.Range(-45, 45),
                        q.x, q.y, q.z, q.w);
            }
        }

        // reset index
        index = count;
    }

    private void DispatchComputeShader()
    {
        if (!instantiated)
        {
            // create boid buffer to send to GPU
            boidsBuffer = new ComputeBuffer(data.Length, stride_size);
            boidsBuffer.SetData(data);

            // get index of kernel 'main'
            kernel_index = computeShader.FindKernel("main");

            // write to GPU buffer and set shader variables
            computeShader.SetBuffer(kernel_index, "boids", boidsBuffer);
            computeShader.SetInt("boid_count", boidsBuffer.count);
            computeShader.SetFloat("boid_speed", speed);
            computeShader.SetFloat("boid_rot_speed", rot_speed * Mathf.Deg2Rad);
            computeShader.SetFloat("viewing_dist", viewing_dist);
            computeShader.SetFloat("avoid_dist", viewing_dist * avoid_dist);
            computeShader.SetFloat("avoidance_factor", avoidance_factor);
            computeShader.SetFloat("alignment_factor", alignment_factor);
            computeShader.SetFloat("cohesion_factor", cohesion_factor);

            // Set thread group size
            threadGroupsX = data.Length / 64;
            if (data.Length % 64 != 0)
                threadGroupsX++;
           
            instantiated = true;
        }

        // set delta time variable
        computeShader.SetFloat("delta_time", delta_time);

        // dispatch
        computeShader.Dispatch( kernel_index, threadGroupsX, 1, 1 );

        // read buffer and set to data
        boidsBuffer.GetData(data);

        // update objects rotation and position
        for( int i = 0; i < objects.Count; i++ )
        {
            GameObject obj = objects[i];
            Boid boid = data[i];
            
            obj.transform.position = boid.position;
            obj.transform.rotation = boid.getRotation();
        }
    }

    // getter & setter methods
    public int GetCount() { return count; }
    public void SetCount(int count)
    {
        count = Mathf.Clamp(count, 1, 3000);
        this.count = count;
        GenerateBoids();
    }

    public float GetViewingDistance() { return viewing_dist; }
    public void SetViewingDistance(float viewing_dist)
    {
        viewing_dist = Mathf.Clamp(viewing_dist, 5.0f, 20.0f);
        this.viewing_dist = viewing_dist;
        instantiated = false;
    }

    public float GetAvoidance() { return avoidance_factor; }
    public void SetAvoidance(float avoidance_factor)
    {
        avoidance_factor = Mathf.Clamp01(avoidance_factor);
        this.avoidance_factor = avoidance_factor;
        instantiated = false;
    }

    public float GetAlignment() { return alignment_factor; }
    public void SetAlignment(float alignment_factor)
    {
        alignment_factor = Mathf.Clamp01(alignment_factor);
        this.alignment_factor = alignment_factor;
        instantiated = false;
    }

    public float GetCohesion() { return cohesion_factor; }
    public void SetCohesion(float cohesion_factor)
    {
        cohesion_factor = Mathf.Clamp01(cohesion_factor);
        this.cohesion_factor = cohesion_factor;
        instantiated = false;
    }
}