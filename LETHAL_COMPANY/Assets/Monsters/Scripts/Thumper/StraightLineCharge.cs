using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Thumper
{
    public class StraightLineCharge : State
    {
        private Vector3 chargeDirection;
        private Vector3 startPosition;
        private Vector3 lastPosition;
        private float chargeTimer;
        private float stuckTimer;

        public StraightLineCharge(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.THUMPER_STRAIGHT_LINE_CHARGE;
        }

        public override void Enter()
        {
            SetTrigger("isCharging");
            chargeDirection = player != null ? player.position - npc.transform.position : npc.transform.forward;
            chargeDirection.y = 0f;
            if (chargeDirection.sqrMagnitude < 0.01f)
                chargeDirection = npc.transform.forward;
            chargeDirection.Normalize();

            startPosition = npc.transform.position;
            lastPosition = npc.transform.position;
            chargeTimer = 0f;
            stuckTimer = 0f;

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

            if (settings.HasObstacleAhead(chargeDirection, settings.thumperWallCheckDistance))
            {
                TransitionTo(new StunnedWallHit(npc, agent, animator, player));
                return;
            }

            float speed = Mathf.Min(settings.thumperChargeStartSpeed +
                settings.thumperChargeAcceleration * chargeTimer, settings.thumperChargeMaxSpeed);

            if (AgentReady())
                agent.Move(chargeDirection * speed * Time.deltaTime);

            float movement = Vector3.Distance(npc.transform.position, lastPosition);
            if (movement < settings.thumperStuckMovementThreshold)
                stuckTimer += Time.deltaTime;
            else
                stuckTimer = 0f;
            lastPosition = npc.transform.position;

            float travelled = Vector3.Distance(startPosition, npc.transform.position);
            if (stuckTimer >= settings.thumperStuckTime ||
                travelled >= settings.thumperChargeMaxDistance ||
                chargeTimer >= settings.thumperChargeMaxDuration)
            {
                TransitionTo(new StunnedWallHit(npc, agent, animator, player));
            }
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
