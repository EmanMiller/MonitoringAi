using System.Text;
using System.Text.RegularExpressions;
using DashboardApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DashboardApi.Services;

/// <summary>
/// Conversational dashboard creation: guides user through title → metrics → variables, returns structured steps or complete payload.
/// </summary>
public class DashboardFlowService
{
    private readonly GeminiChatService _gemini;

    public DashboardFlowService(GeminiChatService gemini)
    {
        _gemini = gemini;
    }

    /// <summary>
    /// Process user message in dashboard-creation flow. Returns assistant text plus optional step data or complete payload.
    /// </summary>
    public async Task<DashboardFlowResult> ProcessAsync(
        string userMessage,
        DashboardFlowContext? flowContext,
        IReadOnlyList<DashboardFlowHistoryItem>? history,
        CancellationToken cancellationToken = default)
    {
        var system = BuildSystemPrompt();
        var user = BuildUserMessage(userMessage, flowContext, history);
        var raw = await _gemini.GenerateWithSystemAsync(system, user, cancellationToken);
        return ParseResponse(raw);
    }

    private static string BuildSystemPrompt()
    {
        return """
You are a dashboard creation assistant. Guide the user through these steps in order. Be concise and friendly.

STEPS:
1) Dashboard title — Ask for a name. Rules: must start with an uppercase letter, 3–50 characters, only letters, numbers, spaces, hyphens, underscores. If invalid, say what's wrong and ask again.
2) Metric selection — Offer: "Success Rate %", "Error Rate %", "Slow Queries", "Past 7 day trend". User can use defaults or pick custom alternatives (e.g. "4xx/5xx Rate", "Exception Count", "Query Response Time", "Week-over-Week Change"). Collect at least one metric.
3) Template variables — Ask: use defaults (timeslice=15m, domain=example.com, domainPrefix=www, environment=prod) or customize timeslice, domain, domainPrefix, environment.
4) Confirm — Summarize and when user confirms, output the complete payload.

OUTPUT FORMAT — You MUST use exactly these blocks so the app can parse them.

When asking for the next step (or re-asking), output:
[DASHBOARD_STEP]
{"step":1,"prompt":"What would you like to name your dashboard?","type":"text_input"}
[DASHBOARD_STEP]

Step 1 type: "text_input". Step 2 type: "options" and include "options":["Success Rate %","Error Rate %","Slow Queries","Past 7 day trend"]. Step 3 type: "variables" (prompt about defaults vs custom). Step 4 type: "confirm" (prompt to confirm).

When the user has confirmed and you have all required data, output the full payload once:
[DASHBOARD_COMPLETE]
{"dashboardTitle":"...","useDefaults":true,"variables":{"timeslice":"15m","domain":"example.com","domainPrefix":"www","environment":"prod"},"panels":{"Success Rate %":true,"Error Rate %":true}}
[DASHBOARD_COMPLETE]

Panels: use the metric labels as keys; value true for default, or false with "Metric Name_custom":"Alternative Name" for custom. Variables only needed when useDefaults is false.

Always include exactly one [DASHBOARD_STEP] or [DASHBOARD_COMPLETE] block per response. You may add one short sentence before the block. If the user's title is invalid, output a [DASHBOARD_STEP] for step 1 again with a prompt that explains the rule and asks again.
""";
    }

    private static string BuildUserMessage(string userMessage, DashboardFlowContext? flowContext, IReadOnlyList<DashboardFlowHistoryItem>? history)
    {
        var sb = new StringBuilder();
        if (flowContext != null)
        {
            sb.AppendLine("--- Current flow state ---");
            sb.AppendLine("Step: " + (flowContext.Step ?? 0));
            if (flowContext.Collected != null)
                sb.AppendLine("Collected so far: " + JsonConvert.SerializeObject(flowContext.Collected));
            sb.AppendLine("---");
        }
        if (history != null && history.Count > 0)
        {
            sb.AppendLine("--- Conversation so far ---");
            foreach (var h in history.Take(20))
                sb.AppendLine((h.Sender == "user" ? "User: " : "Assistant: ") + (h.Text ?? ""));
            sb.AppendLine("---");
        }
        sb.Append("User now says: ").Append(userMessage ?? "");
        return sb.ToString();
    }

    private static DashboardFlowResult ParseResponse(string raw)
    {
        var result = new DashboardFlowResult
        {
            ResponseText = raw?.Trim() ?? ""
        };

        var stepBlock = Regex.Match(raw ?? "", @"\[DASHBOARD_STEP\]\s*(\{[\s\S]*?\})\s*\[DASHBOARD_STEP\]");
        if (stepBlock.Success)
        {
            try
            {
                result.StepData = JsonConvert.DeserializeObject<DashboardStepData>(stepBlock.Groups[1].Value);
            }
            catch
            {
                // leave null if parse fails
            }
        }

        var completeBlock = Regex.Match(raw ?? "", @"\[DASHBOARD_COMPLETE\]\s*(\{[\s\S]*?\})\s*\[DASHBOARD_COMPLETE\]");
        if (completeBlock.Success)
        {
            try
            {
                var jobj = JObject.Parse(completeBlock.Groups[1].Value);
                result.CompletePayload = new DashboardWizardRequest
                {
                    DashboardTitle = jobj["dashboardTitle"]?.ToString(),
                    UseDefaults = jobj["useDefaults"]?.Value<bool>() ?? true,
                    Variables = jobj["variables"] != null ? jobj["variables"].ToObject<TemplateVariables>() : null,
                    Panels = jobj["panels"] != null ? jobj["panels"].ToObject<Dictionary<string, object>>() : null
                };
            }
            catch
            {
                result.CompletePayload = null;
            }
        }

        return result;
    }
}

public class DashboardFlowContext
{
    public int? Step { get; set; }
    public DashboardCollected? Collected { get; set; }
}

public class DashboardCollected
{
    public string? DashboardTitle { get; set; }
    public bool? UseDefaults { get; set; }
    public TemplateVariables? Variables { get; set; }
    public Dictionary<string, object>? Panels { get; set; }
}

public class DashboardStepData
{
    public int Step { get; set; }
    public string? Prompt { get; set; }
    public string? Type { get; set; }
    public List<string>? Options { get; set; }
}

public class DashboardFlowResult
{
    public string ResponseText { get; set; } = "";
    public DashboardStepData? StepData { get; set; }
    public DashboardWizardRequest? CompletePayload { get; set; }
}

public class DashboardFlowHistoryItem
{
    public string? Sender { get; set; }
    public string? Text { get; set; }
}
