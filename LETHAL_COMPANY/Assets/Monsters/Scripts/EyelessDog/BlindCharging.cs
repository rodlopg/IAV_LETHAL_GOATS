using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.EyelessDog
{
    public class BlindCharging : State
    {
        private Vector3 chargeDirection;
        private Vector3 startPosition;
        private float chargeTimer;

        public BlindCharging(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.DOG_BLIND_CHARGING;
        }

        public override void Enter()
        {
            SetTrigger("isCharging");
            Vector3 noisePosition;
            float loudness;
            if (settings.TryGetLatestNoise(out noisePosition, out loudness))
                chargeDirection = noisePosition - npc.transform.position;
            else
                chargeDirection = npc.transform.forward;

            chargeDirection.y = 0f;
            if (chargeDirection.sqrMagnitude < 0.01f)
                chargeDirection = npc.transform.forward;
            chargeDirection.Normalize();
            startPosition = npc.transform.position;
            chargeTimer = 0f;

            if (AgentReady())
            {
                agent.ResetPath();
                agent.isStopped = false;
                agent.updateRotation = false;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            chargeTimer += Time.deltaTime;
            npc.transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up);

            float speed = Mathf.Min(settings.dogChargeStartSpeed +
                settings.dogChargeAcceleration * chargeTimer, settings.dogChargeMaxSpeed);

            if (settings.HasObstacleAhead(chargeDirection, 1.1f))
            {
                TransitionTo(new Roaming(npc, agent, animator, player));
                return;
            }

            if (AgentReady())
                agent.Move(chargeDirection * speed * Time.deltaTime);

            float travelled = Vector3.Distance(startPosition, npc.transform.position);
            if (travelled >= settings.dogChargeMaxDistance || chargeTimer >= settings.dogChargeMaxDuration)
                TransitionTo(new Roaming(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isCharging");
            if (AgentReady())
                agent.updateRotation = true;
            base.Exit();
        }
    }
}
