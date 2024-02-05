using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TableController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoidController boidController;
    [SerializeField] private Slider count;
    [SerializeField] private Slider viewing_dist;
    [SerializeField] private Slider avoidance;
    [SerializeField] private Slider alignment;
    [SerializeField] private Slider cohesion;

    // Start is called before the first frame update
    void Start()
    {
        count.value = boidController.GetCount();
        viewing_dist.value = boidController.GetViewingDistance();
        avoidance.value = boidController.GetAvoidance();
        alignment.value = boidController.GetAlignment();
        cohesion.value = boidController.GetCohesion();
    }

    public void OnValueChanged()
    {
        boidController.SetCount((int)count.value);
        boidController.SetViewingDistance(viewing_dist.value);
        boidController.SetAvoidance(avoidance.value);
        boidController.SetAlignment(alignment.value);
        boidController.SetCohesion(cohesion.value);
    }
}
