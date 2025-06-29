using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Interfaces;

public interface IRulesService
{
    IEnumerable<WatchRule> GetWatchRules();
    void AddWatchRule(WatchRule rule);
    void UpdateWatchRule(WatchRule rule);
    void RemoveWatchRule(WatchRule rule);
    void UpdateRuleLastCopied(WatchRule rule, DateTime lastCopied);
    event EventHandler<WatchRule>? RuleAdded;
    event EventHandler<WatchRule>? RuleUpdated;
    event EventHandler<WatchRule>? RuleRemoved;
}