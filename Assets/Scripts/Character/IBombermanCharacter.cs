using BombermanRL.Props;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL.Character
{
    public interface IBombermanCharacter
    {
        string Name { get; }
        CharacterType Type { get; }
        int BombLimit { get; set; }
        int BombCount { get; set; }
        Vector3 OffsetMovement { get; set; }
        UnityEvent<Vector2> OnRequestMove {  get; set; } 
        UnityEvent OnRequestPlaceBomb { get; set; }

        void Move(Vector3 targetPos, bool canMove, Action onTileChanged);
        void Dead();
        void Kill(IBombermanCharacter character);
        void DestroyProps(IDestroyableProps prop);
    }
}
