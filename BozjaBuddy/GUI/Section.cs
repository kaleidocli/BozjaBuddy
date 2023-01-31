using SamplePlugin.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamplePlugin.GUI
{
    /// <summary>
    /// Represents a Section in a Tab
    /// </summary>
    internal abstract class Section : IDrawable, IDisposable
    {
        protected abstract Plugin mPlugin { get; set; }

        public abstract bool DrawGUI();

        public abstract void DrawGUIDebug();

        public abstract void Dispose();
    }
}
