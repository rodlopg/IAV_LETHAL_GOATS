using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.CoilHead
{
    public class Recharging : State
    {
        private float rechargeTimer;

        public Recharging(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.COIL_HEAD_RECHARGING;
        }

        public override void Enter()
        {
            SetTrigger("isRecharging");
            rechargeTimer = 0f;
            if (AgentReady())
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer < settings.coilRechargeDuration)
                return;

            if (player != null && Vector3.Distance(npc.transform.position, player.position) <= settings.viewDistance &&
                !settings.IsPlayerLookingAtMonster(settings.coilLookAngle))
                TransitionTo(new Pursue(npc, agent, animator, player));
            else
                TransitionTo(new Patrol(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isRecharging");
            base.Exit();
        }
    }
}
