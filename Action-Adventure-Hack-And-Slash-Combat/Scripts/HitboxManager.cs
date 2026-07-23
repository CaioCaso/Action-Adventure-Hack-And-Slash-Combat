using UnityEngine;

namespace HYPERINFINITYPRODUCTIONS.IsekaiRift
{
    public class HitboxManager : MonoBehaviour
    {
        [Header("Arraste os Box Colliders dos ossos aqui")]
        public Collider rightHandHitbox;
        public Collider leftHandHitbox;
        public Collider rightFootHitbox;
        public Collider chargeAttackHitbox;
        public Collider leftFootHitbox;
        public Collider rightKneeHitbox;
        public Collider leftKneeHitbox;
        public Collider Heavy_Quick_3Hitbox;

        // --- NOVO: HITBOX DO GAP CLOSER ---
        public Collider gapCloserHitbox;

        // --- NOVO: FUNÇÃO DE LIMPEZA GERAL ---
        public void DisableAllHitboxes()
        {
            if (rightHandHitbox) rightHandHitbox.enabled = false;
            if (leftHandHitbox) leftHandHitbox.enabled = false;
            if (rightFootHitbox) rightFootHitbox.enabled = false;
            if (leftFootHitbox) leftFootHitbox.enabled = false;
            if (rightKneeHitbox) rightKneeHitbox.enabled = false;
            if (leftKneeHitbox) leftKneeHitbox.enabled = false;
            if (chargeAttackHitbox) chargeAttackHitbox.enabled = false;
            if (Heavy_Quick_3Hitbox) Heavy_Quick_3Hitbox.enabled = false;

            // --- NOVO: LIMPEZA DO GAP CLOSER ---
            if (gapCloserHitbox) gapCloserHitbox.enabled = false;
        }

        // --- NOVO: CONTROLE DE KNOCKBACK FINAL ---
        [Header("Finisher Settings")]
        public bool isComboFinisher = false;
        public float finisherPushbackMultiplier = 4f; // Multiplica o empurrão em 4x no último golpe!

        // --- NOVO: TRAVA DE SEGURANÇA GERAL ---
        [Header("Security")]
        public bool isCombatMode = false; // Só deixa ligar hitbox se for TRUE

        // --- MÃO DIREITA ---
        public void EnableRightHand() { if (rightHandHitbox) rightHandHitbox.enabled = true; }
        public void DisableRightHand() { if (rightHandHitbox) rightHandHitbox.enabled = false; }

        // --- MÃO ESQUERDA ---
        public void EnableLeftHand() { if (leftHandHitbox) leftHandHitbox.enabled = true; }
        public void DisableLeftHand() { if (leftHandHitbox) leftHandHitbox.enabled = false; }

        // --- PÉ DIREITO ---
        public void EnableRightFoot() { if (rightFootHitbox) rightFootHitbox.enabled = true; }
        public void DisableRightFoot() { if (rightFootHitbox) rightFootHitbox.enabled = false; }

        // --- PÉ ESQUERDO ---
        public void EnableLeftFoot() { if (leftFootHitbox) leftFootHitbox.enabled = true; }
        public void DisableLeftFoot() { if (leftFootHitbox) leftFootHitbox.enabled = false; }

        // --- JOELHO DIREITO ---
        public void EnableRightKnee() { if (rightKneeHitbox) rightKneeHitbox.enabled = true; }
        public void DisableRightKnee() { if (rightKneeHitbox) rightKneeHitbox.enabled = false; }

        // --- JOELHO ESQUERDO ---
        public void EnableLeftKnee() { if (leftKneeHitbox) leftKneeHitbox.enabled = true; }
        public void DisableLeftKnee() { if (leftKneeHitbox) leftKneeHitbox.enabled = false; }

        // --- Heavy_Quick_3 ---
        public void EnableHeavyQuick3() { if (Heavy_Quick_3Hitbox) Heavy_Quick_3Hitbox.enabled = true; }
        public void DisableHeavyQuick3() { if (Heavy_Quick_3Hitbox) Heavy_Quick_3Hitbox.enabled = false; }

        // --- ATAQUE CARREGADO ---
        public void EnableChargeAttack() { if (chargeAttackHitbox) chargeAttackHitbox.enabled = true; }
        public void DisableChargeAttack() { if (chargeAttackHitbox) chargeAttackHitbox.enabled = false; }

        // --- NOVO: GAP CLOSER ---
        public void EnableGapCloser() { if (gapCloserHitbox) gapCloserHitbox.enabled = true; }
        public void DisableGapCloser() { if (gapCloserHitbox) gapCloserHitbox.enabled = false; }

        // --- NOVO: PONTES PARA EVENTOS DE ANIMAÇÃO DE ÁUDIO ---
        public void AnimEvent_PlayLightAudio()
        {
            PlayerAudio audio = GetComponentInParent<PlayerAudio>();
            if (audio != null) audio.PlayLightAttackAudio();
        }

        public void AnimEvent_PlayHeavyAudio()
        {
            PlayerAudio audio = GetComponentInParent<PlayerAudio>();
            if (audio != null) audio.PlayHeavyAttackAudio();
        }

        public void AnimEvent_PlayChargeAudio()
        {
            PlayerAudio audio = GetComponentInParent<PlayerAudio>();
            if (audio != null) audio.PlayChargeAttackAudio();
        }

        // --- NOVO: PONTE DE ÁUDIO PARA O GAP CLOSER ---
        public void AnimEvent_PlayGapCloserAudio()
        {
            PlayerAudio audio = GetComponentInParent<PlayerAudio>();
            if (audio != null) audio.PlayGapCloserAudio();
        }

        // --- NOVO: PONTE PARA O SOM DE PASSOS ---
        public void AnimEvent_PlayFootstep()
        {
            // Procura o script de passos no objeto principal (pai) e manda ele tocar!
            PlayerFootsteps footsteps = GetComponentInParent<PlayerFootsteps>();
            if (footsteps != null) footsteps.PlayFootstepSound();
        }
    }
}