using UnityEngine;

namespace HYPERINFINITYPRODUCTIONS.IsekaiRift
{
    [AddComponentMenu("HyperInfinityProductions/Isekai Rift/Player/States/Combat Player State")]
    public class CombatPlayerState : PlayerState
    {
        // --- MENTE DO COMBO ---
        private float m_comboTimer;
        private int m_comboStep;
        private float m_attackCooldownTimer;

        // --- Referência ao Áudio ---
        private PlayerAudio m_audio;

        // --- BUFFERS DE MEMÓRIA MUSCULAR ---
        private float m_lightInputBufferTimer;
        private float m_heavyInputBufferTimer;
        private bool m_wasFullCharge;

        // --- REFERÊNCIA AO ROSTO ---
        private FacialExpressionController m_faceController;

        // VARIÁVEIS DE ATAQUE PESADO
        private float m_chargeTimer;
        private bool m_isCharging;
        private Animator m_animator;

        // CONTROLE DA ESQUIVA
        private float m_dodgeCooldownTimer;
        private float m_dodgeActiveTimer;
        private Vector3 m_dodgeDirection;
        private float m_dodgeLockTimer; // <--- NOVO

        // CONTROLE DE DESLIZE (FORWARD MOMENTUM)
        private float m_momentumTimer;

        // CONTROLE DO ZANSHIN (STARE DOWN SOFTLOCK)
        private float m_softUnlockTimer;
        private bool m_isSoftUnlocking;

        // --- GERENCIAMENTO DE REAÇÃO A DANO INLINE ---
        private bool m_isHurt;
        public bool isHurt => m_isHurt;
        private bool m_isHeavyHurt;
        private float m_hurtTimer;

        protected override void OnEnter(Player player)
        {
            if (m_animator == null)
                m_animator = player.GetComponent<PlayerAnimator>().animator;

            if (m_audio == null)
                m_audio = player.GetComponent<PlayerAudio>();

            if (m_faceController == null)
                m_faceController = player.GetComponentInChildren<FacialExpressionController>();

            m_softUnlockTimer = 0f;
            m_isSoftUnlocking = false;

            m_comboTimer = 0;
            m_comboStep = 0;
            m_attackCooldownTimer = 0;

            // --- TRANSFERÊNCIA DO CLIQUE (Gap Closer no primeiro frame) ---
            if (player.inputs.GetLightAttackDown())
            {
                m_lightInputBufferTimer = 0.4f;
            }
            else
            {
                m_lightInputBufferTimer = 0;
            }

            m_heavyInputBufferTimer = 0;
            m_chargeTimer = 0;
            m_isCharging = false;
            m_dodgeCooldownTimer = 0;
            m_dodgeActiveTimer = 0;

            m_momentumTimer = 0;
            m_isHurt = false;

            player.transform.rotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y, 0);

            // --- DESTRAVA AS HITBOXES PARA O COMBATE ---
            HitboxManager hitboxManager = player.GetComponentInChildren<HitboxManager>();
            if (hitboxManager != null) hitboxManager.isCombatMode = true;

        }

        protected override void OnExit(Player player)
        {
            player.SetInvulnerable(false);

            // Garante que não vai sair do modo combate com soco ativado
            HitboxManager hitboxManager = player.GetComponentInChildren<HitboxManager>();
            if (hitboxManager != null)
            {
                hitboxManager.isCombatMode = false;
                hitboxManager.DisableAllHitboxes();
            }
        }

