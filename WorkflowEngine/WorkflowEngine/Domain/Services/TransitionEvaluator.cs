using System.Text.Json;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Domain.Services;

/// <summary>
/// Evaluates transition conditions based on step data
/// </summary>
public static class TransitionEvaluator
{
    /// <summary>
    /// Evaluates which transitions to take based on step data.
    /// Returns multiple step IDs when parallel transitions are configured.
    /// </summary>
    /// <param name="transitions">Available transitions from current step</param>
    /// <param name="stepData">JSON data from the completed step</param>
    /// <returns>List of next step IDs to transition to, or empty list for sequential flow</returns>
    public static List<Guid> EvaluateNextSteps(List<StepTransition> transitions, string? stepData)
    {
        if (transitions == null || transitions.Count == 0)
            return new List<Guid>(); // No custom transitions, use sequential flow

        Dictionary<string, object>? data = null;
        if (!string.IsNullOrEmpty(stepData))
        {
            try
            {
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(stepData);
            }
            catch
            {
                // If parsing fails, treat as no data
                data = null;
            }
        }

        // Sort by priority (lower first)
        var sortedTransitions = transitions.OrderBy(t => t.Priority).ToList();

        var nextStepIds = new List<Guid>();
        int? currentPriority = null;
        bool foundMatch = false;

        // Evaluate each transition condition
        foreach (var transition in sortedTransitions)
        {
            // If we already found a non-parallel match at a higher priority, stop
            if (foundMatch && currentPriority.HasValue && transition.Priority > currentPriority.Value)
                break;

            bool conditionMatches = false;

            // Unconditional transitions must always be eligible, even when the completed
            // step includes data (for example approval payloads like {"approved":true}).
            if (string.IsNullOrEmpty(transition.Condition))
            {
                conditionMatches = true;
            }
            else if (transition.IsDefault && (data == null || transition.IsDefault))
            {
                conditionMatches = true;
            }
            // If we have data, evaluate the condition
            else if (data != null && EvaluateCondition(transition.Condition, data))
            {
                conditionMatches = true;
            }

            if (conditionMatches)
            {
                nextStepIds.Add(transition.ToStepId);
                foundMatch = true;
                currentPriority = transition.Priority;

                // If this is not a parallel transition, we're done
                if (!transition.IsParallel)
                    break;
                
                // If parallel, continue to check other transitions at same priority
            }
        }

        // If no conditions matched and there's a default transition, use it
        if (nextStepIds.Count == 0)
        {
            var defaultTransition = sortedTransitions.FirstOrDefault(t => t.IsDefault);
            if (defaultTransition != null)
                nextStepIds.Add(defaultTransition.ToStepId);
        }

        return nextStepIds;
    }

    /// <summary>
    /// Legacy method for backward compatibility. Returns first step ID or null.
    /// </summary>
    [Obsolete("Use EvaluateNextSteps instead for parallel step support")]
    public static Guid? EvaluateNextStep(List<StepTransition> transitions, string? stepData)
    {
        var steps = EvaluateNextSteps(transitions, stepData);
        return steps.Count > 0 ? steps[0] : null;
    }

    /// <summary>
    /// Evaluates a single condition against data
    /// Supports: =, !=, >, <, >=, <=
    /// Format: "key=value" or "key>value"
    /// </summary>
    private static bool EvaluateCondition(string condition, Dictionary<string, object> data)
    {
        // Parse condition
        var operators = new[] { ">=", "<=", "!=", "=", ">", "<" };
        
        foreach (var op in operators)
        {
            if (condition.Contains(op))
            {
                var parts = condition.Split(new[] { op }, 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                    return false;

                var key = parts[0].Trim();
                var expectedValue = parts[1].Trim();

                if (!data.ContainsKey(key))
                    return false;

                var actualValue = data[key]?.ToString() ?? string.Empty;

                return op switch
                {
                    "=" => string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
                    "!=" => !string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
                    ">" => CompareNumeric(actualValue, expectedValue) > 0,
                    "<" => CompareNumeric(actualValue, expectedValue) < 0,
                    ">=" => CompareNumeric(actualValue, expectedValue) >= 0,
                    "<=" => CompareNumeric(actualValue, expectedValue) <= 0,
                    _ => false
                };
            }
        }

        return false;
    }

    private static int CompareNumeric(string value1, string value2)
    {
        if (double.TryParse(value1, out var num1) && double.TryParse(value2, out var num2))
        {
            return num1.CompareTo(num2);
        }
        // Fallback to string comparison
        return string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase);
    }
}
