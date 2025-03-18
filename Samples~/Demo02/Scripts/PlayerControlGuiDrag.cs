using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

namespace Game
{
    [RequireComponent(typeof(RectTransform))]
    public class PlayerControlGuiDrag : OnScreenControl, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Title("Config")]
        [SerializeField] private string _controlPath;
        [SerializeField] private float _multiple;

        private Vector2 _inputValue;

        private bool _inputValueChanged = false;

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        private void Update()
        {
            if (!_inputValueChanged)
                return;

            SendValueToControl(_inputValue);

            if (_inputValue != Vector2.zero)
                _inputValue = Vector2.zero;
            else
                _inputValueChanged = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            Drag(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            Drag(eventData);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            _inputValue = Vector2.zero;

            _inputValueChanged = true;
        }

        private void Drag(PointerEventData eventData)
        {
            _inputValue = (eventData.delta / Screen.dpi) * _multiple;

            _inputValueChanged = true;
        }
    }
}