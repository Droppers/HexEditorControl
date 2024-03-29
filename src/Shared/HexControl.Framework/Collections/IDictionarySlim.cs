﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace HexControl.Framework.Collections;

/// <summary>
/// A base interface masking <see cref="DictionarySlim{TKey,TValue}"/> instances and exposing non-generic functionalities.
/// </summary>
[PublicAPI]
internal interface IDictionarySlim
{
    /// <summary>
    /// Gets the count of entries in the dictionary.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Clears the current dictionary.
    /// </summary>
    void Clear();
}