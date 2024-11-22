using BozjaBuddy.GUI.NodeGraphViewer.ext;
using BozjaBuddy.GUI.NodeGraphViewer.NodeContent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.Sheets;
using QuickGraph;
using static BozjaBuddy.GUI.NodeGraphViewer.Node;
using System.Numerics;
using QuickGraph.Algorithms.Observers;

namespace BozjaBuddy.GUI.NodeGraphViewer.utils
{
    public class JsonConverters
    {
        /// https://stackoverflow.com/questions/66783180/in-json-nets-jsonconverter-readjson-why-is-reader-value-null
        public class NodeJsonConverter : JsonConverter<Node>
        {
            public override Node ReadJson(JsonReader reader, Type objectType, Node? existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {

                JObject jo = JObject.Load(reader);

                Node node = jo["mType"]?.ToString() switch
                {
                    BasicNode.nodeType => new BasicNode(),
                    BBNode.nodeType => new BBNode(),
                    AuxNode.nodeType => new AuxNode(),
                    _ => new BasicNode()
                };
                string id = jo["mId"]?.ToString() ?? "-1";
                int graphId = (int?)jo["mGraphId"] ?? -1;
                string? tag = jo["mTag"]?.ToString();
                Node.NodeStyle? style = jo["mStyle"]?.ToObject<Node.NodeStyle>(serializer);

                Dictionary<string, string>? contentTemp = jo["mContent"]?.ToObject<Dictionary<string, string>>();
                string? type = null;
                if (contentTemp != null)
                {
                    contentTemp.TryGetValue("_contentType", out string? ct);
                    type = ct;
                }
                NodeContent.NodeContent content = type switch
                {
                    NodeContent.NodeContent.nodeContentType => jo["mContent"]?.ToObject<NodeContent.NodeContent>(serializer),
                    BBNodeContent.nodeContentType => jo["mContent"]?.ToObject<BBNodeContent>(serializer),
                    NodeContentError.nodeContentType => jo["mContent"]?.ToObject<NodeContentError>(serializer),
                    _ => jo["mContent"]?.ToObject<NodeContentError>(serializer)
                } ?? new NodeContentError("NodeDeserializationError", "Unable to deserialize node's content for this node.");

                node.Init(id, graphId, content, _style: style, tag);
                node.mPack = jo["mPack"]?.ToObject<HashSet<string>>() ?? new();
                node.mPackerNodeId = jo["mPackerNodeId"] == null ? null : jo["mPackerNodeId"]!.ToString();
                if (node.mPackerNodeId == string.Empty) node.mPackerNodeId = null;
                node.mIsPacked = jo["mIsPacked"] == null ? false : (bool)jo["mIsPacked"]!;
                node.mPackingStatus = (PackingStatus)Convert.ToInt32(jo["mPackingStatus"]);
                node._relaPosLastPackingCall = jo["_relaPosLastPackingCall"] == null ? null : jo["_relaPosLastPackingCall"]?.ToObject<Vector2?>();

                return node;
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, Node node, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class NodeCanvasJsonConverter : JsonConverter<NodeCanvas>
        {
            public override NodeCanvas ReadJson(JsonReader reader, Type objectType, NodeCanvas? existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {

                JObject jo = JObject.Load(reader);

                NodeCanvas canvas = new(Convert.ToInt32(jo["mId"]), jo["mName"]?.ToString());

                var graph = JsonConvert.DeserializeObject<AdjacencyGraph<int, SEdge<int>>>(jo["mGraph"]!.ToString(), new AdjacencyGraphConverter());
                var nodes = JsonConvert.DeserializeObject<Dictionary<string, Node>>(jo["mNodes"]!.ToString(), new NodeJsonConverter());
                var edges = jo["mEdges"]?.ToObject<List<Edge>>();
                // reconstructing graph
                if (graph != null)
                {
                    if (nodes != null)
                    {
                        foreach (var node in nodes.Values)
                        {
                            if (node == null) continue;
                            graph.AddVertex(node.mGraphId);
                        }
                    }
                    if (edges != null)
                    {
                        foreach (var edge in edges)
                        {
                            if (edge == null) continue;
                            graph.AddEdge(edge.GetEdge());
                        }
                    }
                }

                canvas.Init(
                        Convert.ToInt32(jo["_nodeCounter"]),
                        jo["mMap"]?.ToObject<NodeMap>() ?? new(),
                        jo["mNodes"] == null ? new() : (JsonConvert.DeserializeObject<Dictionary<string, Node>>(jo["mNodes"]!.ToString(), new NodeJsonConverter()) ?? new()),
                        jo["_nodeIds"]?.ToObject<HashSet<string>>() ?? new(),
                        jo["mOccuppiedRegion"]?.ToObject<OccupiedRegion>() ?? new(),
                        graph ?? new(),
                        edges ?? new(),
                        jo["mConfig"]?.ToObject<NodeCanvas.CanvasConfig>() ?? new(),
                        jo["_nodeRenderZOrder"]?.ToObject<LinkedList<string>>() ?? new(),
                        jo["_nodeIdAndNodeGraphId"]?.ToObject<Dictionary<int, string>>() ?? new()
                    );

                return canvas;
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, NodeCanvas canvas, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class PartialCanvasDataConverter : JsonConverter<NodeCanvas.PartialCanvasData>
        {
            public override NodeCanvas.PartialCanvasData ReadJson(JsonReader reader, Type objectType, NodeCanvas.PartialCanvasData? existingValue,
                                                                    bool hasExistingValue, JsonSerializer serializer)
            {

                JObject jo = JObject.Load(reader);

                NodeCanvas.PartialCanvasData data = new();

                List<Node> nodes = jo["nodes"] == null ? new() : (JsonConvert.DeserializeObject<List<Node>>(jo["nodes"]!.ToString(), new NodeJsonConverter()) ?? new());
                List<Edge> relatedEdges = jo["relatedEdges"] == null ? new List<Edge>() : (jo["relatedEdges"]!.ToObject<List<Edge>>() ?? new List<Edge>());

                string? anchorNodeId = jo["anchorNodeId"]?.ToString();
                Dictionary<string, Vector2> offsetFromAnchor = jo["offsetFromAnchor"]?.ToObject<Dictionary<string, Vector2>>() ?? new();

                data.nodes = nodes;
                data.relatedEdges = relatedEdges;
                data.offsetFromAnchor = offsetFromAnchor;
                data.anchorNodeId = anchorNodeId;

                return data;
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, NodeCanvas.PartialCanvasData? graph, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public class AdjacencyGraphConverter : JsonConverter<AdjacencyGraph<int, SEdge<int>>>
        {
            public override AdjacencyGraph<int, SEdge<int>> ReadJson(JsonReader reader, Type objectType, AdjacencyGraph<int, SEdge<int>>? existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {

                JObject jo = JObject.Load(reader);

                AdjacencyGraph<int, SEdge<int>> graph = new();

                List<int> vertices = jo["Vertices"]?.ToObject<List<int>>() ?? new();

                return graph;
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, AdjacencyGraph<int, SEdge<int>> graph, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