        protected override void OnStep(Player player)
        {
            player.Gravity();
            player.SnapToGround();

            // --- INTERCEPTOR DE DANO COMBATE ---
            if (m_isHurt)
            {
                player.lateralVelocity = Vector3.Lerp(player.lateralVelocity, Vector3.zero, Time.deltaTime * 6f);
                m_hurtTimer -= Time.deltaTime;

                if (m_isHeavyHurt && m_hurtTimer < 0.25f && player.inputs.GetDodgeDown())
                {
                    m_isHurt = false;
                }

                if (m_hurtTimer <= 0)
                {
                    m_isHurt = false;
                    bool hasHardLockHurt = LockOnSystem.instance != null && LockOnSystem.instance.CurrentTarget != null;
                    bool hasSoftLockHurt = player.currentSoftTarget != null;

                    if (!hasHardLockHurt && !hasSoftLockHurt)
                    {
                        player.states.Change<IdlePlayerState>();
                        return;
                    }
                }

                // --- NOVO: Destrói qualquer botão "esmagado" enquanto o Yuji apanha ---
                m_lightInputBufferTimer = 0f;
                m_heavyInputBufferTimer = 0f;

                if (m_isHurt) return;
            }

            // --- LÓGICA DA ESQUIVA DE COMBATE ---
            if (m_dodgeActiveTimer > 0)
            {
                m_dodgeActiveTimer -= Time.deltaTime;
                player.lateralVelocity = m_dodgeDirection * 15f;
                return;
            }

            // --- IDENTIFICAÇÃO DOS MODOS DE MIRA ---
            bool hasHardLock = LockOnSystem.instance != null && LockOnSystem.instance.CurrentTarget != null;

            // --- NOVO: AUTO-SWITCH E STARE DOWN (ZANSHIN) PARA O SOFTLOCK ---
            if (!hasHardLock)
            {
                bool targetIsInvalid = false;

                // 1. Verifica se o alvo foi destruído ou morreu
                if (player.currentSoftTarget == null || !player.currentSoftTarget.gameObject.activeInHierarchy)
                {
                    targetIsInvalid = true;
                }
                else
                {
                    EmeraldAI.EmeraldHealth enemyHealth = player.currentSoftTarget.GetComponentInParent<EmeraldAI.EmeraldHealth>();
                    if (enemyHealth != null && enemyHealth.CurrentHealth <= 0)
                    {
                        targetIsInvalid = true;
                    }
                }

                // 2. Lógica de transição e Zanshin (Stare Down)
                if (targetIsInvalid && player.currentSoftTarget != null)
                {
                    Transform nextTarget = player.FindSoftTarget();

                    if (nextTarget != null)
                    {
                        // Tem outro inimigo perto! Muda a mira na hora e cancela qualquer timer.
                        player.currentSoftTarget = nextTarget;
                        m_isSoftUnlocking = false;
                    }
                    else
                    {
                        // Não tem mais inimigos vivos perto. Inicia o Stare Down no inimigo morto!
                        if (!m_isSoftUnlocking)
                        {
                            m_isSoftUnlocking = true;
                            m_softUnlockTimer = 2f; // 2 segundos de encarada
                        }
                        else
                        {
                            m_softUnlockTimer -= Time.deltaTime;

                            // Se o Yuji começar a correr ou pular, quebra a pose na hora
                            if (player.inputs.GetRun() || player.inputs.GetJumpDown() || player.inputs.GetDashDown())
                            {
                                m_softUnlockTimer = 0f;
                            }

                            if (m_softUnlockTimer <= 0)
                            {
                                player.currentSoftTarget = null; // Limpa o alvo morto
                                m_isSoftUnlocking = false;       // O restante do OnStep vai expulsar pro Idle
                            }
                        }
                    }
                }
                else if (!targetIsInvalid)
                {
                    // Alvo está vivo e bem
                    m_isSoftUnlocking = false;
                }
            }

            Transform target = hasHardLock ? LockOnSystem.instance.CurrentTarget : player.currentSoftTarget;

            // --- ESCAPE DINÂMICO (EXCLUSIVO PARA SOFT-LOCK) ---
            if (!hasHardLock && m_attackCooldownTimer <= 0 && !m_isCharging)
            {
                // Verifica se o alvo ficou absurdamente longe (ex: 15 unidades de distância) para soltar a mira
                bool targetGotTooFar = target != null && Vector3.Distance(player.transform.position, target.position) > 15f;

                // CORREÇÃO: Removemos o GetDashDown() daqui. 
                // Agora ele só sai do combate se o jogador Correr, Pular ou o inimigo sumir.
                if (player.inputs.GetRun() || player.inputs.GetJumpDown() || targetGotTooFar)
                {
                    player.currentSoftTarget = null;
                    player.states.Change<IdlePlayerState>();

                    // Mantém a fluidez executando apenas o pulo na mesma hora
                    if (player.inputs.GetJumpDown()) player.Jump();

                    return;
                }
            }

            // --- ROTAÇÃO DO PERSONAGEM ---
            if (target != null)
            {
                Vector3 directionToTarget = target.position - player.transform.position;
                directionToTarget.y = 0;

                if (directionToTarget != Vector3.zero)
                    player.FaceDirectionSmooth(directionToTarget.normalized);
            }
            else if (player.inputs.GetMovementCameraDirection().sqrMagnitude > 0 && m_attackCooldownTimer <= 0)
            {
                player.FaceDirectionSmooth(player.inputs.GetMovementCameraDirection());
            }

            // --- ATAQUE E AVANÇO MAGNÉTICO ---
            if (m_attackCooldownTimer > 0)
            {
                m_attackCooldownTimer -= Time.deltaTime;

                if (m_momentumTimer > 0)
                {
                    m_momentumTimer -= Time.deltaTime;

                    if (target != null)
                    {
                        float distance = Vector3.Distance(player.transform.position, target.position);

                        if (distance > 1.5f && distance < 10.5f)
                        {
                            Vector3 dirToTarget = (target.position - player.transform.position).normalized;
                            dirToTarget.y = 0;
                            player.lateralVelocity = Vector3.Lerp(player.lateralVelocity, dirToTarget * 16f, Time.deltaTime * 12f);
                        }
                        else
                        {
                            player.lateralVelocity = Vector3.Lerp(player.lateralVelocity, Vector3.zero, Time.deltaTime * 8f);
                        }
                    }
                    else
                    {
                        player.lateralVelocity = Vector3.Lerp(player.lateralVelocity, Vector3.zero, Time.deltaTime * 8f);
                    }
                }
                else
                {
                    player.lateralVelocity = Vector3.zero;
                }
            }
            else
            {
                // --- MOVIMENTAÇÃO DE POSTURA (WALK) ---
                if (player.inputs.GetMovementCameraDirection().sqrMagnitude > 0)
                {
                    player.AccelerateToInputDirection();

                    float maxCombatSpeed = 3.5f;
                    if (player.lateralVelocity.magnitude > maxCombatSpeed)
                        player.lateralVelocity = player.lateralVelocity.normalized * maxCombatSpeed;
                }
                else
                {
                    player.lateralVelocity = Vector3.zero;
                }
            }

            // --- ATUALIZAÇÃO DO ANIMATOR ---
            if (m_animator != null)
            {
                float combatZ = Vector3.Dot(player.transform.forward, player.lateralVelocity);
                float combatX = Vector3.Dot(player.transform.right, player.lateralVelocity);
                m_animator.SetFloat("CombatHorizontal", combatX, 0.1f, Time.deltaTime);
                m_animator.SetFloat("CombatVertical", combatZ, 0.1f, Time.deltaTime);
            }

            // --- TIMERS INDEPENDENTES ---
            if (m_dodgeCooldownTimer > 0) m_dodgeCooldownTimer -= Time.deltaTime;

            // --- NOVO: Timer da trava de esquiva ---
            if (m_dodgeLockTimer > 0) m_dodgeLockTimer -= Time.deltaTime;

            if (m_comboTimer > 0)
            {
                m_comboTimer -= Time.deltaTime;
                if (m_comboTimer <= 0)
                {
                    // Apenas reseta a contagem do combo para o Light_1, mas NÃO expulsa do modo combate!
                    m_comboStep = 0;
                }
            }

            // --- INTERCEPTA ESQUIVA NO FRAME EXATO ---
            if (HandleDodge(player)) return;

            // --- INPUT BUFFER UNIFICADO ---
            if (player.inputs.GetLightAttackDown())
            {
                m_lightInputBufferTimer = 0.4f;
            }

            if (player.inputs.GetHeavyAttackHeld())
            {
                m_isCharging = true;
                m_chargeTimer += Time.deltaTime;
            }
            else if (m_isCharging)
            {
                m_heavyInputBufferTimer = 0.4f;
                m_wasFullCharge = (m_chargeTimer >= player.stats.current.heavyAttackChargeTime) && (player.chargeAttackCooldownTimer <= 0);
                m_isCharging = false;
                m_chargeTimer = 0;
            }

            if (m_lightInputBufferTimer > 0) m_lightInputBufferTimer -= Time.deltaTime;
            if (m_heavyInputBufferTimer > 0) m_heavyInputBufferTimer -= Time.deltaTime;

            // --- EXECUÇÃO DE GOLPES ---

            // Verifica se a animação da esquiva ainda está acontecendo (primeiros 0.5s)
            bool isDodgeAnimationPlaying = m_dodgeCooldownTimer > 0.3f;

            // Se o jogador "esmagar" o botão durante a esquiva, nós limpamos o buffer
            // Isso impede que o soco fique guardado e saia sozinho quando a esquiva acabar
            if (isDodgeAnimationPlaying)
            {
                m_lightInputBufferTimer = 0f;
                m_heavyInputBufferTimer = 0f;
            }

            // Só permite atacar se o cooldown de ataque estiver zerado E a esquiva tiver acabado
            if (m_attackCooldownTimer <= 0 && !isDodgeAnimationPlaying)
            {
                player.SetInvulnerable(false);

                if (!hasHardLock && player.currentSoftTarget == null)
                {
                    player.states.Change<IdlePlayerState>();
                    return;
                }

                if (m_heavyInputBufferTimer > 0)
                {
                    m_heavyInputBufferTimer = 0;
                    m_lightInputBufferTimer = 0;
                    ExecuteHeavyAttack(player, m_wasFullCharge);
                }
                else if (m_lightInputBufferTimer > 0)
                {
                    m_lightInputBufferTimer = 0;

                    bool executedGapCloser = false;

                    // Lógica de distância (GAP CLOSER)
                    if (m_comboStep == 0 && target != null)
                    {
                        float distanceToTarget = Vector3.Distance(player.transform.position, target.position);

                        if (distanceToTarget > 3.0f && distanceToTarget <= 10.0f)
                        {
                            executedGapCloser = true;
                            ExecuteGapCloser(player, target);
                        }
                    }

                    if (!executedGapCloser)
                    {
                        ExecuteLightCombo(player);
                    }
                }
            }
        }

