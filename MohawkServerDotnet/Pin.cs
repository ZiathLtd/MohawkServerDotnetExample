using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MohawkWebserverDotNet
{
    public class Pin
    {
        public int row { get; set; } //this is what I mean by property
        public int column { get; set; }

        public Pin(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

    }
}
