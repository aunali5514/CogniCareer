using System.Text;
using System.Text.Json;
using CogniCareer.Models;

namespace CogniCareer.Services
{
    /// <summary>
    /// Multi-provider AI service with automatic fallback.
    /// Reads a chain of providers from appsettings.json ("AI:Providers")
    /// and tries each one in order. If a provider returns 429 / 5xx /
    /// any error, the next provider in the list is tried automatically.
    ///
    /// Supported providers:
    ///   - "gemini"     → Google Gemini API
    ///   - "groq"       → Groq Cloud (OpenAI-compatible)
    ///   - "openrouter" → OpenRouter (OpenAI-compatible)
    ///
    /// Backward compatible: if "AI:Providers" is missing, falls back to
    /// reading the legacy "Gemini:ApiKey" + "Gemini:Model" config.
    /// </summary>
    public class AIService
    {
        private readonly HttpClient _http;
        private readonly ILogger<AIService> _log;
        private readonly List<ProviderConfig> _providers;

        public AIService(HttpClient http, IConfiguration config, ILogger<AIService> log)
        {
            _http = http;
            _log = log;
            _providers = LoadProviders(config);

            if (_providers.Any())
                _log.LogInformation("AI service initialised with {Count} provider(s): {Names}",
                    _providers.Count, string.Join(" → ", _providers.Select(p => p.Name)));
            else
                _log.LogWarning("AI service has NO providers configured.");
        }

        public bool IsConfigured => _providers.Any();

        // ─────────────────────────────────────────────────────────
        //  Provider config loading (with backward compatibility)
        // ─────────────────────────────────────────────────────────
        private static List<ProviderConfig> LoadProviders(IConfiguration config)
        {
            var list = new List<ProviderConfig>();

            var section = config.GetSection("AI:Providers");
            if (section.Exists())
            {
                foreach (var child in section.GetChildren())
                {
                    var name = (child["Name"] ?? "").Trim().ToLowerInvariant();
                    var key = (child["ApiKey"] ?? "").Trim();
                    var model = (child["Model"] ?? "").Trim();
                    if (IsValidKey(key) && !string.IsNullOrEmpty(name))
                        list.Add(new ProviderConfig { Name = name, ApiKey = key, Model = model });
                }
            }

            // Legacy fallback: just "Gemini:ApiKey" / "Gemini:Model"
            if (!list.Any())
            {
                var oldKey = (config["Gemini:ApiKey"] ?? "").Trim();
                if (IsValidKey(oldKey))
                {
                    list.Add(new ProviderConfig
                    {
                        Name = "gemini",
                        ApiKey = oldKey,
                        Model = (config["Gemini:Model"] ?? "gemini-2.5-flash").Trim()
                    });
                }
            }

            return list;
        }

