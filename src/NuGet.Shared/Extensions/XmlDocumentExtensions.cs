﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;

#if UAP
using System.Text.RegularExpressions;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;
using XmlNode = Windows.Data.Xml.Dom.IXmlNode;
#else
using XmlDocument = System.Xml.XmlDocument;
using XmlElement = System.Xml.XmlElement;
using XmlNode = System.Xml.XmlNode;
#endif

namespace NuGet.Shared.Extensions
{
	public static class XmlDocumentExtensions
	{
		/// <summary>
		/// Retrieves the PackageReferences from the given XmlDocument. If a package is present multiple time, only the first version will be returned.
		/// </summary>
		/// <param name="document"></param>
		/// <returns>A Dictionary where the key is the id of a package and the value its version.</returns>
		public static Dictionary<string, string> GetPackageReferences(this XmlDocument document)
		{
			var references = new Dictionary<string, string>();

			var packageReferences = document.SelectElements("PackageReference");
			var dotnetCliReferences = document.SelectElements("DotNetCliToolReference");

			foreach(var packageReference in packageReferences.Concat(dotnetCliReferences))
			{
				var packageId = new[] { "Include", "Update", "Remove" }
					.Select(packageReference.GetAttribute)
					.FirstOrDefault(x => !string.IsNullOrEmpty(x));
				var packageVersion = packageReference.GetAttribute("Version");

				if(packageVersion.HasValue())
				{
					references.TryAdd(packageId, packageReference.GetAttribute("Version"));
				}
				else
				{
					var node = packageReference.SelectNode("Version");
					if(node != null)
					{
						references.TryAdd(packageId, node.InnerText);
					}
				}
			}

			return references;
		}

		/// <summary>
		/// Retrieves the dependency elements from the given XmlDocument. If a package is present multiple time, only the first version will be returned.
		/// </summary>
		/// <param name="document"></param>
		/// <returns>A Dictionary where the key is the id of a package and the value its version.</returns>
		public static Dictionary<string, string> GetDependencies(this XmlDocument document)
			=> document
				.SelectElements("dependency")
				.ToDictionary(dependency => dependency.GetAttribute("id"), dependency => dependency.GetAttribute("version"));

		#region Utilities

		/// <summary>
		/// Loads an XmlDocument from the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static async Task<XmlDocument> LoadDocument(this string path, CancellationToken ct)
		{
#if UAP
			var file = await StorageFile
				.GetFileFromPathAsync(path)
				.AsTask(ct);

			var document = await XmlDocument
				.LoadFromFileAsync(file, new XmlLoadSettings { ElementContentWhiteSpace = true })
				.AsTask(ct);
#else
			var document = new XmlDocument()
			{
				PreserveWhitespace = true,
			};

			document.Load(path);
#endif

			return document;
		}

		/// <summary>
		/// Save the given XmlDocument at the given path.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="ct"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		public static async Task Save(this XmlDocument document, CancellationToken ct, string path)
		{
#if UAP
			var xml = document.GetXml();

			xml = Regex.Replace(xml, @"(<\? ?xml)(?<declaration>.+)( ?\?>)", x => !x.Groups["declaration"].Value.Contains("encoding", StringComparison.OrdinalIgnoreCase)
				? x.Result("$1${declaration} encoding=\"utf-8\"$2") // restore encoding declaration that is stripped by `GetXml`
				: x.Value
			);
			xml = Regex.Replace(xml, "(\\?>)(<)", "$1\n$2"); // xml declaration should follow by a new line
			xml = Regex.Replace(xml, "([^ ])(/>)", "$1 $2"); // self-enclosing tag should end with a space

			await FileIO.WriteTextAsync(await StorageFile.GetFileFromPathAsync(path).AsTask(ct), xml);
#else
			document.Save(path);
#endif
		}

		/// <summary>
		/// Select the XmlElements matching the given element name in the XmlDocument.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="elementName">Name of the XML tags to look for.</param>
		/// <param name="filter">Addtional xpath filter to apply.</param>
		/// <returns></returns>
		public static IEnumerable<XmlElement> SelectElements(this XmlDocument document, string elementName, string filter = null) => document
			.SelectNodes($"//*[local-name() = '{elementName}']{filter}") //Using local-name to avoid having to deal with namespaces
			.OfType<XmlElement>();

		/// <summary>
		/// Select the first child node with the given of an element.
		/// </summary>
		/// <param name="element"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static XmlNode SelectNode(this XmlElement element, string name) => element
			.ChildNodes
			.OfType<XmlElement>()
			.FirstOrDefault(e => e.LocalName.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
		#endregion
	}
}