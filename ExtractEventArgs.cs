using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DATLib
{
    public delegate void ExtractEvent(ExtractEventArgs e);

    public class ExtractEventArgs
    {
        protected string name;

        public string Name { get { return name; } }

        public ExtractEventArgs(string name)
        {
            this.name = name;
        }
    }
}
