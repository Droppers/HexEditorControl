namespace HexControl.SharedControl.Documents;

internal record DocumentState(
    IReadOnlyDictionary<Guid, MarkerState> MarkerStates,
    Selection? Selection,
    Caret? Caret);