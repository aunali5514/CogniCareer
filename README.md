# CogniCareer

**An AI-powered career intelligence platform connecting students, companies, and institutions.**

🔗 **Live:** [cognicareer-app.azurewebsites.net](https://cognicareer-app.azurewebsites.net)

---

## Overview

CogniCareer helps students discover the right career paths and job opportunities by matching their skills against real company requirements, benchmarking them against peers, and closing skill gaps with targeted learning resources. Built as a full-stack academic project, it combines a normalized relational database, a weighted matching algorithm, and a multi-provider AI layer into one cohesive platform.

## Features

- **Weighted Skill-Match Algorithm** — scores student profiles against job/company requirements using a 70/30 weighting model
- **Multi-Provider AI Fallback Chain** — Gemini → Groq → OpenRouter, ensuring AI features stay available even if one provider fails or rate-limits
- **Role-Based Portals** — separate, permission-scoped experiences for Students, Companies, and Admins
- **Peer Benchmarking** — see how your skill profile stacks up against other students
- **Skill Gap Analyzer** — identifies missing skills for a target role and recommends learning resources
- **Secure Authentication** — BCrypt password hashing
- **AI Chat & Dashboard Insights** — conversational AI assistant plus six AI-driven dashboard analytics handlers

## Tech Stack

- **Backend:** ASP.NET Core (Razor Pages), C#
- **Database:** SQL Server, T-SQL — 12-table normalized schema, stored-procedure-only data access
- **AI Integration:** Gemini, Groq, OpenRouter (fallback chain)
- **Auth:** BCrypt

## Architecture

The system follows a layered architecture:

```
Models/     → Domain entities
Data/       → Stored procedures & data access layer
Services/   → Business logic (MatchScoreService, AIService, etc.)
Pages/      → Razor Pages (Student / Company / Admin portals)
```

All data access goes through stored procedures — no inline SQL in application code — for security and maintainability.

## Team

Built with **Ahmad Sohail**, **Areej Ahmad**, and **Tasbeeha Moeed**, under the mentorship of Prof. Esha Hayat (OOP) and Prof. Amna Adnan (Database Systems), as a second-semester project at UET Lahore.

## Deployment

Hosted on **Azure App Service** with an **Azure SQL Database** backend, with GitHub Actions handling continuous deployment on every push to `main`.

## Setup

```bash
# Clone the repository
git clone https://github.com/aunali5514/CogniCareer.git
cd CogniCareer

# Restore dependencies
dotnet restore

# Update connection string in appsettings.json (not committed — use appsettings.Development.json locally)

# Run database migrations / stored procedure scripts (see /Data/Scripts)

# Run the app
dotnet run
```

> **Note:** No secrets or connection strings are committed to this repo. Configure your own `appsettings.Development.json` locally with your SQL Server connection string and AI provider API keys.

## License

This project was built for academic purposes as part of coursework at UET Lahore.
