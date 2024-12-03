using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class Spawner : MonoBehaviour
{
    private List<SpawnPoint> spawnPointList;
    private List<Character> spawnedCharacters;
    private bool hasSpawned;
    public Collider _collider;
    public UnityEvent onAllSpawnedCharacterEliminated;

    private void Awake(){
        var spawnPointArray = transform.parent.GetComponentsInChildren<SpawnPoint>();
        spawnPointList = new List<SpawnPoint>(spawnPointArray);
        spawnedCharacters = new List<Character>();


    }

    private void Update(){
        if(!hasSpawned || spawnedCharacters.Count == 0){
            return;
        }
        
        bool allSpawnedAreDead = true;

        foreach(Character c in spawnedCharacters){
            if(c.CurrentState != Character.CharacterState.Dead){
                allSpawnedAreDead = false;
                break;
            }
        }

        if(allSpawnedAreDead){
            if(onAllSpawnedCharacterEliminated != null){
                    onAllSpawnedCharacterEliminated.Invoke();
            }

            spawnedCharacters.Clear();
        }
    }

    public void SpawnCharacter(){
        if(hasSpawned){
            return;
        }

        hasSpawned = true;


        foreach (SpawnPoint point in spawnPointList){
            if (point.EnemyToSpawn != null){
                GameObject spawmedGameObject = Instantiate(point.EnemyToSpawn, point.transform.position, point.transform.rotation);
                spawnedCharacters.Add(spawmedGameObject.GetComponent<Character>());
            }
        }


    }


    private void OnTriggerEnter(Collider other){
        if(other.tag == "Player"){
            SpawnCharacter();
        }
    }

    private void OnDrawGizmos(){
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, _collider.bounds.size);

    }


}
