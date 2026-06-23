using System.Collections;
using UnityEngine;

namespace BombermanRL
{
    [CreateAssetMenu(fileName = "GameModeConfig", menuName = "Bomberman/GameModeConfig")]
    public class GameModeConfig : ScriptableObject
    {
        [SerializeField] public PlayMode GamePlayMode;

        [Header("Character Prefab")]
        [SerializeField] public GameObject PlayerPrefab;
        [SerializeField] public GameObject EnemyPrefab;

        [Header("Movement Offset")]
        [SerializeField] public Vector3 PlayerOffset;
        [SerializeField] public Vector3 EnemyOffset;
    }
}