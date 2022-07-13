namespace HexControl.SharedControl.Documents;

internal record DocumentState(
    IReadOnlyList<MarkerState> MarkerStates,
    Selection? SelectionState = null,
    Caret? CaretState = null);