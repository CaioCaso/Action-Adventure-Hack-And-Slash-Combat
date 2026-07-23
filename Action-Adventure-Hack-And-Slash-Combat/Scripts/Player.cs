using UnityEngine;

namespace HYPERINFINITYPRODUCTIONS.IsekaiRift
{
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(PlayerStatsManager))]
    [RequireComponent(typeof(PlayerStateManager))]
    [RequireComponent(typeof(Health))]
    [AddComponentMenu("HyperInfinityProductions/Isekai Rift/Player/Player")]
    public class Player : Entity<Player>
    {
        [Header("Player Settings")]
        public PlayerEvents playerEvents;
        public Transform skin;

        [Header("Soft-Lock Combat Settings")]
        public float softLockRadius = 9f; // Distância máxima para detectar inimigos
        public float softLockAngle = 360f; // Cone de visão
        public LayerMask enemyLayer; // Camada dos inimigos

        [HideInInspector]
        public Transform currentSoftTarget; // Guarda o alvo invisível do soft-lock

        protected Vector3 m_respawnPosition;
        protected Quaternion m_respawnRotation;

        protected Vector3 m_skinInitialPosition;
        protected Quaternion m_skinInitialRotation;

        public PlayerInputManager inputs { get; protected set; }
        public PlayerStatsManager stats { get; protected set; }
        public Health health { get; protected set; }

        [HideInInspector]
        public Vector3 lastDamageOrigin;
        public virtual bool isAlive => !health.isEmpty;

        public bool isInvulnerable { get; private set; }

        public void SetInvulnerable(bool value)
        {
            isInvulnerable = value;
        }

        protected virtual void InitializeInputs() => inputs = GetComponent<PlayerInputManager>();
        protected virtual void InitializeStats() => stats = GetComponent<PlayerStatsManager>();
        protected virtual void InitializeHealth() => health = GetComponent<Health>();
        protected virtual void InitializeTag() => tag = GameTags.Player;

        protected virtual void InitializeRespawn()
        {
            m_respawnPosition = transform.position;
            m_respawnRotation = transform.rotation;
        }

        protected virtual void InitializeSkin()
        {
            if (!skin) return;

            m_skinInitialPosition = skin.localPosition;
            m_skinInitialRotation = skin.localRotation;
        }

        public virtual void Respawn()
        {
            health.ResetHealth();
            velocity = Vector3.zero;
            gravityField = null;
            transform.SetPositionAndRotation(m_respawnPosition, m_respawnRotation);
            states.Change<IdlePlayerState>();
        }

        public override void ApplyDamage(int amount, Vector3 origin)
        {
            if (health.isEmpty || health.recovering) return;

            health.Damage(amount);
            lastDamageOrigin = origin;

            if (health.isEmpty)
            {
                states.Change<DiePlayerState>();
                playerEvents.OnHurt?.Invoke();
                playerEvents.OnDie?.Invoke();
                return;
            }

            bool isInCombat = states.IsCurrentOfType(typeof(CombatPlayerState)) ||
                             (LockOnSystem.instance != null && LockOnSystem.instance.CurrentTarget != null);

            if (isInCombat)
            {
                if (!states.IsCurrentOfType(typeof(CombatPlayerState)))
                {
                    states.Change<CombatPlayerState>();
                }

                CombatPlayerState combatState = states.current as CombatPlayerState;
                if (combatState != null)
                {
                    combatState.TriggerCombatHurt(this, origin);
                }
            }
            else
            {
                var head = origin - transform.position;
                var upOffset = Vector3.Dot(transform.up, head);
                var damageDir = (head - transform.up * upOffset).normalized;
                var localDamageDir = Quaternion.FromToRotation(transform.up, Vector3.up) * damageDir;

                FaceDirection(localDamageDir);
                lateralVelocity = -localForward * stats.current.hurtBackwardsForce;

                verticalVelocity = Vector3.up * stats.current.hurtUpwardForce;
                states.Change<HurtPlayerState>();
            }

            playerEvents.OnHurt?.Invoke();
        }

        public Transform FindSoftTarget()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, softLockRadius, enemyLayer);
            Transform bestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (Collider enemy in enemies)
            {
                EmeraldAI.EmeraldHealth enemyHealth = enemy.GetComponentInParent<EmeraldAI.EmeraldHealth>();
                if (enemyHealth != null && enemyHealth.CurrentHealth <= 0) continue;

                Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToEnemy);

                if (angle <= softLockAngle / 2f)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = enemy.transform;
                    }
                }
            }
            return bestTarget;
        }

        public virtual void Die()
        {
            health.Set(0);
            playerEvents.OnDie?.Invoke();
        }

        private bool isRunning = false;

        public virtual void Accelerate(Vector3 direction)
        {
            if (inputs.GetRun())
            {
                isRunning = !isRunning;
            }

            var turningDrag = isGrounded && isRunning
                ? stats.current.runningTurningDrag
                : stats.current.turningDrag;

            var acceleration = isGrounded && isRunning
                ? stats.current.runningAcceleration
                : stats.current.acceleration;

            var finalAcceleration = isGrounded
                ? acceleration
                : stats.current.airAcceleration;

            var topSpeed = isRunning
                ? stats.current.runningTopSpeed
                : stats.current.topSpeed;

            Accelerate(direction, turningDrag, finalAcceleration, topSpeed);

            if (!isRunning)
            {
                lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, topSpeed);
            }
        }

        public virtual void AccelerateToInputDirection()
        {
            var inputDirection = inputs.GetMovementCameraDirection();
            Accelerate(inputDirection);
        }

        public virtual void Gravity()
        {
            if (!isGrounded && verticalVelocity.y > -stats.current.gravityTopSpeed)
            {
                var speed = verticalVelocity.y;
                var force = verticalVelocity.y > 0 ? stats.current.gravity : stats.current.fallGravity;
                speed -= force * gravityMultiplier * Time.deltaTime;
                speed = Mathf.Max(speed, -stats.current.gravityTopSpeed);
                verticalVelocity = new Vector3(0, speed, 0);
            }
        }

        public virtual void SnapToGround() => SnapToGround(stats.current.snapForce);

        public virtual void FaceDirectionSmooth(Vector3 direction) => FaceDirection(direction, stats.current.rotationSpeed);

        public virtual void Jump()
        {
            var canMultiJump = (jumpCounter > 0) && (jumpCounter < stats.current.multiJumps);
            var canCoyoteJump = (jumpCounter == 0) && (Time.time < lastGroundTime + stats.current.coyoteJumpThreshold);

            if (isGrounded || canMultiJump || canCoyoteJump)
            {
                if (inputs.GetJumpDown())
                {
                    if (jumpCounter == 0 && jumpCooldownTimer > 0f) return;
                    Jump(stats.current.maxJumpHeight);
                }
            }

            if (inputs.GetJumpUp() && (jumpCounter > 0) && (verticalVelocity.y > stats.current.minJumpHeight))
            {
                verticalVelocity = new Vector3(0, verticalVelocity.y * 0.5f, 0);
            }
        }

        public virtual void Jump(float height)
        {
            jumpCounter++;
            if (jumpCounter == 1) jumpCooldownTimer = jumpCooldown;

            verticalVelocity = Vector3.up * height;
            states.Change<FallPlayerState>();
            playerEvents.OnJump?.Invoke();
        }

        public int jumpCounter { get; protected set; }
        private float jumpCooldownTimer = 0f;
        public float jumpCooldown = 0.5f;

        public virtual void ResetJumps() => jumpCounter = 0;

        protected override void Awake()
        {
            base.Awake();
            InitializeInputs();
            InitializeStats();
            InitializeHealth();
            InitializeTag();
            InitializeRespawn();

            entityEvents.OnGroundEnter.AddListener(() => ResetJumps());
        }
    }
}