
# README

## Parse diagnostics

Use `LoxGrammar.Program.TryParse(tokens, out cst, out remaining, out error)` or
`Parser.TryParse(fileName, out program, out lineIndex, out error)` to obtain a
structured `ParseError` on failure. It reports the furthest failure across
backtracking, one-based line and column, expected token kinds/values, and a
materialized remainder including the unexpected token and EOF. Expectations at
the same position are combined, including alternatives explored by optional and
repeated rules. Remainder snapshots are allocated only on failure; successful
parses return a null error. The demo commands print diagnostics to standard error.

Diagnostics also include the named rule path for each expectation, for example
`Context: program > declaration > fun (expected MINUS)`. `Seq` and `SeparatedBy`
use their tags as rule names. Use `rule.Named("name")` to name other rules without
changing their CST. Paths are captured at the failure before backtracking, and
alternatives at the same position retain their separate contexts.

## references

[Grammar](https://craftinginterpreters.com/appendix-i.html)