using System.Text;
using System.Text.Json;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    /// <summary>
    /// Talks to Google's Gemini API (free tier).
    /// Two public methods:
    ///   1) AnalyzeResumeAsync  -> scores a pasted resume
    ///   2) AskAdvisorAsync     -> answers career questions
    /// API key is read from appsettings.json -> "Gemini:ApiKey"
    /// </summary>
    public class AIService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<AIService> _log;

        public AIService(HttpClient http, IConfiguration config, ILogger<AIService> log)
        {
            _http = http;
            _apiKey = config["Gemini:ApiKey"] ?? "";
            _model = config["Gemini:Model"] ?? "gemini-2.5-flash";
            _log = log;
        }

        /// <summary>True once a real API key has been pasted into appsettings.json.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_apiKey) &&
            _apiKey != "YOUR_GEMINI_API_KEY_HERE";

        // ─────────────────────────────────────────────────────────
        //  PUBLIC: Resume Analyzer
        // ─────────────────────────────────────────────────────────
        public async Task<ResumeAnalysisResult> AnalyzeResumeAsync(
            string resumeText,
            StudentProfile? profile,
            List<StudentSkill> skills,
            List<JobWithMatchScore> topJobs)
        {
            if (!IsConfigured)
                return ResumeAnalysisResult.Error("AI service is not configured. Add your Gemini API key to appsettings.json.");

            if (string.IsNullOrWhiteSpace(resumeText) || resumeText.Trim().Length < 50)
                return ResumeAnalysisResult.Error("Resume text is too short. Please paste your full resume (at least 50 characters).");

            string context = BuildStudentContext(profile, skills, topJobs, null);
            string prompt = $@"You are an expert resume reviewer for university students applying to internships and entry-level tech jobs.

{context}

RESUME TEXT FROM STUDENT:
---
{resumeText}
---

Analyze this resume against the student's matched jobs above. Return ONLY valid JSON (no markdown fences, no commentary) with EXACTLY this structure:

{{
  ""overall_score"": <integer 0-100>,
  ""skills_alignment"": <integer 0-100, how well listed skills match top-matched job requirements>,
  ""completeness"": <integer 0-100, are key sections present: contact, education, skills, experience, projects>,
  ""experience_language"": <integer 0-100, use of strong action verbs and quantified achievements>,
  ""keyword_density"": <integer 0-100, presence of relevant tech keywords for matched jobs>,
  ""feedback"": ""<2-4 sentences of specific, actionable feedback. Mention 1-2 concrete improvements by name.>""
}}";

            try
            {
                string responseText = await CallGeminiJsonAsync(prompt);
                return ParseResumeAnalysis(responseText);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Resume analysis failed");
                return ResumeAnalysisResult.Error("AI request failed: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  PUBLIC: AI Career Advisor
        // ─────────────────────────────────────────────────────────
        public async Task<string> AskAdvisorAsync(
            string question,
            List<ChatMessage> history,
            StudentProfile? profile,
            List<StudentSkill> skills,
            List<JobWithMatchScore> topJobs,
            SkillGapResult? topGap)
        {
            if (!IsConfigured)
                return "AI service is not configured. Please add your Gemini API key to appsettings.json.";

            if (string.IsNullOrWhiteSpace(question))
                return "Please type a question first.";

            string context = BuildStudentContext(profile, skills, topJobs, topGap);
            string systemInstruction = $@"You are a friendly, concise AI career advisor for students on the CogniCareer platform.

{context}

GUIDELINES:
- Give specific, personalized advice based on the data above. Reference the student's actual skills and matched jobs by name when relevant.
- Keep responses SHORT: 3-6 sentences, or a tight numbered list of up to 4 items.
- Use plain text. No markdown headings. You may bold key terms with **double asterisks** sparingly.
- If asked what to learn next, suggest 1-3 specific missing skills with a short reason WHY (e.g., 'unlocks +X% match on Job Title').
- If you do not have enough data to answer, ask ONE short clarifying question.
- Never invent jobs, skills, or numbers that are not in the data above.";

            // Build Gemini multi-turn payload.
            // Trick: REST Gemini doesn't have a separate system prompt, so we prepend
            // the instruction as a fake user turn + an acknowledging model turn.
            var contents = new List<object>
            {
                new { role = "user",  parts = new[] { new { text = systemInstruction } } },
                new { role = "model", parts = new[] { new { text = "Understood. I'll be a concise, personalized career advisor for this student." } } }
            };

            // Append last 10 turns of chat history (oldest first)
            foreach (var msg in history.TakeLast(10))
            {
                contents.Add(new
                {
                    role = msg.Role == "user" ? "user" : "model",
                    parts = new[] { new { text = msg.Content } }
                });
            }

            // Append the new question
            contents.Add(new { role = "user", parts = new[] { new { text = question } } });

            try
            {
                return await CallGeminiMultiTurnAsync(contents);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Advisor call failed");
                return "Sorry — I couldn't reach the AI service right now. (" + ex.Message + ")";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: shared context block for both features
        // ─────────────────────────────────────────────────────────
        private string BuildStudentContext(
            StudentProfile? profile,
            List<StudentSkill> skills,
            List<JobWithMatchScore> topJobs,
            SkillGapResult? topGap)
        {
            var sb = new StringBuilder();

            sb.AppendLine("STUDENT PROFILE:");
            if (profile != null && profile.IsProfileComplete)
            {
                sb.AppendLine($"- Name: {profile.FullName}");
                sb.AppendLine($"- Degree: {profile.Degree} at {profile.University}");
                sb.AppendLine($"- Semester: {profile.Semester}, GPA: {profile.GPA}, Graduates: {profile.ExpectedGradYear}");
            }
            else
            {
                sb.AppendLine("- Profile not yet completed.");
            }

            sb.AppendLine();
            sb.AppendLine("STUDENT SKILLS:");
            if (skills.Any())
            {
                foreach (var s in skills)
                    sb.AppendLine($"- {s.SkillName} ({s.ProficiencyLevel})");
            }
            else
            {
                sb.AppendLine("- No skills added yet.");
            }

            sb.AppendLine();
            sb.AppendLine("TOP MATCHED JOBS ON THIS PLATFORM:");
            if (topJobs.Any())
            {
                foreach (var j in topJobs.Take(5))
                    sb.AppendLine($"- {j.Job.Title} at {j.Job.CompanyName} — {j.MatchScore}% match");
            }
            else
            {
                sb.AppendLine("- No active jobs at the moment.");
            }

            if (topGap != null && (topGap.MatchedSkills.Any() || topGap.MissingSkills.Any()))
            {
                sb.AppendLine();
                sb.AppendLine("SKILL GAP (for the student's strongest applied job):");
                if (topGap.MatchedSkills.Any())
                    sb.AppendLine($"- Already has: {string.Join(", ", topGap.MatchedSkills.Select(s => s.SkillName))}");
                if (topGap.MissingSkills.Any())
                    sb.AppendLine($"- Missing: {string.Join(", ", topGap.MissingSkills.Select(s => $"{s.SkillName}({s.Priority})"))}");
            }

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: low-level Gemini calls
        // ─────────────────────────────────────────────────────────
        private async Task<string> CallGeminiJsonAsync(string prompt)
        {
            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.4
                }
            };
            return await SendGeminiRequest(body);
        }

        private async Task<string> CallGeminiMultiTurnAsync(List<object> contents)
        {
            var body = new
            {
                contents = contents,
                generationConfig = new { temperature = 0.6 }
            };
            return await SendGeminiRequest(body);
        }

        private async Task<string> SendGeminiRequest(object body)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", _apiKey);
            req.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var resp = await _http.SendAsync(req);
            string raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogError("Gemini API error {Status}: {Body}", resp.StatusCode, raw);
                throw new Exception($"Gemini returned HTTP {(int)resp.StatusCode}. Check your API key and quota.");
            }

            using var doc = JsonDocument.Parse(raw);
            string text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return text.Trim();
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: parse resume JSON
        // ─────────────────────────────────────────────────────────
        private ResumeAnalysisResult ParseResumeAnalysis(string text)
        {
            try
            {
                // Some models occasionally wrap JSON in ```json fences even when told not to.
                text = text.Trim();
                if (text.StartsWith("```"))
                {
                    int nl = text.IndexOf('\n');
                    if (nl > 0) text = text[(nl + 1)..];
                    if (text.EndsWith("```")) text = text[..^3];
                    text = text.Trim();
                }

                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                return new ResumeAnalysisResult
                {
                    Success = true,
                    OverallScore = ReadInt(root, "overall_score"),
                    SkillsAlignment = ReadInt(root, "skills_alignment"),
                    Completeness = ReadInt(root, "completeness"),
                    ExperienceLanguage = ReadInt(root, "experience_language"),
                    KeywordDensity = ReadInt(root, "keyword_density"),
                    Feedback = root.TryGetProperty("feedback", out var fb)
                        ? (fb.GetString() ?? "")
                        : ""
                };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to parse resume JSON. Raw: {Text}", text);
                return ResumeAnalysisResult.Error("AI returned an unexpected response format. Please try again.");
            }
        }

        private static int ReadInt(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p)) return 0;
            if (p.ValueKind == JsonValueKind.Number) return p.GetInt32();
            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out int v)) return v;
            return 0;
        }
    }
}