using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Mady.XML
{
	public class Manipulator
	{
		private XmlDocument Document { get; set; }
		private int NextIndex { get; set; }

		/// <summary>
		/// Create a new XML Manipulator for the specified document.
		/// </summary>
		/// <param name="doc">The XML document to manipulate.</param>
		public Manipulator(XmlDocument doc)
		{
			Document = doc;
		}

		/// <summary>
		/// Manipulate the XML document, returning one or more XML documents.
		/// </summary>
		/// <param name="count">The number of documents to generate.</param>
		/// <param name="tagsMatch">Tag/Attribute chains specifying the attributes to match on.</param>
		/// <param name="valsInclude">Target values to include.</param>
		/// <param name="valsExclude">Target values to exclude.</param>
		/// <param name="tagsOnce">Tags to only include in one document.</param>
		/// <param name="tagsExclude">Tags to exclude from all documents.</param>
		/// <param name="emptyTag">Tag to create if the document is empty.</param>
		/// <returns>List of XML documents resulting from the manipulation.</returns>
		public List<XmlDocument> Manipulate(int count, IEnumerable<string> tagsMatch = null, IEnumerable<string> valsInclude = null, IEnumerable<string> valsExclude = null, IEnumerable<string> tagsOnce = null, IEnumerable<string> tagsExclude = null, string emptyTag = "empty")
		{
			var docs = new List<XmlDocument>(count);
			var valueIndex = new Dictionary<string, int>();
			var matchVals = tagsMatch?.Select(v => new MatchVal(v));
			NextIndex = 0;

			// Add commas to the end so they will match the search text.
			tagsOnce = tagsOnce?.Select(v => v + ",");
			tagsExclude = tagsExclude?.Select(v => v + ",");

			for (int i = 0; i < count; i++)
			{
				docs.Add(new XmlDocument());
			}

			foreach (XmlNode n in Document)
			{
				List<NodeResult> results = ParseNode(n, docs, "", valueIndex, matchVals, valsInclude, valsExclude, tagsOnce, tagsExclude);
				foreach (var nr in results)
				{
					docs[nr.Index].AppendChild(nr.Node);
				}
			}

			foreach (var doc in docs)
			{
				bool hasBody = false;
				foreach (XmlNode child in doc.ChildNodes)
				{
					if (child.NodeType == XmlNodeType.Element)
					{
						hasBody = true;
					}
				}
				if (!hasBody)
				{
					doc.AppendChild(doc.CreateElement(emptyTag));
				}
			}

			return docs;
		}

		private List<NodeResult> ParseNode(XmlNode node, List<XmlDocument> docs, string parentName, Dictionary<string, int> valueIndex, IEnumerable<MatchVal> matchVals,
			IEnumerable<string> valsInclude, IEnumerable<string> valsExclude, IEnumerable<string> tagsOnce, IEnumerable<string> tagsExclude)
		{
			var result = new List<NodeResult>();

			string name = parentName + node.Name + ",";

			bool added = false;
			string attribute = matchVals?.FirstOrDefault(v => v.Tags == name)?.Attribute;
			if (attribute != null)
			{
				string val = node.Attributes[attribute]?.Value;
				if (val != null && (valsInclude == null || valsInclude.Contains(val)) && (valsExclude == null || !valsExclude.Contains(val)))
				{
					int ind = NextIndex;
					if (valueIndex.ContainsKey(val))
					{
						ind = valueIndex[val];
					}
					else
					{
						valueIndex[val] = NextIndex;
						NextIndex = (NextIndex + 1) % docs.Count;
					}

					added = true;
					result.Add(new NodeResult(docs, ind, node, true));
				}
			}

			if (!added)
			{
				if (tagsOnce != null && tagsOnce.Contains(name))
				{
					int ind = NextIndex;
					NextIndex = (NextIndex + 1) % docs.Count;
					result.Add(new NodeResult(docs, ind, node, true));
				}
				else if (tagsExclude != null && tagsExclude.Contains(name))
				{
					// Do nothing if the node is on the exclude list.
				}
				else if (node.HasChildNodes &&
					(matchVals?.Any(v => v.Tags.StartsWith(name)) == true ||
					tagsOnce?.Any(v => v.StartsWith(name)) == true ||
					tagsExclude?.Any(v => v.StartsWith(name)) == true))
				{
					var totRes = new List<NodeResult>();
					foreach (XmlNode child in node.ChildNodes)
					{
						List<NodeResult> res = ParseNode(child, docs, name, valueIndex, matchVals, valsInclude, valsExclude, tagsOnce, tagsExclude);
						totRes.AddRange(res);
					}
					var indices = totRes.Select(v => v.Index).Distinct();
					foreach (int i in indices)
					{
						var nr = new NodeResult(docs, i, node, false);
						var nodes = totRes.Where(v => v.Index == i);
						foreach (var n in nodes)
						{
							nr.Node.AppendChild(n.Node);
						}
						result.Add(nr);
					}
				}
				else
				{
					for (int i = 0; i < docs.Count; i++)
					{
						result.Add(new NodeResult(docs, i, node, true));
					}
				}
			}

			return result;
		}

		private class NodeResult
		{
			public int Index { get; set; }
			public XmlNode Node { get; set; }

			public NodeResult(List<XmlDocument> docs, int index, XmlNode node, bool recursive)
			{
				Index = index;
				Node = docs[index].ImportNode(node, recursive);
			}
		}

		private class MatchVal
		{
			public string Tags { get; set; }
			public string Attribute { get; set; }

			public MatchVal(string val)
			{
				int ind = val.LastIndexOf(',') + 1;     // Add 1 so Tags includes the ',' and Attribute is just the name.
				Tags = val.Substring(0, ind);
				Attribute = val.Substring(ind);
			}
		}
	}
}
