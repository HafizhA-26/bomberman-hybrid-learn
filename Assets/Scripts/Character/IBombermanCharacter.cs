using System;
using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL.Character
{
    public interface IBombermanCharacter
    {
        int BombLimit { get; set; }
        int BombCount { get; set; }
        Vector3 OffsetMovement { get; set; }
        UnityEvent<Vector2> OnRequestMove {  get; set; } 
        UnityEvent OnRequestPlaceBomb { get; set; }

        void Move(Vector3 targetPos, bool canMove, Action onCompleteMove);
        void Dead();
    }
}
