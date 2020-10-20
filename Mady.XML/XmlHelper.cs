using System;
using System.Collections;
using System.Xml;

namespace Mady.XML
{
	[AttributeUsage(AttributeTargets.Property)]
	public class XmlFormatAttribute : Attribute
	{
		public string FormatString { get; }
		public XmlFormatAttribute(string formatString)
		{
			FormatString = formatString;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class XmlExcludeAttribute : Attribute { }

	public static class XmlHelper
	{
		/// <summary>
		/// Creates a new XmlDocument instance with a default XML declaration.
		/// </summary>
		/// <returns>The new XmlDocument instance.</returns>
		public static XmlDocument CreateDocument()
		{
			var doc = new XmlDocument();
			doc.InsertBefore(doc.CreateXmlDeclaration("1.0", "UTF-8", ""), doc.DocumentElement);
			return doc;
		}

		/// <summary>
		/// Adds a child node to the current XmlNode.
		/// </summary>
		/// <param name="node">The node to add the node to.</param>
		/// <param name="name">The name of the new node.</param>
		/// <returns>The newly added XmlNode.</returns>
		public static XmlNode Add(this XmlNode node, string name)
		{
			return node.AppendChild(node.OwnerDocument.CreateElement(name));
		}

		/// <summary>
		/// Adds a node to the current XmlDocument.
		/// </summary>
		/// <param name="doc">The document to add the node to.</param>
		/// <param name="name">The name of the new node.</param>
		/// <returns>The newly added XmlNode.</returns>
		public static XmlNode Add(this XmlDocument doc, string name)
		{
			return doc.AppendChild(doc.CreateElement(name));
		}

		/// <summary>
		/// Sets the specified attribute on the current XmlNode.
		/// </summary>
		/// <param name="node">The node whose attribute will be set.</param>
		/// <param name="name">The name of the attribute to set.</param>
		/// <param name="value">The value to set for the attribute.</param>
		/// <returns>The node whose attribute was set.</returns>
		public static XmlNode Set(this XmlNode node, string name, object value)
		{
			var attribute = node.OwnerDocument.CreateAttribute(name);
			attribute.Value = value?.ToString();
			node.Attributes.SetNamedItem(attribute);
			return node;
		}

		/// <summary>
		/// Constructs an XML document based off of the current object and
		/// all of its public properties.
		/// </summary>
		/// <param name="obj">The object to create XML for.</param>
		/// <returns>The XML document based off of the specified object.</returns>
		public static XmlDocument ToXml(this object obj)
		{
			var doc = CreateDocument();
			doc.Add(obj.GetType().Name).AddPropertiesFromObject(obj);
			return doc;
		}

		private static void AddPropertiesFromObject(this XmlNode parent, object obj)
		{
			foreach (var prop in obj.GetType().GetProperties())
			{
				object val = prop.GetValue(obj);
				if (val != null && Attribute.GetCustomAttribute(prop, typeof(XmlExcludeAttribute)) == null)
				{
					var formatAttribute = (XmlFormatAttribute)Attribute.GetCustomAttribute(prop, typeof(XmlFormatAttribute));
					if (formatAttribute != null)
					{
						var method = val.GetType().GetMethod("ToString", new[] { typeof(string) });
						if (method != null)
						{
							val = method.Invoke(val, new[] { formatAttribute.FormatString });
						}
						else
						{
							throw new Exception($"Cannot apply format string \"{formatAttribute.FormatString}\" to type {prop.PropertyType.FullName}");
						}
					}

					if (prop.PropertyType == typeof(string))
					{
						parent.Set(prop.Name, val);
					}
					else if (prop.PropertyType.GetInterface(nameof(IEnumerable)) != null)
					{
						foreach (object v in (IEnumerable)val)
						{
							parent.Add(v.GetType().Name).AddPropertiesFromObject(v);
						}
					}
					else if (prop.PropertyType.IsClass)
					{
						parent.Add(prop.PropertyType.Name).AddPropertiesFromObject(val);
					}
					else
					{
						parent.Set(prop.Name, val);
					}
				}
			}
		}
	}
}
