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
     SkillGapResult? topGap,
     List<LearningResource> learningResources)
        {
            if (!IsConfigured)
                return "AI service is not configured. Please add your Gemini API key to appsettings.json.";

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
- When recommending a skill to learn, if there is a learning resource for it in the LEARNING RESOURCES list above, recommend that exact resource by its title and provider (e.g., 'Try ""Docker for Beginners"" by Udemy on the Skill Gap page').
- If you do not have enough data to answer, ask ONE short clarifying question.
- Never invent jobs, skills, courses, or numbers that are not in the data above.";

            // Build Gemini multi-turn payload.
            // Trick: REST Gemini doesn't have a separate system prompt, so we prepend
            // the instruction as a fake user turn + an acknowledging model turn.
            var contents = new List<object>
    {
        new { role = "user",  parts = new[] { new { text = systemInstruction } } },
        new { role = "model", parts = new[] { new { text = "Understood. I'll be a concise, personalized career advisor for this student and recommend specific platform resources when relevant." } } }
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
        //  PUBLIC: Explain Match Score (per-job)
        // ─────────────────────────────────────────────────────────
        public async Task<string> ExplainMatchAsync(
            Job job,
            decimal matchScore,
            List<StudentSkill> studentSkills,
            SkillGapResult gap)
        {
            if (!IsConfigured)
                return "AI service is not configured. Add your Gemini API key to appsettings.json.";

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
                var contents = new List<object>
        {
            new { role = "user", parts = new[] { new { text = prompt } } }
        };
                return await CallGeminiMultiTurnAsync(contents);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Explain match failed");
                return "Sorry — couldn't generate an explanation right now. (" + ex.Message + ")";
            }
        }
        // ─────────────────────────────────────────────────────────
        //  PUBLIC: Cover Letter Generator (per-job)
        // ─────────────────────────────────────────────────────────
        public async Task<string> GenerateCoverLetterAsync(
            Job job,
            StudentProfile? profile,
            List<StudentSkill> studentSkills,
            SkillGapResult gap,
            string tone = "professional")
        {
            if (!IsConfigured)
                return "AI service is not configured. Add your Gemini API key to appsettings.json.";

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

            // Tone guidance
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
                var contents = new List<object>
        {
            new { role = "user", parts = new[] { new { text = prompt } } }
        };
                return await CallGeminiMultiTurnAsync(contents);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Cover letter generation failed");
                return "Sorry — couldn't generate a cover letter right now. (" + ex.Message + ")";
            }
        }
        // ─────────────────────────────────────────────────────────
        //  PRIVATE: shared context block for both features
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

            // === NEW: learning resources available on the platform for the student's missing skills ===
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
                if ((int)resp.StatusCode == 429)
                    throw new Exception("AI service is at its free-tier limit right now. Please wait a minute and try again, or switch to gemini-2.5-flash-lite in appsettings.json.");
                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                    throw new Exception("AI service rejected the API key. Check the key in appsettings.json.");
                if ((int)resp.StatusCode == 503 || (int)resp.StatusCode == 500 || (int)resp.StatusCode == 502 || (int)resp.StatusCode == 504)
                    throw new Exception("AI service is temporarily overloaded. Please try again in a few seconds.");
                throw new Exception($"Gemini returned HTTP {(int)resp.StatusCode}.");
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