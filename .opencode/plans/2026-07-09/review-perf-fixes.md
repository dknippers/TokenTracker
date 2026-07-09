## Task

Review commit `ba59a2f` (perf fixes by deepseek-v4-pro) and fix issues found.

## Issues found

1. **WidgetViewModel — unnecessary allocation on empty case**: The code allocates a new `ObservableCollection` even when `visibleKeys` is empty. Should use `ModelRows.Clear()` for the empty case (no allocation).
2. **WidgetViewModel — plan/implementation mismatch**: Plan said `AddRange`; implementation used ref swap. The ref swap is fine (single `PropertyChanged` from setter), but `ObservableCollection` ctor with `List<T>` fires a single `Reset` event, which is better than adding items one by one.
3. **ModelDisplayNameRules — premature ConcurrentDictionary removal**: The claim that `Format()` is UI-thread-only is unenforced. `ConcurrentDictionary` lock overhead is negligible (nanoseconds per lookup) and it future-proofs against background-thread callers.

## Changes

- `src/ViewModels/WidgetViewModel.cs`: Empty case → `Clear()`, non-empty → `List<T>` with capacity hint → `ObservableCollection` ctor → property swap.
- `src/Services/ModelDisplayNameRules.cs`: Revert to `ConcurrentDictionary` with `GetOrAdd`.
