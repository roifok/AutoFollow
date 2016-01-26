using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeta.Common;

namespace AutoFollow.Resources
{
    public interface ITargetable
    {
        int AcdId { get; }
        Vector3 Position { get; }
        int WorldSnoId { get; }
        float Distance { get; }
    }
}

