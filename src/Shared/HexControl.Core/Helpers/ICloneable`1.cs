using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.Core.Helpers
{
    internal interface ICloneable<out TClone>
    {
        public TClone Clone();
    }
}
