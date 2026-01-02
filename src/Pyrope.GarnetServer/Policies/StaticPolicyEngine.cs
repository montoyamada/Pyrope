using System;
using Pyrope.GarnetServer.Model;

namespace Pyrope.GarnetServer.Policies
{
    public class StaticPolicyEngine : IPolicyEngine
    {
        private class PolicyState
        {
            public TimeSpan Ttl { get; init; }
        }

        private volatile PolicyState _state;

        public StaticPolicyEngine(TimeSpan defaultTtl)
        {
            _state = new PolicyState { Ttl = defaultTtl };
        }

        public PolicyDecision Evaluate(QueryKey key)
        {
            // Simple static rule: Always cache with current TTL
            var state = _state;
            return PolicyDecision.Cache(state.Ttl);
        }

        public void UpdatePolicy(Pyrope.Policy.WarmPathPolicy policy)
        {
            var newState = new PolicyState
            {
                Ttl = TimeSpan.FromSeconds(policy.TtlSeconds)
            };
            _state = newState;
        }
    }
}
