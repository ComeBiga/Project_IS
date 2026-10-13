
# Project
- Engine: Unity 2022.3.61f1
- Language: C#
- Render Pipeline: URP
- Genre: 2.5D Puzzle Platformer

# Naming Conventions
- Public methods and classes use PascalCase.
- Ordinary private fields use mName format.
- [SerializeField] private fields use _name format.
- Private methods start with a lowercase letter.
- Keep the existing LogCollector and LogEntry class names.

# Coding Rules
- Follow the existing architecture and coding style.
- Use APIs compatible with Unity 2022.3.
- Do not introduce Unity 6-only APIs.
- Avoid unnecessary allocations in frequently called methods.
- Prefer minimal changes over large refactoring.

# Unity Asset Safety
- Do not manually modify .meta files.
- Do not change scenes or prefabs without approval.
- Do not modify Library, Temp, or obj folders.
- Do not install or upgrade packages without approval.

# Workflow
- Explain significant changes before implementing them.
- Summarize changed files and validation steps.
- Clearly state any tests that were not run.
  