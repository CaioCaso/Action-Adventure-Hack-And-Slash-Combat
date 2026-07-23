using UnityEngine;
using UnityEngine.InputSystem;

namespace HYPERINFINITYPRODUCTIONS.IsekaiRift
{
    [AddComponentMenu("HyperInfinityProductions/Isekai Rift/Player/Player Input Manager")]
    public class PlayerInputManager : MonoBehaviour
    {
        public InputActionAsset actions;

        protected InputAction m_movement;
        protected InputAction m_run;
        protected InputAction m_jump;
        protected InputAction m_look;
        protected InputAction m_LockOn;
        protected InputAction m_stopLockOn;
        protected InputAction m_lightAttack;
        protected InputAction m_heavyAttack;
        protected InputAction m_dodge;

        protected Camera m_camera;

        protected float m_movementDirectionUnlockTime;
        protected float? m_lastJumpTime;

        protected const string k_mouseDeviceName = "Mouse";
        protected const float k_jumpBuffer = 0.15f;

        protected virtual void CacheActions()
        {
            m_movement = actions["Movement"];
            m_run = actions["Run"];
            m_jump = actions["Jump"];
            m_look = actions["Look"];
            m_LockOn = actions["Lock On"];
            m_stopLockOn = actions["Stop Lock On"];
            m_lightAttack = actions["LightAttack"];
            m_heavyAttack = actions["HeavyAttack"];
            m_dodge = actions["Dodge"];
        }

        public virtual Vector3 GetMovementDirection()
        {
            if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;

            var value = m_movement.ReadValue<Vector2>();
            return GetAxisWithCrossDeadZone(value);
        }

        public virtual Vector3 GetLookDirection()
        {
            var value = m_look.ReadValue<Vector2>();

            if (IsLookingWithMouse())
            {
                return new Vector3(value.x, 0, value.y);
            }

            return GetAxisWithCrossDeadZone(value);
        }

        public virtual Vector3 GetMovementCameraDirection(bool localSpace = true)
        {
            var direction = GetMovementDirection();

            if (direction.sqrMagnitude > 0)
            {
                var rotation = Quaternion.FromToRotation(m_camera.transform.up, transform.up);
                direction = rotation * m_camera.transform.rotation * direction;

                if (localSpace)
                {
                    direction = Vector3.ProjectOnPlane(direction, transform.up);
                    direction = Quaternion.FromToRotation(transform.up, Vector3.up) * direction;
                }

                direction = direction.normalized;
            }

            return direction;
        }

        public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis)
        {
            var deadzone = InputSystem.settings.defaultDeadzoneMin;
            axis.x = Mathf.Abs(axis.x) > deadzone ? RemapToDeadzone(axis.x, deadzone) : 0;
            axis.y = Mathf.Abs(axis.y) > deadzone ? RemapToDeadzone(axis.y, deadzone) : 0;
            return new Vector3(axis.x, 0, axis.y);
        }

        public virtual bool IsLookingWithMouse()
        {
            if (m_look.activeControl == null)
            {
                return false;
            }

            return m_look.activeControl.device.name.Equals(k_mouseDeviceName);
        }

        public virtual bool GetRun() => m_run.WasPressedThisFrame();
        public virtual bool GetRunUp() => m_run.WasReleasedThisFrame();

        public virtual bool GetJumpDown()
        {
            if (m_lastJumpTime != null &&
                Time.time - m_lastJumpTime < k_jumpBuffer)
            {
                m_lastJumpTime = null;
                return true;
            }

            return false;
        }

        public virtual bool GetJumpUp() => m_jump.WasReleasedThisFrame();
        public virtual bool GetDodgeDown() => m_dodge.WasPressedThisFrame();
        public virtual bool GetLockedOn() => m_LockOn.WasPressedThisFrame();
        public virtual bool StopLockOn() => m_stopLockOn.WasPressedThisFrame();

        public virtual bool GetLightAttackDown() => m_lightAttack.WasPressedThisFrame();
        public virtual bool GetHeavyAttackDown() => m_heavyAttack.WasPressedThisFrame();
        public virtual bool GetHeavyAttackHeld() => m_heavyAttack.IsPressed();

        protected float RemapToDeadzone(float value, float deadzone) =>
            Mathf.Sign(value) * ((Mathf.Abs(value) - deadzone) / (1 - deadzone));

        public virtual void LockMovementDirection(float duration = 0.25f)
        {
            m_movementDirectionUnlockTime = Time.time + duration;
        }

        protected virtual void Awake() => CacheActions();

        protected virtual void Start()
        {
            m_camera = Camera.main;
            actions.Enable();
        }

        protected virtual void Update()
        {
            if (m_jump.WasPressedThisFrame())
            {
                m_lastJumpTime = Time.time;
            }
        }

        protected virtual void OnEnable() => actions?.Enable();
        protected virtual void OnDisable() => actions?.Disable();
    }
}