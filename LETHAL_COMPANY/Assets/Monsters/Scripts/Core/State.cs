using UnityEngine;
using UnityEngine.AI;

namespace LethalCompanyAI
{
    // One state instance owns one phase of an enemy's behavior.
    public enum EVENT
    {
        ENTER,
        UPDATE,
        EXIT
    }

    public enum STATE
    {
        BRACKEN_STALKING,
        BRACKEN_RETREATING,
        BRACKEN_ENRAGED_ATTACK,

        COIL_HEAD_PATROL,
        COIL_HEAD_PURSUE,
        COIL_HEAD_RECHARGING,

        DOG_ROAMING,
        DOG_INVESTIGATING,
        DOG_BLIND_CHARGING,

        NUTCRACKER_WANDERING,
        NUTCRACKER_SCANNING,
        NUTCRACKER_SHOOTING,

        THUMPER_PATROLLING,
        THUMPER_STRAIGHT_LINE_CHARGE,
        THUMPER_STUNNED_WALL_HIT
    }

    public abstract class State
    {
        protected GameObject npc;
        protected NavMeshAgent agent;
        protected Animator animator;
        protected Transform player;
        protected MonsterController settings;

        protected STATE name;
        protected EVENT stage = EVENT.ENTER;
        protected State nextState;

        protected State(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player)
        {
            npc = _npc;
            agent = _agent;
            animator = _animator;
            player = _player;
            settings = npc != null ? npc.GetComponent<MonsterController>() : null;
        }

        // Called once per frame by MonsterController. ENTER and EXIT are each
        // processed on their own frame, keeping transitions predictable.
        public State Process()
        {
            if (settings != null)
                player = settings.Player;

            switch (stage)
            {
                case EVENT.ENTER:
                    Enter();
                    break;
                case EVENT.UPDATE:
                    Update();
                    break;
                case EVENT.EXIT:
                    Exit();
                    return nextState != null ? nextState : this;
            }

            return this;
        }

        public abstract void Enter();
        public abstract void Update();
        // Optional shared cleanup hook. Concrete states may call base.Exit().
        public virtual void Exit() { }

        protected void TransitionTo(State state)
        {
            nextState = state;
            stage = EVENT.EXIT;
        }

        protected void SetTrigger(string triggerName)
        {
            if (animator != null && HasTrigger(triggerName))
                animator.SetTrigger(triggerName);
        }

        protected void ResetTrigger(string triggerName)
        {
            if (animator != null && HasTrigger(triggerName))
                animator.ResetTrigger(triggerName);
        }

        private bool HasTrigger(string triggerName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == triggerName &&
                    parameters[i].type == AnimatorControllerParameterType.Trigger)
                    return true;
            }

            return false;
        }

        protected bool AgentReady()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        protected void SetAgentStopped(bool stopped)
        {
            if (AgentReady())
                agent.isStopped = stopped;
        }
    }
}