        public void TriggerCombatHurt(Player player, Vector3 damageOrigin)
        {
            m_isHurt = true;
            m_isHeavyHurt = false;
            float pushForce = 9f;
            float duration = 0.45f;

            if (LockOnSystem.instance != null && LockOnSystem.instance.CurrentTarget != null)
            {
                Transform target = LockOnSystem.instance.CurrentTarget;
                if (target.CompareTag("HeavyEnemy") || target.CompareTag("Boss") || target.gameObject.CompareTag("HeavyEnemy") || target.gameObject.CompareTag("Boss"))
                {
                    m_isHeavyHurt = true;
                    pushForce = 14f;
                    duration = 0.65f;
                }
            }

            Vector3 hitDir = (damageOrigin - player.transform.position).normalized;
            hitDir.y = 0;

            float forwardDot = Vector3.Dot(player.transform.forward, hitDir);
            float rightDot = Vector3.Dot(player.transform.right, hitDir);

            string triggerName = "CombatHurtFront";
            Vector3 pushDirection = -player.transform.forward;

            if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
            {
                if (forwardDot > 0)
                {
                    triggerName = "CombatHurtFront";
                    pushDirection = -player.transform.forward;
                }
                else
                {
                    triggerName = "CombatHurtBack";
                    pushDirection = player.transform.forward;
                }
            }
            else
            {
                if (rightDot > 0)
                {
                    triggerName = "CombatHurtRight";
                    pushDirection = -player.transform.right;
                }
                else
                {
                    triggerName = "CombatHurtLeft";
                    pushDirection = player.transform.right;
                }
            }

            if (m_animator != null)
            {
                m_animator.SetFloat("CombatHorizontal", 0f);
                m_animator.SetFloat("CombatVertical", 0f);
                m_animator.SetTrigger(triggerName);
            }

            m_hurtTimer = duration;
            player.lateralVelocity = pushDirection * pushForce;

            // --- NOVO: Zera o combo, cancela impulsos e adiciona cooldown ---
            m_comboStep = 0;
            m_momentumTimer = 0f;
            m_dodgeActiveTimer = 0f;

            // Trava os golpes pelo tempo que durar o dano (mais uma margem de segurança de 0.1s)
            m_attackCooldownTimer = duration + 0.1f;
        }

