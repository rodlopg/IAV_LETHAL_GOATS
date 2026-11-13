using UnityEngine;
using UnityEngine.AI;
using LethalCompanyAI;

namespace LethalCompanyAI.Thumper
{
    public class StunnedWallHit : State
    {
        private float stunTimer;

        public StunnedWallHit(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
            : base(_npc, _agent, _animator, _player)
        {
            name = STATE.THUMPER_STUNNED_WALL_HIT;
        }

        public override void Enter()
        {
            SetTrigger("isStunned");
            stunTimer = 0f;
            if (AgentReady())
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.updateRotation = true;
            }
            stage = EVENT.UPDATE;
        }

        public override void Update()
        {
            stunTimer += Time.deltaTime;
            if (stunTimer >= settings.thumperStunDuration)
                TransitionTo(new Patrolling(npc, agent, animator, player));
        }

        public override void Exit()
        {
            ResetTrigger("isStunned");
            base.Exit();
        }
    }
}
