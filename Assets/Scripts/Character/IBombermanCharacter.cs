using System;
using UnityEngine;
using UnityEngine.Events;

namespace BombermanRL.Character
{
    public interface IBombermanCharacter
    {
        Vector3 OffsetMovement { get; set; }
        int BombCount { get; set; }

        UnityEvent<Vector2> OnRequestMove {  get; set; } 
        UnityEvent OnRequestPlaceBomb { get; set; }

        void Move(Vector3 targetPos, bool canMove, Action onCompleteMove);
        void Dead();
    }
}