        private void ExecuteLightCombo(Player player)
        {
            m_comboStep++;
            HitboxManager hitboxManager = player.GetComponentInChildren<HitboxManager>();

            if (hitboxManager != null)
            {
                hitboxManager.isComboFinisher = false;
                hitboxManager.DisableAllHitboxes();
            }

            if (m_comboStep >= 4)
            {
                m_comboStep = 0;
            }

            if (m_animator != null) m_animator.SetTrigger("Light");

            if (m_faceController != null)
                m_faceController.SetAllAngry(player.stats.current.lightAttackDuration + 0.2f);

            player.lateralVelocity = player.transform.forward * 5f;
            m_momentumTimer = 0.15f;

            m_comboTimer = player.stats.current.comboWindowTime;
            m_attackCooldownTimer = player.stats.current.lightAttackDuration;

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.CompletePhase(TutorialManager.TutorialPhase.LightAttack);
            }
        }

        // --- MÉTODO GAP CLOSER ---
        private void ExecuteGapCloser(Player player, Transform target)
        {
            m_comboStep++;

            HitboxManager hitboxManager = player.GetComponentInChildren<HitboxManager>();
            if (hitboxManager != null)
            {
                hitboxManager.isComboFinisher = false;
                hitboxManager.DisableAllHitboxes();
            }

            if (m_animator != null) m_animator.SetTrigger("GapCloser");

            // --- NOVO: AVISAR O TUTORIAL ---
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.CompletePhase(TutorialManager.TutorialPhase.GapCloser);
            }

            if (m_faceController != null)
                m_faceController.SetAllAngry(player.stats.current.lightAttackDuration + 0.2f);

            Vector3 dirToTarget = (target.position - player.transform.position).normalized;
            dirToTarget.y = 0;

            player.lateralVelocity = dirToTarget * 25f;
            m_momentumTimer = 0.50f;

            m_comboTimer = player.stats.current.comboWindowTime;
            m_attackCooldownTimer = player.stats.current.lightAttackDuration + 0.1f;

            // --- NOVO: Trava a esquiva pelo mesmo tempo do Gap Closer ---
            m_dodgeLockTimer = player.stats.current.lightAttackDuration + 0.1f;
        }

        private void ExecuteHeavyAttack(Player player, bool isFullCharge)
        {
            m_comboStep++;
            HitboxManager hitboxManager = player.GetComponentInChildren<HitboxManager>();

            if (hitboxManager != null) hitboxManager.DisableAllHitboxes();

            if (isFullCharge)
            {
                if (hitboxManager != null) hitboxManager.isComboFinisher = false;
                m_comboStep = 0;

                if (m_animator != null) m_animator.SetTrigger("ChargeAttack");
                m_attackCooldownTimer = player.stats.current.heavyAttackDuration + 0.2f;

                if (m_faceController != null)
                {
                    m_faceController.SetAllAngry(m_attackCooldownTimer + 0.2f);
                    m_faceController.SetMTHA(m_attackCooldownTimer + 0.2f);
                    m_faceController.SetEyeSurprised(m_attackCooldownTimer + 0.2f);
                }

                player.SetInvulnerable(true);
                player.chargeAttackCooldownTimer = player.stats.current.chargeAttackCooldown;

                player.lateralVelocity = player.transform.forward * 12f;
                m_momentumTimer = 0.25f;

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.CompletePhase(TutorialManager.TutorialPhase.ChargeAttack);
                }
            }
            else
            {
                if (m_comboStep >= 3)
                {
                    if (hitboxManager != null) hitboxManager.isComboFinisher = true;
                    m_comboStep = 0;
                }
                else
                {
                    if (hitboxManager != null) hitboxManager.isComboFinisher = false;
                }

                if (m_animator != null) m_animator.SetTrigger("Heavy");
                m_attackCooldownTimer = player.stats.current.heavyAttackDuration;

                if (m_faceController != null)
                {
                    m_faceController.SetAllAngry(m_attackCooldownTimer + 0.2f);
                    m_faceController.SetMTHA(m_attackCooldownTimer + 0.2f);
                    m_faceController.SetEyeSurprised(m_attackCooldownTimer + 0.2f);
                }

                player.lateralVelocity = player.transform.forward * 3.5f;
                m_momentumTimer = 0.08f;

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.CompletePhase(TutorialManager.TutorialPhase.HeavyAttack);
                }
            }

            m_comboTimer = player.stats.current.comboWindowTime;
        }

        private bool HandleDodge(Player player)
        {
            // --- ALTERAÇÃO: Adicionada a verificação do m_dodgeLockTimer ---
            if (player.inputs.GetDodgeDown() && m_dodgeCooldownTimer <= 0 && m_dodgeActiveTimer <= 0 && m_dodgeLockTimer <= 0)
            {
                if (m_audio != null) m_audio.PlayDodgeAudio();

                // Trava os golpes pelo tempo da animação da esquiva
                m_attackCooldownTimer = 0f;
                m_momentumTimer = 0;
                m_lightInputBufferTimer = 0;
                m_heavyInputBufferTimer = 0;

                if (m_animator != null)
                {
                    m_animator.ResetTrigger("Light");
                    m_animator.ResetTrigger("Heavy");
                    m_animator.ResetTrigger("ChargeAttack");
                }

                Vector3 inputDir = player.inputs.GetMovementCameraDirection();

                if (inputDir.sqrMagnitude < 0.1f)
                {
                    if (m_animator != null) m_animator.SetTrigger("DodgeBackward");
                    m_dodgeDirection = -player.transform.forward;
                }
                else
                {
                    m_dodgeDirection = inputDir.normalized;
                    float forwardPower = Vector3.Dot(player.transform.forward, inputDir);
                    float rightPower = Vector3.Dot(player.transform.right, inputDir);

                    if (Mathf.Abs(forwardPower) > Mathf.Abs(rightPower))
                    {
                        if (forwardPower > 0)
                        {
                            if (m_animator != null) m_animator.SetTrigger("DodgeForward");
                        }
                        else
                        {
                            if (m_animator != null) m_animator.SetTrigger("DodgeBackward");
                        }
                    }
                    else
                    {
                        if (rightPower > 0)
                        {
                            if (m_animator != null) m_animator.SetTrigger("DodgeRight");
                        }
                        else
                        {
                            if (m_animator != null) m_animator.SetTrigger("DodgeLeft");
                        }
                    }
                }
                m_dodgeActiveTimer = 0.3f;
                m_dodgeCooldownTimer = 0.8f;

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.CompletePhase(TutorialManager.TutorialPhase.Dodge);
                }

                // Força o empurrão da esquiva imediatamente
                player.lateralVelocity = m_dodgeDirection * 15f;

                return true;
            }

            return false;
        }

        public override void OnContact(Player player, Collider other) { }
    }
}