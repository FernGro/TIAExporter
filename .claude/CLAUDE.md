# AI Framework

Use `.ai-framework/` as the initialization layer for this repository.

Read order:

1. `.ai-framework/README.md`
2. `.ai-framework/context/current-state.md`
3. `.ai-framework/rules/global-rules.md`
4. The task-specific files in `.ai-framework/agents/`, `.ai-framework/skills/`, `.ai-framework/pipelines/` and `.ai-framework/exchange/`

Directory map:

- `.ai-framework/agents/`: agent roles and responsibilities
- `.ai-framework/skills/`: reusable workflows and task hooks
- `.ai-framework/rules/`: mandatory behavior and guardrails
- `.ai-framework/pipelines/`: end-to-end task flows
- `.ai-framework/context/`: current state, decisions and open questions
- `.ai-framework/exchange/`: task brief, plan, handoff and reports
- `.ai-framework/templates/`: reusable task and review templates

`.claude/` only points to the framework. It is not the source of truth.
