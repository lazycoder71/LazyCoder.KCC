using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace Game
{
    [RequireComponent(typeof(RectTransform))]
    public class PlayerControlGuiStick : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Title("Reference")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;

        [Title("Config")]
        [SerializeField] private float _range = 50f;
        [SerializeField] private string _controlPath;

        private Vector2 _originPos;
        private Canvas _parentCanvas;
        private RectTransform _rectTransform;

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            _originPos = _background.localPosition;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            // Activate and position the joystick
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                eventData.position,
                _parentCanvas.worldCamera,
                out Vector2 localPoint);

            _background.localPosition = localPoint;

            Drag(eventData); // Update handle position immediately
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            Drag(eventData);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            // Reset state
            _handle.localPosition = Vector2.zero;

            _background.localPosition = _originPos;

            SendValueToControl(Vector2.zero);
        }

        private void Drag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                _parentCanvas.worldCamera,
                out Vector2 localPoint);

            // Calculate delta from center
            Vector2 delta = localPoint;
            delta = Vector2.ClampMagnitude(delta, _range);

            // Update handle position
            _handle.localPosition = delta;

            // Send normalized input
            Vector2 input = delta / _range;

            SendValueToControl(input);
        }
    }
}