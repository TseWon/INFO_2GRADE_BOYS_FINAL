using UnityEngine;

namespace FacilityEscape
{
    public enum EscapeControl { ColorButton, DialClockwise, DialCounterclockwise, ConfirmLever }

    [DisallowMultipleComponent]
    public sealed class MultiEscapeInteractable : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private EscapeControl control;
        [SerializeField] private LockColor color;
        [SerializeField, Range(0, 2)] private int dialIndex;

        // 각 장치 또는 자식에 비 Trigger Collider를 추가하세요.
        public void Interact(MultiEscapePlayer player)
        {
            if (!isActiveAndEnabled || game == null || !game.IsOperator(player)) return;
            switch (control)
            {
                case EscapeControl.ColorButton: game.PressColor(player, color); break;
                case EscapeControl.DialClockwise: game.RotateDial(player, dialIndex, 1); break;
                case EscapeControl.DialCounterclockwise: game.RotateDial(player, dialIndex, -1); break;
                case EscapeControl.ConfirmLever: game.PullLever(player); break;
            }
        }
    }
}
