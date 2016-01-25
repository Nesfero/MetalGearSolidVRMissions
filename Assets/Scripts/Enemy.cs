﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EnemyState {
    PATROL,
    SEE_PLAYER,
    INVESTIGATE
};

public class Enemy : MonoBehaviour {
    public float speed = 10f;
    public float detect_range = 5f; // Max detection range
    public float detect_angle = 30f;

    public float detect_delay = 0.5f;
    public float undetect_delay = 1f;
    List<GameObject> search_path;
    List<GameObject> possible_path;
    public Color default_color;
    public Color surprised_color = Color.yellow;
    public GameObject waypoint_prefab;
    public GameObject current_patrol_point;
    public GameObject investigate_point;
    GameObject current_point;

    bool ____________________;  // Divider for the inspector

    public EnemyState current_state;

    Rigidbody body;
    NavMeshAgent mesh_agent;
    LineRenderer detect_area_parimeter;
    int detect_parimeter_arc_vertices = 10;
    int detect_parimeter_total_vertices = 12;

    // Use this for initialization
    void Start() {
        body = gameObject.GetComponent<Rigidbody>();
        mesh_agent = gameObject.GetComponent<NavMeshAgent>();
        default_color = this.GetComponent<Renderer>().material.color;

        detect_area_parimeter = gameObject.GetComponent<LineRenderer>();
        detect_area_parimeter.SetVertexCount(detect_parimeter_total_vertices);

        current_state = EnemyState.PATROL;
        search_path=new List<GameObject>();
        possible_path=new List<GameObject>();
    }

    // Update is called once per frame
    void Update() {
        drawDetectParimeter();
        if (current_state == EnemyState.PATROL) {
            patrolUpdate();
        }
        else if (current_state == EnemyState.SEE_PLAYER) {
            seePlayerUpdate();
        }
    }

    void patrolUpdate() {
        // Move along patrol route
        mesh_agent.destination = current_patrol_point.transform.position;

        // Check if player can now be seen
        if (playerInFieldOfView()) {
            Invoke("detectPlayer", detect_delay);
        }
    }

    void seePlayerUpdate() {
        // Stop patrol
        mesh_agent.destination = this.transform.position;

        if (!playerInFieldOfView()) {
            Invoke("undetectPlayer", undetect_delay);
        }
    }

    bool playerInFieldOfView() {
        RaycastHit see_player;
        Vector3 to_player = MovementController.player.transform.position - transform.position;

        Physics.Raycast(this.transform.position, to_player, out see_player);
        if (Vector3.Angle(this.transform.forward, to_player) < detect_angle &&
                see_player.collider.gameObject.tag == "Player" && see_player.distance < detect_range) {
            Debug.DrawRay(this.transform.position, to_player);
            return true;
        }
        return false;
    }

    void drawDetectParimeter() {
        int cur_vertex = 0;
        float current_angle = -detect_angle;
        float angle_delta = (detect_angle * 2) / (detect_parimeter_arc_vertices - 1);
        bool hit;
        Vector3 arc_offset;
        Vector3 arc_vertex;
        RaycastHit ray_hit;

        detect_area_parimeter.SetPosition(cur_vertex++, gameObject.transform.position);
        for (int i = 0; i < detect_parimeter_arc_vertices; ++i) {
            arc_offset = Quaternion.Euler(0, current_angle, 0) * gameObject.transform.forward * detect_range;
            hit = Physics.Raycast(this.transform.position, arc_offset, out ray_hit, detect_range);
            if (hit && ray_hit.collider.gameObject.tag == "Obstacle") {
                arc_vertex = ray_hit.point;
            }
            else {
                arc_vertex = gameObject.transform.position + arc_offset;
            }

            detect_area_parimeter.SetPosition(cur_vertex++, arc_vertex);
            current_angle += angle_delta;
        }
        detect_area_parimeter.SetPosition(cur_vertex, gameObject.transform.position);
    }

    void detectPlayer() {
        if (playerInFieldOfView()) {
            current_state = EnemyState.SEE_PLAYER;
            this.GetComponent<Renderer>().material.color = surprised_color;
        }
    }

    void undetectPlayer() {
        if (!playerInFieldOfView()) {
            current_state = EnemyState.PATROL;
            this.GetComponent<Renderer>().material.color = default_color;
        }
    }

    public void setPatrolPoint(GameObject new_patrol_point) {
        if(current_state == EnemyState.PATROL)
        current_patrol_point = new_patrol_point;
        else if(current_state == EnemyState.INVESTIGATE) {
            GameObject next_point=search_path.FirstOrDefault();
            if (new_patrol_point == next_point) {
                search_path.Remove(next_point);
                current_patrol_point = search_path.FirstOrDefault();
            }
        }
    }
    

    
    public void investigate(GameObject new_point) {
        current_state=EnemyState.INVESTIGATE;
        investigate_point=new_point;
        current_point = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
        float shortest = 9999999;
        float current_dist=0;
        possible_path.Clear();
        findRoute(current_point, ref shortest, ref current_dist);

    }

    void findRoute(GameObject current_point, ref float shortest, ref float current_dist) {
        possible_path.Add(current_point);
        if (current_point==investigate_point) {
            shortest =current_dist;
            search_path=possible_path;
            return;
        }
        foreach (Neighbor next in current_point.GetComponent<PatrolPoint>().neighbors) {
            
            current_dist+=next.distance;
            if (current_dist<shortest) {
                if(!current_point.GetComponent<PatrolPoint>().neighbors.Contains(next))
                findRoute(next.point, ref shortest, ref current_dist);
            }
            current_dist-=next.distance;
            
        }
        possible_path.Remove(current_point);

    }

    
}
