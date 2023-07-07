using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    public struct NodeGraphViewerConfig
    {
        public NodeGraphViewerConfig() { }

        public float unitGridSmall = 10;
        public float unitGridLarge = 50;
        public float gridSnapProximity = 3.5f;
        public float timeForRulerTextFade = 2500;
        public bool showRulerText = false;
        public Vector2? sizeLastKnown = null;

        public float autoSaveInterval = 120000;
        public float saveCapacity = 60;        // intend to cover half an hour
    }
}
