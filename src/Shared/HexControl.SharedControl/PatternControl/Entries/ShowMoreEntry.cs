using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.SharedControl.PatternControl.Entries
{
    internal class ShowMoreEntry : PatternEntry
    {
        public PatternEntry Entry { get; }

        public ShowMoreEntry(SharedPatternControl tree, PatternData pattern, PatternEntry entry) : base(tree, pattern)
        {
            Entry = entry;
        }

        public override string FormatName() => "Show more items...";
    }
}
