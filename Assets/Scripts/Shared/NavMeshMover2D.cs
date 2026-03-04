using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wrapper okolo NavMeshAgent pre 2D top-down pohyb.
/// Konfiguruje agenta pre XY rovinu a poskytuje jednoduché API.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshMover2D : MonoBehaviour
{
    private NavMeshAgent agent;

    /// <summary>Rýchlosť agenta (nastaviteľná zvonku).</summary>
    public float Speed
    {
        get => agent.speed;
        set => agent.speed = value;
    }

    /// <summary>Stopping distance agenta.</summary>
    public float StoppingDistance
    {
        get => agent.stoppingDistance;
        set => agent.stoppingDistance = value;
    }

    /// <summary>Aktuálna velocity agenta ako Vector2 (XY).</summary>
    public Vector2 Velocity => new Vector2(agent.velocity.x, agent.velocity.y);

    /// <summary>Vráti true ak agent dorazil do cieľa (alebo nemá cestu).</summary>
    public bool HasReachedDestination
    {
        get
        {
            if (!agent.enabled || !agent.isOnNavMesh) return true;
            if (agent.pathPending) return false;
            return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
        }
    }

    /// <summary>Vráti true ak agent práve hľadá cestu.</summary>
    public bool IsPathPending => agent.pathPending;

    /// <summary>Vráti true ak je agent aktívny a na NavMeshi.</summary>
    public bool IsReady => agent.enabled && agent.isOnNavMesh;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Kritické pre 2D – agent sa neotáča a neupravuje os
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    /// <summary>Nastav cieľovú pozíciu. Agent automaticky nájde cestu cez NavMesh.</summary>
    public void SetDestination(Vector2 target)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.SetDestination(new Vector3(target.x, target.y, transform.position.z));
    }

    /// <summary>Zastav agenta na mieste.</summary>
    public void Stop()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    /// <summary>Zapne/vypne NavMeshAgent. Pri zapnutí warpne agenta na najbližší bod NavMeshu.</summary>
    public void SetEnabled(bool enabled)
    {
        if (enabled && !agent.enabled)
        {
            // Najprv nájdi najbližší bod na NavMesh
            Vector3 pos = transform.position;
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                // Posuň objekt na platný NavMesh bod ešte pred zapnutím agenta
                transform.position = hit.position;
                agent.enabled = true;
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[NavMeshMover2D] No NavMesh found within 5 units of {pos}. Agent not enabled.");
            }
        }
        else
        {
            agent.enabled = enabled;
        }
    }

    /// <summary>Warpne agenta na pozíciu (napr. po teleporte, spawn).</summary>
    public void Warp(Vector2 position)
    {
        if (agent.enabled)
            agent.Warp(new Vector3(position.x, position.y, transform.position.z));
        else
            transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}


