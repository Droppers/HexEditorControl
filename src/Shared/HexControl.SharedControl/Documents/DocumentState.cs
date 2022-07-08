namespace HexControl.SharedControl.Documents;

// TODO: Investigate whether selection state must be implemented
internal record DocumentState(
    IReadOnlyList<MarkerState> MarkerStates,
    Selection? SelectionState = null,
    Caret? CaretState = null);