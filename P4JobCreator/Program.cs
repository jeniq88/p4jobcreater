using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

class Program
{
    static int Main(string[] args)
    {
        string p4Port   = Environment.GetEnvironmentVariable("P4PORT")   ?? "JQUESTA0725:1666";
        string p4User   = Environment.GetEnvironmentVariable("P4USER")   ?? "jeniq";
        string p4Passwd = Environment.GetEnvironmentVariable("P4PASSWD") ?? "Password";
        string p4Client = Environment.GetEnvironmentVariable("P4CLIENT") ?? "jeniq_JQUESTA0725_5856";

        string payloadJson = Console.In.ReadToEnd();
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            Console.Error.WriteLine("P4JobCreator: No payload received on stdin.");
            return 1;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"P4JobCreator: Failed to parse JSON payload: {ex.Message}");
            return 1;
        }

        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("body", out JsonElement body))
        {
            Console.Error.WriteLine("P4JobCreator: Payload missing 'body' field.");
            return 1;
        }

        if (!body.TryGetProperty("events", out JsonElement events) || events.ValueKind != JsonValueKind.Array)
        {
            Console.Error.WriteLine("P4JobCreator: Payload missing 'events' array.");
            return 1;
        }

        TrustP4Server(p4Port, p4User, p4Passwd);

        int errorCount = 0;
        foreach (JsonElement evt in events.EnumerateArray())
        {
            string issueId  = "";
            string issueUrl = "";

            if (evt.TryGetProperty("item", out JsonElement item))
            {
                if (item.TryGetProperty("tag", out JsonElement tag))
                    issueId = tag.GetString() ?? "";

                if (item.TryGetProperty("httpurl", out JsonElement url))
                    issueUrl = url.GetString() ?? "";
            }

            string description = BuildDescription(payloadJson, evt);
            string jobSpec = BuildJobSpec(p4User, description, issueId, issueUrl);

            bool success = CreateP4Job(jobSpec, p4Port, p4User, p4Passwd, p4Client);
            if (!success) errorCount++;
        }

        return errorCount > 0 ? 1 : 0;
    }

    static string BuildDescription(string fullPayload, JsonElement evt)
    {
        string prettyPayload = FormatJson(fullPayload);
        var sb = new StringBuilder();

        if (evt.TryGetProperty("action", out JsonElement action))
            sb.AppendLine($"Action: {action.GetString()}");

        if (evt.TryGetProperty("item", out JsonElement item))
        {
            if (item.TryGetProperty("itemType", out JsonElement itemType))
                sb.AppendLine($"Item Type: {itemType.GetString()}");
            if (item.TryGetProperty("tag", out JsonElement tag))
                sb.AppendLine($"Tag: {tag.GetString()}");
            if (item.TryGetProperty("number", out JsonElement number))
                sb.AppendLine($"Number: {number}");
        }

        sb.AppendLine();
        sb.AppendLine("Full Payload:");
        sb.Append(prettyPayload);

        return sb.ToString();
    }

    static string FormatJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    static string BuildJobSpec(string user, string description, string issueId, string issueUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Job:\tnew");
        sb.AppendLine("Status:\topen");
        sb.AppendLine($"User:\t{user}");

        sb.AppendLine("Description:");
        foreach (string line in description.Split('\n'))
            sb.AppendLine($"\t{line}");

        if (!string.IsNullOrEmpty(issueId))
            sb.AppendLine($"ISSUE_ID:\t{issueId}");

        if (!string.IsNullOrEmpty(issueUrl))
            sb.AppendLine($"ISSUE_URL:\t{issueUrl}");

        return sb.ToString();
    }

    static void TrustP4Server(string p4Port, string p4User, string p4Passwd)
    {
        try
        {
            RunP4Command("trust -y", p4Port, p4User, p4Passwd, null, out _, out _);
        }
        catch { }
    }

    static bool CreateP4Job(string jobSpec, string p4Port, string p4User, string p4Passwd, string p4Client)
    {
        bool success = RunP4Command("job -i", p4Port, p4User, p4Passwd, p4Client, out string stdout, out string stderr, jobSpec);

        if (!string.IsNullOrWhiteSpace(stdout))
            Console.WriteLine($"P4JobCreator: {stdout.Trim()}");

        if (!string.IsNullOrWhiteSpace(stderr))
            Console.Error.WriteLine($"P4JobCreator Error: {stderr.Trim()}");

        return success;
    }

    static bool RunP4Command(string p4Args, string p4Port, string p4User, string p4Passwd, string? p4Client,
        out string stdout, out string stderr, string? stdinData = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "p4",
            Arguments = p4Args,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute = false
        };

        psi.Environment["P4PORT"]   = p4Port;
        psi.Environment["P4USER"]   = p4User;
        psi.Environment["P4PASSWD"] = p4Passwd;
        if (!string.IsNullOrEmpty(p4Client))
            psi.Environment["P4CLIENT"] = p4Client;

        using var process = Process.Start(psi)!;

        if (stdinData != null)
            process.StandardInput.Write(stdinData);

        process.StandardInput.Close();

        stdout = process.StandardOutput.ReadToEnd();
        stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode == 0;
    }
}
