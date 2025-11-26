using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// Manages object pooling for combat-related objects to reduce allocations.
    /// Extracted from CombatInteractionManager to reduce complexity.
    /// </summary>
    public class CombatObjectPoolManager
    {
        private readonly SkillExecutionPool executionPool = new SkillExecutionPool();
        private static Stack<List<SkillExecution>> skillExecutionListPool = new Stack<List<SkillExecution>>();
        private static Stack<List<List<SkillExecution>>> nestedListPool = new Stack<List<List<SkillExecution>>>();
        private static Stack<List<SpeedResolutionGroupResult>> resultsListPool = new Stack<List<SpeedResolutionGroupResult>>();

        // SkillExecution pool methods
        public SkillExecution GetSkillExecution()
        {
            return executionPool.Get();
        }

        public void ReturnSkillExecution(SkillExecution execution)
        {
            executionPool.Return(execution);
        }

        // List pool methods
        public List<SkillExecution> GetSkillExecutionList()
        {
            return skillExecutionListPool.Count > 0 ? skillExecutionListPool.Pop() : new List<SkillExecution>();
        }

        public void ReturnSkillExecutionList(List<SkillExecution> list)
        {
            list.Clear();
            skillExecutionListPool.Push(list);
        }

        public List<SpeedResolutionGroupResult> GetResultsList()
        {
            return resultsListPool.Count > 0 ? resultsListPool.Pop() : new List<SpeedResolutionGroupResult>();
        }

        public void ReturnResultsList(List<SpeedResolutionGroupResult> list)
        {
            list.Clear();
            resultsListPool.Push(list);
        }

        public List<List<SkillExecution>> GetNestedList()
        {
            return nestedListPool.Count > 0 ? nestedListPool.Pop() : new List<List<SkillExecution>>();
        }

        public void ReturnNestedList(List<List<SkillExecution>> list)
        {
            // Return inner lists to pool first
            foreach (var innerList in list)
            {
                ReturnSkillExecutionList(innerList);
            }
            list.Clear();
            nestedListPool.Push(list);
        }

        private class SkillExecutionPool
        {
            private Stack<SkillExecution> pool = new Stack<SkillExecution>(CombatConstants.SKILL_EXECUTION_POOL_INITIAL_CAPACITY);

            public SkillExecution Get()
            {
                return pool.Count > 0 ? pool.Pop() : new SkillExecution();
            }

            public void Return(SkillExecution execution)
            {
                execution.Reset();
                pool.Push(execution);
            }
        }
    }
}