        private static bool IsValidKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            if (key.StartsWith("YOUR_") || key.EndsWith("_HERE")) return false;
            if (key.StartsWith("PASTE_")) return false;
            return true;
        }

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
                return ResumeAnalysisResult.Error("AI service is not configured. Add at least one provider API key to appsettings.json.");

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
                string responseText = await CallWithFallbackAsync(
                    geminiBody: BuildGeminiJsonBody(prompt, 0.4),
                    openAiMessages: new List<object> { new { role = "user", content = prompt } },
                    jsonOutput: true,
                    temperature: 0.4);
                return ParseResumeAnalysis(responseText);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Resume analysis failed across all providers");
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
            SkillGapResult? topGap,
            List<LearningResource> learningResources)
        {
            if (!IsConfigured)
                return "AI service is not configured. Please add at least one provider API key.";

            if (string.IsNullOrWhiteSpace(question))
                return "Please type a question first.";

            string context = BuildStudentContext(profile, skills, topJobs, topGap, learningResources);
            string systemInstruction = $@"You are a friendly, concise AI career advisor for students on the CogniCareer platform.

{context}

GUIDELINES:
- Give specific, personalized advice based on the data above. Reference the student's actual skills and matched jobs by name when relevant.
- Keep responses SHORT: 3-6 sentences, or a tight numbered list of up to 4 items.
- Use plain text. No markdown headings. You may bold key terms with **double asterisks** sparingly.
- If asked what to learn next, suggest 1-3 specific missing skills with a short reason WHY (e.g., 'unlocks +X% match on Job Title').
- When recommending a skill to learn, if there is a learning resource for it in the LEARNING RESOURCES list above, recommend that exact resource by its title and provider.
- If you do not have enough data to answer, ask ONE short clarifying question.
- Never invent jobs, skills, courses, or numbers that are not in the data above.";

            try
            {
                // Build BOTH provider-specific payloads — fallback picks the right one
                var geminiContents = BuildGeminiChatContents(systemInstruction, history, question);
                var openAiMessages = BuildOpenAIMessages(systemInstruction, history, question);

                var geminiBody = new
                {
                    contents = geminiContents,
                    generationConfig = new { temperature = 0.6 }
                };

                return await CallWithFallbackAsync(
                    geminiBody: geminiBody,
                    openAiMessages: openAiMessages,
                    jsonOutput: false,
                    temperature: 0.6);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Advisor call failed across all providers");
                return "Sorry — I couldn't reach any AI service right now. (" + ex.Message + ")";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  PUBLIC: Explain Match Score
        // ─────────────────────────────────────────────────────────
        public async Task<string> ExplainMatchAsync(
            Job job,
            decimal matchScore,
            List<StudentSkill> studentSkills,
            SkillGapResult gap)
        {
            if (!IsConfigured)
                return "AI service is not configured. Add at least one provider API key to appsettings.json.";

            var sb = new StringBuilder();
            sb.AppendLine($"JOB: {job.Title} at {job.CompanyName}");
            sb.AppendLine($"JOB TYPE: {job.JobType}");
            sb.AppendLine($"JOB DESCRIPTION: {(string.IsNullOrWhiteSpace(job.Description) ? "Not provided." : job.Description)}");
            sb.AppendLine($"COMPUTED MATCH SCORE: {matchScore}%");
            sb.AppendLine();
            sb.AppendLine($"STUDENT'S SKILLS: {(studentSkills.Any() ? string.Join(", ", studentSkills.Select(s => $"{s.SkillName}({s.ProficiencyLevel})")) : "None added.")}");

            if (gap.MatchedSkills.Any())
                sb.AppendLine($"STRENGTHS (skills required by the job that the student already has): {string.Join(", ", gap.MatchedSkills.Select(s => s.SkillName))}");
            else
                sb.AppendLine("STRENGTHS: None of the student's skills directly match the job requirements.");

            if (gap.MissingSkills.Any())
                sb.AppendLine($"GAPS (skills required by the job that the student is missing, priority shown): {string.Join(", ", gap.MissingSkills.Select(s => $"{s.SkillName}({s.Priority})"))}");
            else
                sb.AppendLine("GAPS: No missing skills detected.");

            string prompt = $@"You are a career coach explaining to a university student why they match this specific job at the given percentage.

{sb}

Write a clear 3-5 sentence explanation in plain text. Structure:
1. First sentence: name the 2-3 strongest reasons the student matches (cite specific matching skills by name).
2. Second sentence: name the biggest 1-2 gaps that are pulling the score down (cite specific missing skills by name).
3. Final sentence: one practical, encouraging piece of advice on what to focus on first.

Use a friendly tone. Plain text only — no markdown headings, no bullet points, no numbered lists. Do not invent skills or facts that are not in the data above.";

            try
            {
                return await CallWithFallbackAsync(
                    geminiBody: BuildGeminiSimpleBody(prompt, 0.5),
                    openAiMessages: new List<object> { new { role = "user", content = prompt } },
                    jsonOutput: false,
                    temperature: 0.5);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Explain match failed across all providers");
                return "Sorry — couldn't generate an explanation right now. (" + ex.Message + ")";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  PUBLIC: Cover Letter Generator
        // ─────────────────────────────────────────────────────────
        public async Task<string> GenerateCoverLetterAsync(
            Job job,
            StudentProfile? profile,
            List<StudentSkill> studentSkills,
            SkillGapResult gap,
            string tone = "professional")
        {
            if (!IsConfigured)
                return "AI service is not configured. Add at least one provider API key to appsettings.json.";

            var sb = new StringBuilder();
            sb.AppendLine($"JOB TITLE: {job.Title}");
            sb.AppendLine($"COMPANY: {job.CompanyName}");
            sb.AppendLine($"JOB TYPE: {job.JobType}");
            sb.AppendLine($"JOB DESCRIPTION: {(string.IsNullOrWhiteSpace(job.Description) ? "Not provided." : job.Description)}");
            sb.AppendLine();
            sb.AppendLine("ABOUT THE CANDIDATE:");
            if (profile != null && profile.IsProfileComplete)
            {
                sb.AppendLine($"- Name: {profile.FullName}");
                sb.AppendLine($"- Email: {profile.Email}");
                sb.AppendLine($"- Education: {profile.Degree} at {profile.University}");
                sb.AppendLine($"- Current Semester: {profile.Semester}, GPA: {profile.GPA}, Graduating: {profile.ExpectedGradYear}");
            }
            else
            {
                sb.AppendLine("- Profile incomplete (only name available).");
                if (profile != null) sb.AppendLine($"- Name: {profile.FullName}");
            }

            if (studentSkills.Any())
                sb.AppendLine($"- Skills: {string.Join(", ", studentSkills.Select(s => $"{s.SkillName}({s.ProficiencyLevel})"))}");

            if (gap.MatchedSkills.Any())
                sb.AppendLine($"- Skills that match this job: {string.Join(", ", gap.MatchedSkills.Select(s => s.SkillName))}");

            string toneGuide = tone.ToLower() switch
            {
                "enthusiastic" => "Warm and energetic. Show genuine excitement about the company. Use confident, engaged language without being over-the-top.",
                "concise" => "Brief and direct. Aim for ~180-220 words total. Cut filler. Get to the point fast.",
                _ => "Professional and confident, but not stiff. Personable yet polished. Aim for ~250-320 words total."
            };

            string prompt = $@"You are writing a personalized cover letter on behalf of a university student applying to the job below.

{sb}

TONE: {toneGuide}

INSTRUCTIONS:
- Write a complete cover letter starting with ""Dear Hiring Manager,"" and ending with ""Sincerely,\n{(profile?.FullName ?? "[Your Name]")}"".
- Structure: opening hook (1 paragraph) → relevant skills/experience tied to the job (1-2 paragraphs) → enthusiastic close with call to action (1 paragraph).
- Reference the candidate's ACTUAL matching skills by name. Tie them to what the job needs.
- Do NOT invent experiences, internships, certifications, or skills not listed above.
- Do NOT use placeholder text like ""[insert example]"" or ""[your achievement]"" — write real, plausible sentences using only the data above.
- Do NOT include the date, addresses, or a subject line — just the salutation, body paragraphs, and signature.
- Output plain text only. No markdown. No bullet points. Paragraphs separated by blank lines.";

            try
            {
                return await CallWithFallbackAsync(
                    geminiBody: BuildGeminiSimpleBody(prompt, 0.7),
                    openAiMessages: new List<object> { new { role = "user", content = prompt } },
                    jsonOutput: false,
                    temperature: 0.7);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Cover letter failed across all providers");
                return "Sorry — couldn't generate a cover letter right now. (" + ex.Message + ")";
            }
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: Build shared student context (same as before)
        // ─────────────────────────────────────────────────────────
        private string BuildStudentContext(
            StudentProfile? profile,
            List<StudentSkill> skills,
            List<JobWithMatchScore> topJobs,
            SkillGapResult? topGap,
            List<LearningResource>? learningResources = null)
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

            if (learningResources != null && learningResources.Any() && topGap != null)
            {
                sb.AppendLine();
                sb.AppendLine("LEARNING RESOURCES AVAILABLE ON THIS PLATFORM (recommend these by name when relevant):");
                foreach (var skillGroup in learningResources.GroupBy(r => r.SkillID))
                {
                    var skillName = topGap.MissingSkills.FirstOrDefault(s => s.SkillID == skillGroup.Key)?.SkillName ?? "unknown";
                    sb.AppendLine($"  For {skillName}:");
                    foreach (var r in skillGroup.Take(3))
                    {
                        string freeTag = r.IsFree ? " [FREE]" : "";
                        sb.AppendLine($"    - \"{r.Title}\" by {r.Provider} ({r.ResourceType}){freeTag}");
                    }
                }
            }

            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: Build payload helpers
        // ─────────────────────────────────────────────────────────
        private static object BuildGeminiSimpleBody(string prompt, double temperature) => new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature }
        };

        private static object BuildGeminiJsonBody(string prompt, double temperature) => new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { response_mime_type = "application/json", temperature }
        };

        private static List<object> BuildGeminiChatContents(
            string systemInstruction, List<ChatMessage> history, string question)
        {
            // Gemini REST has no separate system role; prepend as fake user + ack
            var contents = new List<object>
            {
                new { role = "user",  parts = new[] { new { text = systemInstruction } } },
                new { role = "model", parts = new[] { new { text = "Understood. I'll be a concise, personalized career advisor for this student and recommend specific platform resources when relevant." } } }
            };
            foreach (var msg in history.TakeLast(10))
            {
                contents.Add(new
                {
                    role = msg.Role == "user" ? "user" : "model",
                    parts = new[] { new { text = msg.Content } }
                });
            }
            contents.Add(new { role = "user", parts = new[] { new { text = question } } });
            return contents;
        }

        private static List<object> BuildOpenAIMessages(
            string systemInstruction, List<ChatMessage> history, string question)
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemInstruction }
            };
            foreach (var msg in history.TakeLast(10))
            {
                messages.Add(new
                {
                    role = msg.Role == "user" ? "user" : "assistant",
                    content = msg.Content
                });
            }
            messages.Add(new { role = "user", content = question });
            return messages;
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: Orchestrator — try providers in order
        // ─────────────────────────────────────────────────────────
        private async Task<string> CallWithFallbackAsync(
            object geminiBody,
            List<object> openAiMessages,
            bool jsonOutput,
            double temperature)
        {
            Exception? lastError = null;
            foreach (var p in _providers)
            {
                try
                {
                    _log.LogInformation("Trying AI provider: {Name}", p.Name);
                    string result = p.Name switch
                    {
                        "gemini" => await SendGeminiRequest(p, geminiBody),
                        "groq" => await SendOpenAIRequest(p,
                                            "https://api.groq.com/openai/v1/chat/completions",
                                            openAiMessages, jsonOutput, temperature),
                        "openrouter" => await SendOpenAIRequest(p,
                                            "https://openrouter.ai/api/v1/chat/completions",
                                            openAiMessages, jsonOutput, temperature),
                        _ => throw new Exception($"Unknown provider in config: {p.Name}")
                    };
                    _log.LogInformation("AI provider {Name} succeeded.", p.Name);
                    return result;
                }
                catch (Exception ex)
                {
                    _log.LogWarning("Provider {Name} failed: {Msg}. Trying next provider...", p.Name, ex.Message);
                    lastError = ex;
                }
            }
            throw lastError ?? new Exception("No AI providers available.");
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: Gemini HTTP call
        // ─────────────────────────────────────────────────────────
        private async Task<string> SendGeminiRequest(ProviderConfig p, object body)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{p.Model}:generateContent";

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-goog-api-key", p.ApiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            string raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogError("Gemini API error {Status}: {Body}", resp.StatusCode, raw);
                throw new Exception($"Gemini HTTP {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim() ?? "";
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: OpenAI-compatible HTTP call (Groq, OpenRouter, etc.)
        // ─────────────────────────────────────────────────────────
        private async Task<string> SendOpenAIRequest(
            ProviderConfig p,
            string endpoint,
            List<object> messages,
            bool jsonOutput,
            double temperature)
        {
            object body = jsonOutput
                ? (object)new
                {
                    model = p.Model,
                    messages,
                    temperature,
                    response_format = new { type = "json_object" }
                }
                : new
                {
                    model = p.Model,
                    messages,
                    temperature
                };

            using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
            req.Headers.Add("Authorization", $"Bearer {p.ApiKey}");
            if (p.Name == "openrouter")
            {
                // OpenRouter recommends these headers — used for analytics / app identification
                req.Headers.Add("HTTP-Referer", "https://cognicareer.local");
                req.Headers.Add("X-Title", "CogniCareer");
            }
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            string raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogError("{Provider} API error {Status}: {Body}", p.Name, resp.StatusCode, raw);
                throw new Exception($"{p.Name} HTTP {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim() ?? "";
        }

        // ─────────────────────────────────────────────────────────
        //  PRIVATE: JSON parsing for resume analysis
        //  More lenient than before — extracts the first {...} block
        //  even if a model wraps it in prose
        // ─────────────────────────────────────────────────────────
        private ResumeAnalysisResult ParseResumeAnalysis(string text)
        {
            try
            {
                text = text.Trim();

                // Strip markdown fences if present
                if (text.StartsWith("```"))
                {
                    int nl = text.IndexOf('\n');
                    if (nl > 0) text = text[(nl + 1)..];
                    if (text.EndsWith("```")) text = text[..^3];
                    text = text.Trim();
                }

                // Extract first {...} block in case the model added extra prose
                int firstBrace = text.IndexOf('{');
                int lastBrace = text.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    text = text.Substring(firstBrace, lastBrace - firstBrace + 1);

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

    // ─────────────────────────────────────────────────────────
    //  Internal helper types
    // ─────────────────────────────────────────────────────────
    internal class ProviderConfig
    {
        public string Name { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "";
    }
}