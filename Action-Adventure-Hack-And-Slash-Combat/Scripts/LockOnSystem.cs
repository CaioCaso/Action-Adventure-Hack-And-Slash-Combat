using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using EmeraldAI;

namespace HYPERINFINITYPRODUCTIONS.IsekaiRift
{
    [AddComponentMenu("HyperInfinityProductions/Isekai Rift/Combat/Lock On System")]
    public class LockOnSystem : MonoBehaviour
    {
        #region Static Instance
        public static LockOnSystem instance { get; private set; }
        #endregion

        #region Inspector Variables
        [Tooltip("Speed at which the player slerps towards the target")]
        [SerializeField] private float slerpSpeed = 5f;
        [Tooltip("Layer mask for filtering lockable targets")]
        [SerializeField] private LayerMask targetLayer;
        [Tooltip("Reference to the player's camera")]
        [SerializeField] private Transform playerCamera;
        [Tooltip("UI element to indicate locked-on target")]
        [SerializeField] private Image lockOnIndicator;
        [Tooltip("Maximum lock-on radius")]
        [SerializeField] private float lockOnRadius = 30f;

        [Header("Game Feel: Zanshin (Stare Down)")]
        [Tooltip("Tempo em segundos que o personagem mantém a guarda e encara o inimigo após matá-lo")]
        [SerializeField] private float unlockDelayAfterKill = 2f;
        #endregion

        #region Private Variables
        private Transform currentTarget;
        private Player player => Level.instance.player;

        private PlayerCamera playerCameraScript;
        private CinemachineImpulseSource m_impulseSource;

        private float m_unlockTimer = 0f;
        private bool m_isUnlocking = false;
        #endregion

        #region Properties
        public Transform CurrentTarget
        {
            get => currentTarget;
            private set
            {
                if (currentTarget != value)
                {
                    currentTarget = value;
                    if (lockOnIndicator != null)
                        lockOnIndicator.enabled = currentTarget != null;
                }
            }
        }
        #endregion

        private void Awake() => instance = this;

        private void Start()
        {
            if (lockOnIndicator != null)
                lockOnIndicator.enabled = false;

            playerCameraScript = GetComponent<PlayerCamera>();
            m_impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private void Update()
        {
            if (player.health.isEmpty)
            {
                if (currentTarget != null)
                {
                    StopTargetLocking(false);
                }
                return;
            }

            if (player.inputs.GetLockedOn() && !m_isUnlocking)
                LockOnNearestTarget();

            if (player.inputs.StopLockOn())
            {
                bool canUnlock = true;

                if (player.states.current is CombatPlayerState combatState)
                {
                    if (combatState.isHurt)
                    {
                        canUnlock = false;
                    }
                }

                if (canUnlock)
                {
                    StopTargetLocking(true);
                }
                else
                {
                    Debug.Log("Lock-On mantido: Você não pode fugir do combate enquanto sofre dano!");
                }
            }

            if (currentTarget != null)
            {
                bool isTargetDead = false;

                if (currentTarget.Equals(null) || !currentTarget.gameObject.activeInHierarchy)
                {
                    isTargetDead = true;
                }
                else
                {
                    EmeraldHealth enemyHealth = currentTarget.GetComponentInParent<EmeraldHealth>();
                    if (enemyHealth != null && enemyHealth.CurrentHealth <= 0)
                    {
                        isTargetDead = true;
                    }
                }

                if (isTargetDead)
                {
                    if (!TryLockOnNextTarget())
                    {
                        if (!m_isUnlocking)
                        {
                            m_isUnlocking = true;
                            m_unlockTimer = unlockDelayAfterKill;

                            if (lockOnIndicator != null)
                                lockOnIndicator.enabled = false;
                        }
                        else
                        {
                            m_unlockTimer -= Time.deltaTime;
                            if (m_unlockTimer <= 0)
                            {
                                StopTargetLocking(false);
                            }
                        }
                    }
                    else
                    {
                        m_isUnlocking = false;
                        if (lockOnIndicator != null)
                            lockOnIndicator.enabled = true;
                    }
                }
                else
                {
                    m_isUnlocking = false;
                }
            }
            else
            {
                if (lockOnIndicator != null && lockOnIndicator.enabled)
                    lockOnIndicator.enabled = false;
            }
        }

        private bool TryLockOnNextTarget()
        {
            if (player.holding) return false;

            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, lockOnRadius, targetLayer);

            if (enemiesInRange.Length == 0) return false;

            Transform nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var enemyCollider in enemiesInRange)
            {
                EmeraldHealth enemyHealth = enemyCollider.GetComponentInParent<EmeraldHealth>();
                if (enemyHealth != null && enemyHealth.CurrentHealth <= 0)
                    continue;

                float distanceToEnemy = Vector3.Distance(transform.position, enemyCollider.transform.position);
                if (distanceToEnemy < nearestDistance)
                {
                    nearestDistance = distanceToEnemy;
                    nearestTarget = enemyCollider.transform;
                }
            }

            if (nearestTarget != null)
            {
                CurrentTarget = nearestTarget;

                if (playerCameraScript != null)
                    playerCameraScript.lockTarget = CurrentTarget;

                return true;
            }

            return false;
        }

        private void LockOnNearestTarget()
        {
            if (player.holding) return;

            if (TryLockOnNextTarget())
            {
                bool canEnterCombat = player.states.IsCurrentOfType(
                    typeof(IdlePlayerState),
                    typeof(WalkPlayerState)
                );

                if (canEnterCombat)
                {
                    player.states.Change<CombatPlayerState>();
                }
            }
            else
            {
                StopTargetLocking(true);
            }
        }

        private void StopTargetLocking(bool forceStateChange = true)
        {
            currentTarget = null;
            m_isUnlocking = false;

            if (playerCameraScript != null)
                playerCameraScript.lockTarget = null;

            if (lockOnIndicator != null)
                lockOnIndicator.enabled = false;

            if (forceStateChange && player.states.IsCurrentOfType(typeof(CombatPlayerState)))
            {
                player.states.Change<IdlePlayerState>();
            }
        }

        public void ShakeCamera(float force)
        {
            if (m_impulseSource != null)
            {
                m_impulseSource.GenerateImpulseWithForce(force);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, lockOnRadius);
        }
    }
}