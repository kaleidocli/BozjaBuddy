using BozjaBuddy.GUI.NodeGraphViewer.ext;
using BozjaBuddy.GUI.NodeGraphViewer.NodeContent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                NodeContent.NodeContent contentTemp = jo["mContent"]?.ToObject<NodeContent.NodeContent>(serializer) ?? new NodeContentError("NodeDeserializationError", "Unable to deserialize node's content for this node.");
                NodeContent.NodeContent content = contentTemp._contentType switch
                {
                    NodeContent.NodeContent.nodeContentType => jo["mContent"]?.ToObject<NodeContent.NodeContent>(serializer),
                    BBNodeContent.nodeContentType => jo["mContent"]?.ToObject<BBNodeContent>(serializer),
                    NodeContentError.nodeContentType => jo["mContent"]?.ToObject<NodeContentError>(serializer),
                    _ => jo["mContent"]?.ToObject<NodeContentError>(serializer)
                } ?? new NodeContentError("NodeDeserializationError", "Unable to deserialize node's content for this node.");
                Node.NodeStyle? style = jo["mStyle"]?.ToObject<Node.NodeStyle>(serializer);

                node.Init(id, graphId, content, _style: style);

                return node;
            }

            public override bool CanWrite => false;

            public override void WriteJson(JsonWriter writer, Node polygon, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
        
    }
}
