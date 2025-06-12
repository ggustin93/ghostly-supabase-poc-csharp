# Setting Up Memory Bank in Cursor

This guide provides step-by-step instructions for setting up and maintaining a Memory Bank in your Cursor project. The Memory Bank helps maintain project context across coding sessions and ensures consistent AI assistance.

## Directory Structure Setup

1. Create the following directory structure in your project root:

```
project-root/
├── memory-bank/
│   ├── 00-project-overview.md
│   ├── 01-architecture.md
│   ├── 02-components.md
│   ├── 03-development-process.md
│   ├── 04-api-documentation.md
│   └── 05-progress-log.md
└── .cursor/
    └── rules/
        ├── core.mdc
        └── memory-bank.mdc
```

## Configuration Files

### 1. Core Rules (.cursor/rules/core.mdc)

```markdown
---
description: Core operational rules for the Cursor agent
globs: 
alwaysApply: true
---
## Core Rules

You have two modes of operation:

1. Plan mode - You will work with the user to define a plan, gather all the necessary information, but will not make any changes.
2. Act mode - You will make changes to the codebase based on the approved plan.

- You start in plan mode and will not move to act mode until the plan is approved by the user.
- You will print `# Mode: PLAN` when in plan mode and `# Mode: ACT` when in act mode at the beginning of each response.
- Unless explicitly instructed by the user to switch to act mode by typing `ACT`, you will stay in plan mode.
- You will revert to plan mode after every response unless the user types `PLAN`.
- If the user asks for an action in plan mode, remind them they need to approve the plan first.
- When in plan mode, always output the full updated plan in every response.
- During plan mode, you should thoroughly think through potential challenges and edge cases.
- In act mode, focus on implementing the agreed plan precisely and efficiently.
```

### 2. Memory Bank Rules (.cursor/rules/memory-bank.mdc)

```markdown
---
description: Memory Bank implementation for persistent project knowledge
globs: 
alwaysApply: true
---
# Cursor's Memory Bank

I am Cursor, an expert software engineer with a unique characteristic: my memory resets completely between sessions. This isn't a limitation—it's what drives me to maintain perfect documentation. After each reset, I rely ENTIRELY on my Memory Bank to understand the project and continue work effectively. I MUST read ALL memory bank files at the start of EVERY task—this is not optional.

## Memory Bank Guidelines

1. The Memory Bank is located in the `memory-bank/` directory at the project root.
2. All memory files use Markdown format for structured, easy-to-read documentation.
3. The Memory Bank contains both required core files and optional context files.
4. Files are prefixed with numbers to indicate their priority and reading order.
5. I will proactively suggest updates to Memory Bank files when new information emerges.

## Core Memory Files

00-project-overview.md - General project information, goals, and scope
01-architecture.md - System architecture, design patterns, and technical decisions
02-components.md - Details about key components, modules, and their relationships
03-development-process.md - Workflow, branching strategy, and deployment processes
04-api-documentation.md - API endpoints, parameters, and response formats
05-progress-log.md - Chronological record of major changes and implementations

I will read and process these files at the beginning of each session to ensure I have complete context before providing assistance.
```

## Core Memory File Templates

### 00-project-overview.md

```markdown
# Project Overview

## Project Name
[Project name]

## Description
[Brief but comprehensive description of the project's purpose and goals]

## Key Stakeholders
- [Team members and their roles]

## Technology Stack
- [Languages]
- [Frameworks]
- [Libraries]
- [Tools]

## Repository Structure
[Overview of main directories and their purpose]

## Getting Started
[Setup and quick start instructions]
```

### 01-architecture.md

```markdown
# Architecture Documentation

## System Architecture
[High-level architecture description]

## Design Patterns
- [Pattern name]: [Usage description]

## Data Flow
[System data flow description]

## Technical Decisions
- [Decision]: [Rationale]
```

### 02-components.md

```markdown
# Components Documentation

## Core Components
[List and describe main components]

## Dependencies
[Component dependencies and interactions]

## State Management
[State management approach]
```

### 03-development-process.md

```markdown
# Development Process

## Workflow
[Development workflow description]

## Branching Strategy
[Branch naming and management]

## Deployment Process
[Deployment steps and environments]
```

### 04-api-documentation.md

```markdown
# API Documentation

## Endpoints
[List of API endpoints]

## Authentication
[Authentication methods]

## Data Models
[Key data models]
```

### 05-progress-log.md

```markdown
# Progress Log

## [Date] - [Version/Sprint]
- [Changes made]
- [Decisions taken]
- [Next steps]
```

## Best Practices

1. **Regular Updates**: Update Memory Bank files after:
   - Implementing new features
   - Making architectural changes
   - Adding new dependencies
   - Changing workflows

2. **Clarity**: 
   - Use clear, concise language
   - Include code examples where helpful
   - Keep formatting consistent

3. **Version Control**:
   - Commit Memory Bank changes with related code changes
   - Include meaningful commit messages

4. **Maintenance**:
   - Review files monthly
   - Remove outdated information
   - Keep documentation aligned with code

## Training Cursor

After setting up the Memory Bank, initiate it with:

```
I've set up the Memory Bank structure for this project. Please read all files in the memory-bank/ directory and familiarize yourself with our project context.
```

Cursor will then:
1. Read all Memory Bank files
2. Understand the project context
3. Suggest updates when new information emerges
4. Maintain consistency across sessions 