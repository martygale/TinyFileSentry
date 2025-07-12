using TinyFileSentry.Core.Interfaces;
using TinyFileSentry.Core.Models;

namespace TinyFileSentry.Core.Services;

public class RulesService : IRulesService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogService _logService;
    private Configuration _configuration;

    public event EventHandler<WatchRule>? RuleAdded;
    public event EventHandler<WatchRule>? RuleUpdated;
    public event EventHandler<WatchRule>? RuleRemoved;

    public RulesService(IConfigurationService configurationService, ILogService logService)
    {
        _configurationService = configurationService;
        _logService = logService;
        _configuration = _configurationService.LoadConfiguration();
    }

    public IEnumerable<WatchRule> GetWatchRules() => _configuration.WatchRules;

    public void AddWatchRule(WatchRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        _configuration.WatchRules.Add(rule);
        _configurationService.SaveConfiguration(_configuration);
        _logService.Info($"Watch rule added: {rule.SourceFile} -> {rule.DestinationRoot}", nameof(RulesService));
        RuleAdded?.Invoke(this, rule);
    }

    public void UpdateWatchRule(WatchRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        var existingRule = _configuration.WatchRules.FirstOrDefault(r => r.SourceFile == rule.SourceFile);
        if (existingRule == null)
        {
            _logService.Warning($"Watch rule not found for update: {rule.SourceFile}", nameof(RulesService));
            return;
        }

        existingRule.DestinationRoot = rule.DestinationRoot;
        existingRule.PostAction = rule.PostAction;

        _configurationService.SaveConfiguration(_configuration);
        _logService.Info($"Watch rule updated: {rule.SourceFile}", nameof(RulesService));
        RuleUpdated?.Invoke(this, existingRule);
    }

    public void RemoveWatchRule(WatchRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        var removed = _configuration.WatchRules.Remove(rule);
        if (removed)
        {
            _configurationService.SaveConfiguration(_configuration);
            _logService.Info($"Watch rule removed: {rule.SourceFile}", nameof(RulesService));
            RuleRemoved?.Invoke(this, rule);
        }
        else
        {
            _logService.Warning($"Watch rule not found for removal: {rule.SourceFile}", nameof(RulesService));
        }
    }

}