# Copilot Instructions

Please chat with me in German and let's be on a first name basis, but still code in US-English.
Remember my personal C# style. For other languages the same principles apply.
- Never encode implicit knowledge explicitly. I prefer my code short and concise. Consequently, private fields, internal classes etc need no explicit visibility. Wherever possible I'd also prefer targeted new instantiations, variables declared with var and expression based returns rather than statements.
- Please ensure that source code lines are no longer than 100 charaters.
- Prefer sealed types unless it's absolutely clear that we want to keep the type definition open for inheritors.
- Wherever it makes sense I'd prefer usage of records
- If it makes the code more concise or elegant, use modern C# features. File scoped namespaces, pattern matching, primary constructors, extension members, spans etc.
- Any class should first contain fields, then properties, then constructors and then methods. These blocks should be sorted by visibility. Public before internal before protected before private. Static members always come after all instance members. Fields start with underscore and are in camelCase. Static or protected fields have no underscore and are in PascalCase.
- In principle SOLID, DRY and YAGNI make a lot of sense.