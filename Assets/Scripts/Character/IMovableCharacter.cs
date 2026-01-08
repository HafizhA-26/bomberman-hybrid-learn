using System;
using UnityEngine;

namespace BombermanRL.Character
{
    public interface IMovableCharacter
    {
        Vector3 OffsetMovement { get; set; }

        void Move(Vector3 targetPos, bool canMove, Action onCompleteMove);
    }
}
