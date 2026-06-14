using UnityEngine;

namespace Cherry.Puzzles
{
    [AddComponentMenu("Cherry/Puzzles/Door Mover")]
    [DisallowMultipleComponent]
    public class DoorMover : MonoBehaviour
    {
        [Header("Door Movement")]
        [SerializeField] private Transform door;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0f, 2f, 0f);
        [SerializeField] private float moveSpeed = 3f;

        private Vector3 _closedLocalPos;
        private Vector3 _openLocalPos;
        private bool _isOpen;

        private void Awake()
        {
            if (door == null) door = transform;
            _closedLocalPos = door.localPosition;
            _openLocalPos = _closedLocalPos + openLocalOffset;
        }

        private void Update()
        {
            Vector3 target = _isOpen ? _openLocalPos : _closedLocalPos;
            door.localPosition = Vector3.MoveTowards(door.localPosition, target, moveSpeed * Time.deltaTime);
        }

        public void Open() => _isOpen = true;
        public void Close() => _isOpen = false;
        public void SetOpen(bool open) => _isOpen = open;
        public void Toggle() => _isOpen = !_isOpen;
    }
}