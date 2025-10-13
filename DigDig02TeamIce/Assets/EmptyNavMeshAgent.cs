using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EmptyNavMeshAgent : MonoBehaviour
{
    private NavMeshAgent NavAgent;
    [SerializeField] private Transform target;

    private void Awake()
    {
        NavAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        NavAgent.destination = target.position;
    }
}
