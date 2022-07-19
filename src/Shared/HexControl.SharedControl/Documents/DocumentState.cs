using HexControl.Framework.Collections;

namespace HexControl.SharedControl.Documents;

internal record DocumentState(
    DictionarySlim<Guid, MarkerState> MarkerStates,
    Selection? Selection,
    Caret? Caret);